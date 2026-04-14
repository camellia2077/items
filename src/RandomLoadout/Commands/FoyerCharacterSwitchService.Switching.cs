using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class FoyerCharacterSwitchService
    {
        private static bool CanUseForceSwitchFallback(string label)
        {
            string[] prefabSuffixes;
            return TryGetCharacterPrefabSuffixes(label, out prefabSuffixes);
        }

        private static bool TryForceSwitchCharacterInBreach(Foyer foyer, string label, out string failureMessage)
        {
            failureMessage = string.Empty;
            if ((object)foyer == null)
            {
                failureMessage = GuiText.Get("result.characters.breach_only_switch");
                return false;
            }

            string[] prefabSuffixes;
            if (!TryGetCharacterPrefabSuffixes(label, out prefabSuffixes))
            {
                failureMessage = GuiText.Get("result.characters.force_switch_not_configured", GuiText.GetCharacterLabel(label));
                return false;
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null || (object)gameManager.PrimaryPlayer == null)
            {
                failureMessage = GuiText.Get("result.common.player_not_ready");
                return false;
            }

            GameObject prefab = LoadCharacterPrefab(prefabSuffixes);
            if ((object)prefab == null)
            {
                failureMessage = GuiText.Get("result.characters.prefab_not_found", GuiText.GetCharacterLabel(label));
                return false;
            }

            PlayerController currentPlayer = gameManager.PrimaryPlayer;
            bool usedRandomGuns = currentPlayer.CharacterUsesRandomGuns;
            Vector3 spawnPosition = currentPlayer.transform.position;

            if ((object)Pixelator.Instance != null)
            {
                Pixelator.Instance.FadeToBlack(0.25f, false, 0f);
            }

            currentPlayer.SetInputOverride("randomloadout_force_character_switch");
            UnityEngine.Object.Destroy(currentPlayer.gameObject);
            gameManager.ClearPrimaryPlayer();

            GameManager.PlayerPrefabForNewGame = prefab;
            PlayerController prefabController = prefab.GetComponent<PlayerController>();
            if ((object)prefabController == null)
            {
                GameManager.PlayerPrefabForNewGame = null;
                failureMessage = GuiText.Get("result.characters.prefab_missing_controller", GuiText.GetCharacterLabel(label));
                return false;
            }

            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats != null)
            {
                stats.BeginNewSession(prefabController);
            }

            GameObject playerObject = UnityEngine.Object.Instantiate(prefab, spawnPosition, Quaternion.identity) as GameObject;
            GameManager.PlayerPrefabForNewGame = null;
            if ((object)playerObject == null)
            {
                failureMessage = GuiText.Get("result.characters.instantiate_failed", GuiText.GetCharacterLabel(label));
                return false;
            }

            playerObject.SetActive(true);
            PlayerController selectedPlayer = playerObject.GetComponent<PlayerController>();
            if ((object)selectedPlayer == null)
            {
                UnityEngine.Object.Destroy(playerObject);
                failureMessage = GuiText.Get("result.characters.controller_init_failed", GuiText.GetCharacterLabel(label));
                return false;
            }

            gameManager.PrimaryPlayer = selectedPlayer;
            selectedPlayer.PlayerIDX = 0;
            if ((object)gameManager.MainCameraController != null)
            {
                gameManager.MainCameraController.ClearPlayerCache();
                gameManager.MainCameraController.SetManualControl(false, true);
            }

            // Skip Breach character-select callbacks in switch-only mode to avoid
            // side effects such as currency costs on hidden-character selections.
            FinalizeCharacterSwitch(foyer, selectedPlayer, false);

            if (usedRandomGuns && (object)gameManager.PrimaryPlayer != null)
            {
                gameManager.PrimaryPlayer.CharacterUsesRandomGuns = true;
            }

            if ((object)Pixelator.Instance != null)
            {
                Pixelator.Instance.FadeToBlack(0.25f, true, 0f);
            }

            return true;
        }

        private static bool TryGetCharacterPrefabSuffixes(string label, out string[] prefabSuffixes)
        {
            prefabSuffixes = null;
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            switch (label)
            {
                case "Marine":
                    // In this environment the Marine character prefab is named "marine".
                    // Do not use "soldier" here or force-switch loading will fail.
                    prefabSuffixes = new[] { "marine" };
                    return true;
                case "Hunter":
                    prefabSuffixes = new[] { "guide" };
                    return true;
                case "Pilot":
                    prefabSuffixes = new[] { "pilot" };
                    return true;
                case "Convict":
                    prefabSuffixes = new[] { "convict" };
                    return true;
                case "Robot":
                    prefabSuffixes = new[] { "robot" };
                    return true;
                case "Bullet":
                    prefabSuffixes = new[] { "bullet" };
                    return true;
                case "Paradox":
                    prefabSuffixes = new[] { "eevee" };
                    return true;
                case "Gunslinger":
                    prefabSuffixes = new[] { "gunslinger" };
                    return true;
                default:
                    return false;
            }
        }

        private IEnumerator SwitchCharacterRoutine(Foyer foyer, FoyerCharacterSelectFlag flag, HashSet<int> existingPlayerIds)
        {
            yield return foyer.StartCoroutine(foyer.OnSelectedCharacter(0f, flag));

            float deadline = Time.unscaledTime + PendingSelectionTimeoutSeconds;
            PlayerController selectedPlayer = null;
            while (Time.unscaledTime < deadline)
            {
                selectedPlayer = FindNewestPlayer(existingPlayerIds);
                if ((object)selectedPlayer == null)
                {
                    GameManager gameManager = GameManager.Instance;
                    if ((object)gameManager != null)
                    {
                        selectedPlayer = gameManager.PrimaryPlayer;
                    }
                }

                if ((object)selectedPlayer != null && !Foyer.IsCurrentlyPlayingCharacterSelect)
                {
                    break;
                }

                yield return null;
            }

            if ((object)selectedPlayer != null)
            {
                FinalizeCharacterSwitch(foyer, selectedPlayer, true);
            }

            ClearPendingSelection();
        }

        private void RefreshPendingSelectionState(Foyer foyer)
        {
            if ((object)_pendingSelectionFlag == null)
            {
                return;
            }

            if ((object)foyer == null)
            {
                ClearPendingSelection();
                return;
            }

            if ((object)foyer.CurrentSelectedCharacterFlag == (object)_pendingSelectionFlag && !Foyer.IsCurrentlyPlayingCharacterSelect)
            {
                ClearPendingSelection();
                return;
            }

            if (Time.unscaledTime - _pendingSelectionStartedAt >= PendingSelectionTimeoutSeconds)
            {
                ClearPendingSelection();
            }
        }

        private void ClearPendingSelection()
        {
            _pendingSelectionFlag = null;
            _pendingSelectionStartedAt = 0f;
        }

        private static HashSet<int> CaptureCurrentPlayerInstanceIds()
        {
            HashSet<int> instanceIds = new HashSet<int>();
            PlayerController[] players = UnityEngine.Object.FindObjectsOfType(typeof(PlayerController)) as PlayerController[];
            if (players == null)
            {
                return instanceIds;
            }

            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                if ((object)player != null)
                {
                    instanceIds.Add(player.GetInstanceID());
                }
            }

            return instanceIds;
        }

        private static PlayerController FindNewestPlayer(HashSet<int> existingPlayerIds)
        {
            PlayerController[] players = UnityEngine.Object.FindObjectsOfType(typeof(PlayerController)) as PlayerController[];
            if (players == null || players.Length == 0)
            {
                return null;
            }

            PlayerController newestPlayer = null;
            int newestInstanceId = int.MinValue;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                if ((object)player == null)
                {
                    continue;
                }

                int instanceId = player.GetInstanceID();
                if (existingPlayerIds.Contains(instanceId))
                {
                    continue;
                }

                if ((object)newestPlayer == null || instanceId > newestInstanceId)
                {
                    newestPlayer = player;
                    newestInstanceId = instanceId;
                }
            }

            return newestPlayer;
        }

        private static void FinalizeCharacterSwitch(Foyer foyer, PlayerController selectedPlayer, bool notifyFoyerCharacterChanged)
        {
            if (notifyFoyerCharacterChanged && (object)foyer != null)
            {
                foyer.PlayerCharacterChanged(selectedPlayer);
            }

            GameManager gameManager = GameManager.Instance;
            PlayerController coopPlayer = null;
            if ((object)gameManager != null)
            {
                coopPlayer = gameManager.SecondaryPlayer;
                gameManager.PrimaryPlayer = selectedPlayer;
            }

            CleanupExtraPlayers(selectedPlayer, coopPlayer);

            if ((object)gameManager != null)
            {
                gameManager.RefreshAllPlayers();
            }
        }

        private static void CleanupExtraPlayers(PlayerController selectedPlayer, PlayerController coopPlayer)
        {
            PlayerController[] players = UnityEngine.Object.FindObjectsOfType(typeof(PlayerController)) as PlayerController[];
            if (players == null)
            {
                return;
            }

            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                if ((object)player == null)
                {
                    continue;
                }

                if ((object)player == (object)selectedPlayer || (object)player == (object)coopPlayer)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(player.gameObject);
            }
        }

    }
}
