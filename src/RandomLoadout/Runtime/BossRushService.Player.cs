using System;
using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class BossRushService
    {
        private PlayerController TryBootstrapFirstFloorPlayer(PlayerController currentPlayer)
        {
            if ((object)currentPlayer == null)
            {
                return null;
            }

            if ((object)_selectedPlayerPrefab == null)
            {
                CaptureSelectedCharacterProfile();
            }

            if ((object)_selectedPlayerPrefab == null)
            {
                LogWarning("Could not resolve a selected character prefab for first-floor bootstrap.");
                return null;
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null)
            {
                return null;
            }

            Vector3 spawnPosition = currentPlayer.transform.position;
            bool usedRandomGuns = currentPlayer.CharacterUsesRandomGuns;
            PlayerController selectedPlayer = InstantiateSelectedCharacterPrefab(spawnPosition, usedRandomGuns);
            if ((object)selectedPlayer == null)
            {
                return null;
            }

            gameManager.ClearPrimaryPlayer();
            gameManager.PrimaryPlayer = selectedPlayer;
            selectedPlayer.PlayerIDX = 0;
            if ((object)gameManager.MainCameraController != null)
            {
                gameManager.MainCameraController.ClearPlayerCache();
                gameManager.MainCameraController.SetManualControl(false, true);
            }

            CleanupExtraPlayers(selectedPlayer, gameManager.SecondaryPlayer, currentPlayer);
            gameManager.RefreshAllPlayers();
            return selectedPlayer;
        }

        private PlayerController InstantiateSelectedCharacterPrefab(Vector3 spawnPosition, bool usedRandomGuns)
        {
            PlayerController prefabController = _selectedPlayerPrefab.GetComponent<PlayerController>();
            if ((object)prefabController == null)
            {
                LogWarning("Selected character prefab is missing PlayerController: " + _selectedPlayerPrefab.name + ".");
                return null;
            }

            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats != null)
            {
                stats.BeginNewSession(prefabController);
            }

            GameObject playerObject = null;
            try
            {
                GameManager.PlayerPrefabForNewGame = _selectedPlayerPrefab;
                playerObject = UnityEngine.Object.Instantiate(_selectedPlayerPrefab, spawnPosition, Quaternion.identity) as GameObject;
            }
            finally
            {
                GameManager.PlayerPrefabForNewGame = null;
            }

            if ((object)playerObject == null)
            {
                LogWarning("Failed to instantiate selected character prefab for Boss Rush: " + _selectedPlayerPrefab.name + ".");
                return null;
            }

            playerObject.SetActive(true);
            PlayerController selectedPlayer = playerObject.GetComponent<PlayerController>();
            if ((object)selectedPlayer == null)
            {
                UnityEngine.Object.Destroy(playerObject);
                LogWarning("Instantiated player object is missing PlayerController for Boss Rush bootstrap.");
                return null;
            }

            selectedPlayer.CharacterUsesRandomGuns = usedRandomGuns;
            return selectedPlayer;
        }

        private static void CleanupExtraPlayers(PlayerController selectedPlayer, PlayerController coopPlayer, PlayerController oldPrimaryPlayer)
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

                if ((object)player == (object)oldPrimaryPlayer)
                {
                    LogInfoStatic(player, "Destroying old PrimaryPlayer after Boss Rush bootstrap.");
                }

                UnityEngine.Object.Destroy(player.gameObject);
            }
        }

        private void CaptureSelectedCharacterProfile()
        {
            PlayerController currentPlayer = GameManager.Instance != null ? GameManager.Instance.PrimaryPlayer : null;
            _selectedCharacterLabel = ResolveCharacterLabel(currentPlayer);
            _selectedPlayerPrefab = LoadCharacterPrefabForLabel(_selectedCharacterLabel);

            LogInfo(
                "Captured Boss Rush character profile. Label=" +
                GetCharacterLabel(_selectedCharacterLabel) +
                ", PrefabFound=" +
                ((object)_selectedPlayerPrefab != null) +
                ", Player=" +
                DescribePlayerState(currentPlayer) +
                ".");
        }

        private void BeginBossRushSession()
        {
            if (_hasStartedSession)
            {
                return;
            }

            PlayerController prefabController = _selectedPlayerPrefab != null ? _selectedPlayerPrefab.GetComponent<PlayerController>() : null;
            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats != null && (object)prefabController != null)
            {
                stats.BeginNewSession(prefabController);
                _hasStartedSession = true;
                LogInfo("Started Boss Rush session for " + GetCharacterLabel(_selectedCharacterLabel) + ".");
                return;
            }

            if ((object)stats == null)
            {
                LogWarning("GameStatsManager was not available when starting Boss Rush session.");
            }
            else
            {
                LogWarning("Could not start Boss Rush session because the selected character prefab controller was unavailable.");
            }
        }

        private static bool HasUsableGun(PlayerController player)
        {
            if ((object)player == null || (object)player.inventory == null || player.inventory.AllGuns == null)
            {
                return false;
            }

            return player.inventory.AllGuns.Count > 0 && (object)player.CurrentGun != null;
        }

        private static string DescribePlayerState(PlayerController player)
        {
            if ((object)player == null)
            {
                return "<null>";
            }

            int gunCount = 0;
            if ((object)player.inventory != null && player.inventory.AllGuns != null)
            {
                gunCount = player.inventory.AllGuns.Count;
            }

            string currentGunName = player.CurrentGun != null ? GetPickupLabel(player.CurrentGun) : "<none>";
            string roomName = player.CurrentRoom != null ? GetRoomDebugLabel(player.CurrentRoom) : "<none>";
            string characterLabel = ResolveCharacterLabel(player);
            return "Name=" +
                   player.name +
                   ", Character=" +
                   GetCharacterLabel(characterLabel) +
                   ", Guns=" +
                   gunCount +
                   ", CurrentGun=" +
                   currentGunName +
                   ", Room=" +
                   roomName;
        }

        private static string GetPickupLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return "<null>";
            }

            if (pickup.encounterTrackable != null &&
                pickup.encounterTrackable.journalData != null &&
                !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
            {
                return pickup.encounterTrackable.journalData.PrimaryDisplayName;
            }

            return pickup.name;
        }

        private static string ResolveCharacterLabel(PlayerController player)
        {
            if ((object)player == null)
            {
                return string.Empty;
            }

            string reflectedIdentity = ReadCharacterIdentityToken(player);
            string resolved = NormalizeCharacterLabel(reflectedIdentity);
            if (!string.IsNullOrEmpty(resolved))
            {
                return resolved;
            }

            return NormalizeCharacterLabel(player.name);
        }

        private static string ReadCharacterIdentityToken(PlayerController player)
        {
            if ((object)player == null)
            {
                return string.Empty;
            }

            try
            {
                Type playerType = player.GetType();
                PropertyInfo property = playerType.GetProperty("characterIdentity", InstanceFlags);
                if (property != null)
                {
                    object value = property.GetValue(player, null);
                    return value != null ? value.ToString() : string.Empty;
                }

                FieldInfo field = playerType.GetField("characterIdentity", InstanceFlags);
                if (field != null)
                {
                    object value = field.GetValue(player);
                    return value != null ? value.ToString() : string.Empty;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string NormalizeCharacterLabel(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return string.Empty;
            }

            string value = rawValue.Replace("(Clone)", string.Empty).Trim();
            string lower = value.ToLowerInvariant();
            if (lower.IndexOf("marine") >= 0 || lower.IndexOf("soldier") >= 0)
            {
                return "Marine";
            }

            if (lower.IndexOf("hunter") >= 0 || lower.IndexOf("guide") >= 0)
            {
                return "Hunter";
            }

            if (lower.IndexOf("pilot") >= 0 || lower.IndexOf("rogue") >= 0)
            {
                return "Pilot";
            }

            if (lower.IndexOf("convict") >= 0 || lower.IndexOf("ninja") >= 0)
            {
                return "Convict";
            }

            if (lower.IndexOf("robot") >= 0)
            {
                return "Robot";
            }

            if (lower.IndexOf("bullet") >= 0)
            {
                return "Bullet";
            }

            if (lower.IndexOf("eevee") >= 0 || lower.IndexOf("paradox") >= 0)
            {
                return "Paradox";
            }

            if (lower.IndexOf("gunslinger") >= 0)
            {
                return "Gunslinger";
            }

            return string.Empty;
        }

        private static GameObject LoadCharacterPrefabForLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return null;
            }

            switch (label)
            {
                case "Marine":
                    return LoadCharacterPrefab("marine");
                case "Hunter":
                    return LoadCharacterPrefab("guide");
                case "Pilot":
                    return LoadCharacterPrefab("pilot");
                case "Convict":
                    return LoadCharacterPrefab("convict");
                case "Robot":
                    return LoadCharacterPrefab("robot");
                case "Bullet":
                    return LoadCharacterPrefab("bullet");
                case "Paradox":
                    return LoadCharacterPrefab("eevee");
                case "Gunslinger":
                    return LoadCharacterPrefab("gunslinger");
                default:
                    return null;
            }
        }

        private static GameObject LoadCharacterPrefab(params string[] prefabSuffixes)
        {
            if (prefabSuffixes == null || prefabSuffixes.Length == 0)
            {
                return null;
            }

            for (int suffixIndex = 0; suffixIndex < prefabSuffixes.Length; suffixIndex++)
            {
                string prefabSuffix = prefabSuffixes[suffixIndex];
                if (string.IsNullOrEmpty(prefabSuffix))
                {
                    continue;
                }

                string[] candidateNames = new[]
                {
                    "Player" + prefabSuffix,
                    "Player" + prefabSuffix.ToLowerInvariant(),
                    "Player" + char.ToUpperInvariant(prefabSuffix[0]) + prefabSuffix.Substring(1),
                };

                for (int i = 0; i < candidateNames.Length; i++)
                {
                    string candidate = candidateNames[i];
                    GameObject prefab = BraveResources.Load(candidate, ".prefab") as GameObject;
                    if ((object)prefab == null)
                    {
                        prefab = Resources.Load(candidate) as GameObject;
                    }

                    if ((object)prefab != null)
                    {
                        return prefab;
                    }
                }
            }

            return null;
        }

        private static string GetCharacterLabel(string label)
        {
            return !string.IsNullOrEmpty(label) ? label : "<unknown>";
        }
    }
}
