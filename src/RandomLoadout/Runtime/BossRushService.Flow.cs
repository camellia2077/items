using System;
using System.Collections;
using Dungeonator;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class BossRushService
    {
        private void LoadEncounter(BossRushEncounter encounter)
        {
            if (encounter == null || (object)GameManager.Instance == null)
            {
                RaiseStatus(GrantCommandExecutionResult.Localized(false, "result.boss_rush.teleport_failed", GuiText.Get("label.boss_rush.floor.none")));
                Reset();
                return;
            }

            _currentBossRoom = null;
            _hasClaimedRewardThisEncounter = false;
            _hasAttemptedPlayerBootstrapThisFloor = false;
            _state = BossRushState.LoadingFloor;
            if (_currentEncounterIndex == 0 && (object)_selectedPlayerPrefab != null)
            {
                GameManager.PlayerPrefabForNewGame = _selectedPlayerPrefab;
                LogInfo("Prepared selected character prefab for first Boss Rush floor: " + GetCharacterLabel(_selectedCharacterLabel) + ".");
            }

            if (_currentEncounterIndex == 0 && GameManager.Instance.IsFoyer && (object)Foyer.Instance != null)
            {
                LogInfo("Departing foyer before loading the first Boss Rush floor.");
                Foyer.Instance.OnDepartedFoyer();
            }

            LogInfo("Loading Boss Rush floor " + encounter.SceneName + " (" + encounter.FloorKey + ").");
            GameManager.Instance.LoadCustomLevel(encounter.SceneName);
        }

        private IEnumerator PrepareFloorAndTeleportToBossRoom_CR()
        {
            LogInfo("Preparing player state and boss-room teleport for " + GetCurrentFloorLabel() + ".");
            int readyFrames = 0;
            RoomHandler lastReadyRoom = null;
            for (int frame = 0; frame < MaxTeleportFrames; frame++)
            {
                if (!IsActive || _state == BossRushState.ReturningToCharacterSelect)
                {
                    _activeCoroutine = null;
                    yield break;
                }

                GameManager gameManager = GameManager.Instance;
                PlayerController player = gameManager != null ? gameManager.PrimaryPlayer : null;
                Dungeon dungeon = gameManager != null ? gameManager.Dungeon : null;
                if (frame == 0 || frame % 30 == 0)
                {
                    LogInfo(
                        "Boss Rush floor wait frame " +
                        frame +
                        ". Player=" +
                        DescribePlayerState(player) +
                        ", DungeonReady=" +
                        ((object)dungeon != null && dungeon.data != null) +
                        ".");
                }

                if ((object)player != null && (object)dungeon != null && dungeon.data != null)
                {
                    if (!_hasAttemptedPlayerBootstrapThisFloor && _currentEncounterIndex == 0 && !HasUsableGun(player))
                    {
                        _hasAttemptedPlayerBootstrapThisFloor = true;
                        LogWarning("PrimaryPlayer entered Boss Rush without a usable gun. Attempting first-floor bootstrap.");
                        PlayerController bootstrappedPlayer = TryBootstrapFirstFloorPlayer(player);
                        if ((object)bootstrappedPlayer != null)
                        {
                            player = bootstrappedPlayer;
                            LogInfo("First-floor player bootstrap completed. " + DescribePlayerState(player));
                        }
                        else
                        {
                            LogWarning("First-floor player bootstrap did not produce a replacement player.");
                        }
                    }

                    string readinessSummary;
                    if (!IsFloorReadyForBossRush(player, dungeon, out readinessSummary))
                    {
                        readyFrames = 0;
                        lastReadyRoom = null;
                        if (frame == 0 || frame % 30 == 0)
                        {
                            LogInfo("Boss Rush floor is not ready yet for handoff. " + readinessSummary + ".");
                        }
                    }
                    else
                    {
                        RoomHandler currentRoom = player.CurrentRoom;
                        if ((object)currentRoom != (object)lastReadyRoom)
                        {
                            lastReadyRoom = currentRoom;
                            readyFrames = 0;
                        }

                        readyFrames++;
                        if (frame == 0 || frame % 30 == 0 || readyFrames == RequiredReadyFrames)
                        {
                            LogInfo(
                                "Boss Rush floor ready check " +
                                readyFrames +
                                "/" +
                                RequiredReadyFrames +
                                ". " +
                                readinessSummary +
                                ".");
                        }

                        if (readyFrames < RequiredReadyFrames)
                        {
                            yield return null;
                            continue;
                        }

                        string scanSummary;
                        RoomHandler bossRoom;
                        RoomHandler stagingRoom;
                        FindBossRushRooms(dungeon, out bossRoom, out stagingRoom, out scanSummary);
                        if ((object)bossRoom != null)
                        {
                            _currentBossRoom = bossRoom;
                            RoomHandler targetRoom = stagingRoom ?? bossRoom;
                            bool usedStagingRoom = (object)stagingRoom != null;
                            yield return ManualTeleportToRoom_CR(player, targetRoom);
                            if (!usedStagingRoom)
                            {
                                _state = BossRushState.InEncounter;
                            }

                            _activeCoroutine = null;
                            LogInfo(
                                "Teleported to " +
                                (usedStagingRoom ? "boss staging room" : "boss-room fallback") +
                                " for " +
                                GetCurrentFloorLabel() +
                                ". Target=" +
                                GetRoomDebugLabel(targetRoom) +
                                ", Boss=" +
                                GetRoomDebugLabel(bossRoom) +
                                ", Scan=" +
                                scanSummary +
                                ".");
                            yield break;
                        }

                        if (frame == 0 || frame % 30 == 0)
                        {
                            LogWarning("Boss handoff target not ready yet for " + GetCurrentFloorLabel() + ". Scan=" + scanSummary + ".");
                        }
                    }
                }

                yield return null;
            }

            _activeCoroutine = null;
            LogWarning("Failed to locate a boss room for " + GetCurrentFloorLabel() + ".");
            RaiseStatus(GrantCommandExecutionResult.Localized(false, "result.boss_rush.teleport_failed", GetCurrentFloorLabel()));
            BeginReturnToCharacterSelect();
        }

        private void BeginReturnToCharacterSelect()
        {
            StopActiveCoroutine();
            _currentBossRoom = null;
            _hasClaimedRewardThisEncounter = false;
            _state = BossRushState.ReturningToCharacterSelect;
            GameManager.PlayerPrefabForNewGame = null;

            if ((object)GameManager.Instance != null)
            {
                LogInfo("Returning to the character select hub.");
                if ((object)Pixelator.Instance != null)
                {
                    Pixelator.Instance.FadeToBlack(ReturnToCharacterSelectDelaySeconds, false, 0f);
                }

                GameManager.Instance.DelayedLoadCharacterSelect(ReturnToCharacterSelectDelaySeconds);
                return;
            }

            Reset();
        }

        private void Reset()
        {
            StopActiveCoroutine();
            _state = BossRushState.Idle;
            _currentEncounterIndex = -1;
            _currentBossRoom = null;
            _hasClaimedRewardThisEncounter = false;
            _hasAttemptedPlayerBootstrapThisFloor = false;
            _hasStartedSession = false;
            _selectedPlayerPrefab = null;
            _selectedCharacterLabel = string.Empty;
            GameManager.PlayerPrefabForNewGame = null;
        }

        private BossRushEncounter GetCurrentEncounter()
        {
            return _currentEncounterIndex >= 0 && _currentEncounterIndex < Encounters.Length
                ? Encounters[_currentEncounterIndex]
                : null;
        }

        private void StartActiveCoroutine(IEnumerator routine)
        {
            StopActiveCoroutine();
            if (routine == null || ETGMod.StartGlobalCoroutine == null)
            {
                return;
            }

            _activeCoroutine = ETGMod.StartGlobalCoroutine(routine);
        }

        private void StopActiveCoroutine()
        {
            if ((object)_activeCoroutine != null && ETGMod.StopGlobalCoroutine != null)
            {
                ETGMod.StopGlobalCoroutine(_activeCoroutine);
            }

            _activeCoroutine = null;
        }

        private bool TryAdvanceEncounterForObservedScene(string sceneName, string source)
        {
            if (!IsActive || _state != BossRushState.Transitioning || string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            int nextEncounterIndex = _currentEncounterIndex + 1;
            if (nextEncounterIndex >= Encounters.Length)
            {
                return false;
            }

            BossRushEncounter nextEncounter = Encounters[nextEncounterIndex];
            if (!string.Equals(sceneName, nextEncounter.SceneName, StringComparison.Ordinal))
            {
                return false;
            }

            _currentEncounterIndex = nextEncounterIndex;
            _currentBossRoom = null;
            _hasClaimedRewardThisEncounter = false;
            _hasAttemptedPlayerBootstrapThisFloor = false;
            _state = BossRushState.LoadingFloor;
            LogInfo(
                "Accepted vanilla floor transition into " +
                nextEncounter.SceneName +
                " (" +
                nextEncounter.FloorKey +
                ") via " +
                source +
                ".");
            return true;
        }
    }
}
