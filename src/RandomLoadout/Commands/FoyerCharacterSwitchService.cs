using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class FoyerCharacterSwitchService
    {
        private const float PendingSelectionTimeoutSeconds = 5f;
        private static readonly string[] KnownCharacterLabels =
        {
            "Marine",
            "Hunter",
            "Pilot",
            "Convict",
            "Robot",
            "Bullet",
            "Paradox",
            "Gunslinger",
        };

        private FoyerCharacterSelectFlag _pendingSelectionFlag;
        private float _pendingSelectionStartedAt;

        public FoyerCharacterOption[] GetCharacterOptions()
        {
            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                ClearPendingSelection();
                return new FoyerCharacterOption[0];
            }

            RefreshPendingSelectionState(foyer);

            FoyerCharacterSelectFlag[] flags = GetCharacterFlagsForFoyer(foyer);
            List<FoyerCharacterOption> options = new List<FoyerCharacterOption>();
            string selectedLabel = GetSelectedLabel(foyer);
            for (int i = 0; i < KnownCharacterLabels.Length; i++)
            {
                string label = KnownCharacterLabels[i];
                FoyerCharacterSelectFlag flag = FindFlagForLabel(flags, label);
                bool isSelected = !string.IsNullOrEmpty(selectedLabel) &&
                    string.Equals(selectedLabel, label, StringComparison.OrdinalIgnoreCase);
                bool isPending = (object)_pendingSelectionFlag != null &&
                    (object)_pendingSelectionFlag == (object)flag;
                bool isSelectable = !_pendingSelectionFlag &&
                    (isSelected || ((object)flag != null && flag.CanBeSelected()));
                options.Add(new FoyerCharacterOption(label, isSelectable, isSelected, isPending, flag, IsUnlockableCharacter(label)));
            }

            options.Sort(CompareOptions);
            return options.ToArray();
        }

        public string GetAvailabilityStatus()
        {
            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return "Character switching is only available in the Breach.";
            }

            FoyerCharacterOption[] options = GetCharacterOptions();
            int availableCount = 0;
            int lockedCount = 0;
            for (int i = 0; i < options.Length; i++)
            {
                FoyerCharacterOption option = options[i];
                if (option.IsSelected || option.IsSelectable)
                {
                    availableCount++;
                }
                else if (option.CanUnlock)
                {
                    lockedCount++;
                }
            }

            return "Found " + availableCount + " available characters and " + lockedCount + " locked hidden characters.";
        }

        public GrantCommandExecutionResult SwitchCharacter(FoyerCharacterOption option)
        {
            return SwitchCharacterOnly(option);
        }

        public GrantCommandExecutionResult UnlockCharacter(FoyerCharacterOption option)
        {
            if (option == null)
            {
                return new GrantCommandExecutionResult(false, "The selected character option was no longer available.");
            }

            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return new GrantCommandExecutionResult(false, "Character unlocking is only available in the Breach.");
            }

            if (string.Equals(option.Label, "Robot", StringComparison.OrdinalIgnoreCase))
            {
                // Robot is intentionally excluded from unlock mode.
                // In this panel, Robot follows switch-only behavior for reliability.
                return new GrantCommandExecutionResult(false, "Robot is switch-only in this panel and cannot be unlocked here.");
            }

            if (!IsUnlockableCharacter(option.Label))
            {
                return new GrantCommandExecutionResult(false, option.Label + " does not require manual unlock.");
            }

            FoyerCharacterSelectFlag refreshedFlag = FindFlagForLabel(GetCharacterFlagsForFoyer(foyer), option.Label);
            if ((object)refreshedFlag != null && refreshedFlag.CanBeSelected())
            {
                return new GrantCommandExecutionResult(true, option.Label + " is already unlocked.");
            }

            string unlockFailureMessage;
            if (!TryUnlockCharacter(option, out unlockFailureMessage))
            {
                return new GrantCommandExecutionResult(
                    false,
                    !string.IsNullOrEmpty(unlockFailureMessage)
                        ? unlockFailureMessage
                        : option.Label + " could not be unlocked.");
            }

            return new GrantCommandExecutionResult(
                true,
                "Unlocked " + option.Label + ". Reopen Characters or restart the game to refresh availability.");
        }

        public GrantCommandExecutionResult SwitchCharacterOnly(FoyerCharacterOption option)
        {
            if (option == null)
            {
                return new GrantCommandExecutionResult(false, "The selected character option was no longer available.");
            }

            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return new GrantCommandExecutionResult(false, "Character switching is only available in the Breach.");
            }

            RefreshPendingSelectionState(foyer);

            if ((object)_pendingSelectionFlag != null)
            {
                return new GrantCommandExecutionResult(false, "Character selection is already in progress.");
            }

            if (Foyer.IsCurrentlyPlayingCharacterSelect)
            {
                return new GrantCommandExecutionResult(false, "Character selection is already in progress.");
            }

            if (option.IsSelected ||
                ((object)option.Flag != null && (object)foyer.CurrentSelectedCharacterFlag == (object)option.Flag))
            {
                return new GrantCommandExecutionResult(false, option.Label + " is already selected.");
            }

            // Switch-only mode must avoid the native character-select callback flow,
            // because that flow can trigger currency costs for some selections.
            string forceSwitchFailureMessage;
            if (TryForceSwitchCharacterInBreach(foyer, option.Label, out forceSwitchFailureMessage))
            {
                return new GrantCommandExecutionResult(true, "Switched character to " + option.Label + " (switch-only mode).");
            }

            return new GrantCommandExecutionResult(
                false,
                !string.IsNullOrEmpty(forceSwitchFailureMessage)
                    ? forceSwitchFailureMessage
                    : "Force switch failed.");
        }

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
                failureMessage = "Character switching is only available in the Breach.";
                return false;
            }

            string[] prefabSuffixes;
            if (!TryGetCharacterPrefabSuffixes(label, out prefabSuffixes))
            {
                failureMessage = "Force switch is not configured for " + label + ".";
                return false;
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null || (object)gameManager.PrimaryPlayer == null)
            {
                failureMessage = "The player is not ready yet.";
                return false;
            }

            GameObject prefab = LoadCharacterPrefab(prefabSuffixes);
            if ((object)prefab == null)
            {
                failureMessage = "Could not load the character prefab for " + label + ".";
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
                failureMessage = "The " + label + " prefab was missing a PlayerController component.";
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
                failureMessage = "Failed to instantiate the " + label + " character.";
                return false;
            }

            playerObject.SetActive(true);
            PlayerController selectedPlayer = playerObject.GetComponent<PlayerController>();
            if ((object)selectedPlayer == null)
            {
                UnityEngine.Object.Destroy(playerObject);
                failureMessage = "Failed to initialize the " + label + " character controller.";
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

        private static Foyer GetActiveFoyer()
        {
            return UnityEngine.Object.FindObjectOfType(typeof(Foyer)) as Foyer;
        }

        private static FoyerCharacterSelectFlag[] GetCharacterFlagsForFoyer(Foyer foyer)
        {
            FoyerCharacterSelectFlag[] callbackFlags = GetCharacterFlagsFromFoyerCallbacks(foyer);
            if (callbackFlags.Length > 0)
            {
                return callbackFlags;
            }

            FoyerCharacterSelectFlag[] flags = Resources.FindObjectsOfTypeAll(typeof(FoyerCharacterSelectFlag)) as FoyerCharacterSelectFlag[];
            if (flags == null || flags.Length == 0)
            {
                return new FoyerCharacterSelectFlag[0];
            }

            string foyerSceneName = GetSceneName(foyer.gameObject);
            List<FoyerCharacterSelectFlag> foyerFlags = new List<FoyerCharacterSelectFlag>();
            HashSet<int> seenInstanceIds = new HashSet<int>();
            for (int i = 0; i < flags.Length; i++)
            {
                FoyerCharacterSelectFlag flag = flags[i];
                if ((object)flag == null || (object)flag.gameObject == null)
                {
                    continue;
                }

                if (!seenInstanceIds.Add(flag.GetInstanceID()))
                {
                    continue;
                }

                if (!BelongsToFoyerScene(flag.gameObject, foyer, foyerSceneName))
                {
                    continue;
                }

                foyerFlags.Add(flag);
            }

            return foyerFlags.ToArray();
        }

        private static FoyerCharacterSelectFlag[] GetCharacterFlagsFromFoyerCallbacks(Foyer foyer)
        {
            if ((object)foyer == null)
            {
                return new FoyerCharacterSelectFlag[0];
            }

            try
            {
                MethodInfo method = typeof(Foyer).GetMethod(
                    "SetUpCharacterCallbacks",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                {
                    return new FoyerCharacterSelectFlag[0];
                }

                IList callbackFlags = method.Invoke(foyer, null) as IList;
                if (callbackFlags == null || callbackFlags.Count == 0)
                {
                    return new FoyerCharacterSelectFlag[0];
                }

                List<FoyerCharacterSelectFlag> results = new List<FoyerCharacterSelectFlag>();
                HashSet<int> seenInstanceIds = new HashSet<int>();
                for (int i = 0; i < callbackFlags.Count; i++)
                {
                    FoyerCharacterSelectFlag flag = callbackFlags[i] as FoyerCharacterSelectFlag;
                    if ((object)flag == null || !seenInstanceIds.Add(flag.GetInstanceID()))
                    {
                        continue;
                    }

                    results.Add(flag);
                }

                return results.ToArray();
            }
            catch
            {
                return new FoyerCharacterSelectFlag[0];
            }
        }

        private static bool TryUnlockCharacter(FoyerCharacterOption option, out string failureMessage)
        {
            failureMessage = string.Empty;
            if (option == null)
            {
                failureMessage = "The selected character option was no longer available.";
                return false;
            }

            GameStatsManager stats = GameStatsManager.Instance;
            if ((object)stats == null)
            {
                failureMessage = "Could not access save data to unlock " + option.Label + ".";
                return false;
            }

            GungeonFlags[] unlockFlags;
            string unlockCharacterPrefabSuffix = string.Empty;
            switch (option.Label)
            {
                case "Bullet":
                    unlockFlags = new[]
                    {
                        GungeonFlags.SECRET_BULLETMAN_SEEN_01,
                        GungeonFlags.SECRET_BULLETMAN_SEEN_02,
                        GungeonFlags.SECRET_BULLETMAN_SEEN_03,
                        GungeonFlags.SECRET_BULLETMAN_SEEN_04,
                        GungeonFlags.SECRET_BULLETMAN_SEEN_05,
                        GungeonFlags.ACHIEVEMENT_CONSTRUCT_BULLET,
                    };
                    unlockCharacterPrefabSuffix = "bullet";
                    break;
                case "Paradox":
                    unlockFlags = new[]
                    {
                        GungeonFlags.FLAG_EEVEE_UNLOCKED,
                    };
                    unlockCharacterPrefabSuffix = "eevee";
                    break;
                case "Gunslinger":
                    unlockFlags = new[]
                    {
                        GungeonFlags.GUNSLINGER_UNLOCKED,
                    };
                    unlockCharacterPrefabSuffix = "gunslinger";
                    break;
                default:
                    failureMessage = option.Label + " cannot be unlocked from this panel.";
                    return false;
            }

            ApplyUnlockFlags(stats, unlockFlags);
            // This call updates encounter-trackable unlock data. It can help visibility in some flows,
            // but it is not a guaranteed persistent character-unlock path on its own.
            TryForceUnlockCharacterPrefab(stats, unlockCharacterPrefabSuffix);
            GameStatsManager.Save();
            if (!AreAllUnlockFlagsSet(stats, unlockFlags))
            {
                failureMessage = "Failed to persist unlock flags. Verify the active save slot and try again.";
                return false;
            }

            return true;
        }

        private static void TryForceUnlockCharacterPrefab(GameStatsManager stats, string prefabSuffix)
        {
            if ((object)stats == null || string.IsNullOrEmpty(prefabSuffix))
            {
                return;
            }

            GameObject prefab = LoadCharacterPrefab(prefabSuffix);
            if ((object)prefab == null)
            {
                return;
            }

            EncounterTrackable trackable = prefab.GetComponent<EncounterTrackable>();
            if ((object)trackable == null)
            {
                trackable = prefab.GetComponentInChildren<EncounterTrackable>(true);
            }

            if ((object)trackable == null || string.IsNullOrEmpty(trackable.EncounterGuid))
            {
                return;
            }

            // ForceUnlock targets encounter progression data, not a canonical character-unlock API.
            stats.ForceUnlock(trackable.EncounterGuid);
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
                    if (string.IsNullOrEmpty(candidate))
                    {
                        continue;
                    }

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

        private static void ApplyUnlockFlags(GameStatsManager stats, GungeonFlags[] flags)
        {
            if ((object)stats == null || flags == null || flags.Length == 0)
            {
                return;
            }

            for (int i = 0; i < flags.Length; i++)
            {
                stats.SetFlag(flags[i], true);
            }

            stats.SetNextFlag(flags);
        }

        private static bool AreAllUnlockFlagsSet(GameStatsManager stats, GungeonFlags[] flags)
        {
            if ((object)stats == null || flags == null || flags.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < flags.Length; i++)
            {
                if (!stats.GetFlag(flags[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool BelongsToFoyerScene(GameObject gameObject, Foyer foyer, string foyerSceneName)
        {
            if ((object)gameObject == null || (object)foyer == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(foyerSceneName))
            {
                string sceneName = GetSceneName(gameObject);
                if (string.Equals(sceneName, foyerSceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return gameObject.transform.IsChildOf(foyer.transform) ||
                   foyer.transform.IsChildOf(gameObject.transform);
        }

        private static string GetSceneName(GameObject gameObject)
        {
            if ((object)gameObject == null)
            {
                return string.Empty;
            }

            try
            {
                object scene = gameObject.GetType().GetProperty("scene").GetValue(gameObject, null);
                if (scene == null)
                {
                    return string.Empty;
                }

                object name = scene.GetType().GetProperty("name").GetValue(scene, null);
                return name as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int CompareOptions(FoyerCharacterOption left, FoyerCharacterOption right)
        {
            int leftOrder = GetSortOrder(left);
            int rightOrder = GetSortOrder(right);
            if (leftOrder != rightOrder)
            {
                return leftOrder.CompareTo(rightOrder);
            }

            return string.Compare(left.Label, right.Label, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetSortOrder(FoyerCharacterOption option)
        {
            string label = option != null ? option.Label : string.Empty;
            switch (label)
            {
                case "Marine":
                    return 0;
                case "Hunter":
                    return 1;
                case "Pilot":
                    return 2;
                case "Convict":
                    return 3;
                case "Robot":
                    return 4;
                case "Bullet":
                    return 5;
                case "Paradox":
                    return 6;
                case "Gunslinger":
                    return 7;
                default:
                    return 100;
            }
        }

        private static string GetDisplayLabel(FoyerCharacterSelectFlag flag)
        {
            if ((object)flag == null)
            {
                return "Unknown";
            }

            if (flag.IsGunslinger)
            {
                return "Gunslinger";
            }

            if (flag.IsEevee)
            {
                return "Paradox";
            }

            string path = flag.CharacterPrefabPath ?? string.Empty;
            string lowerPath = path.ToLowerInvariant();
            if (lowerPath.IndexOf("marine") >= 0 || lowerPath.IndexOf("soldier") >= 0)
            {
                return "Marine";
            }

            if (lowerPath.IndexOf("hunter") >= 0 || lowerPath.IndexOf("guide") >= 0)
            {
                return "Hunter";
            }

            if (lowerPath.IndexOf("pilot") >= 0)
            {
                return "Pilot";
            }

            if (lowerPath.IndexOf("rogue") >= 0)
            {
                return "Pilot";
            }

            if (lowerPath.IndexOf("convict") >= 0 || lowerPath.IndexOf("ninja") >= 0)
            {
                return "Convict";
            }

            if (lowerPath.IndexOf("robot") >= 0)
            {
                return "Robot";
            }

            if (lowerPath.IndexOf("bullet") >= 0)
            {
                return "Bullet";
            }

            if (lowerPath.IndexOf("cultist") >= 0)
            {
                return "Cultist";
            }

            string rawName = !string.IsNullOrEmpty(path) ? path : flag.name;
            return CleanupLabel(rawName);
        }

        private static string CleanupLabel(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return "Unknown";
            }

            string value = rawValue.Replace("\\", "/");
            int slashIndex = value.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex + 1 < value.Length)
            {
                value = value.Substring(slashIndex + 1);
            }

            value = value.Replace("Player", string.Empty)
                         .Replace("Prefab", string.Empty)
                         .Replace("_", " ")
                         .Replace("-", " ")
                         .Trim();

            if (value.Length == 0)
            {
                return "Unknown";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current) && char.IsLower(value[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString().Trim();
        }

        private static string GetSelectedLabel(Foyer foyer)
        {
            if ((object)foyer == null || (object)foyer.CurrentSelectedCharacterFlag == null)
            {
                return string.Empty;
            }

            return GetDisplayLabel(foyer.CurrentSelectedCharacterFlag);
        }

        private static FoyerCharacterSelectFlag FindFlagForLabel(FoyerCharacterSelectFlag[] flags, string label)
        {
            if (flags == null || string.IsNullOrEmpty(label))
            {
                return null;
            }

            for (int i = 0; i < flags.Length; i++)
            {
                FoyerCharacterSelectFlag flag = flags[i];
                if ((object)flag == null || flag.IsCoopCharacter || flag.IsAlternateCostume)
                {
                    continue;
                }

                if (string.Equals(GetDisplayLabel(flag), label, StringComparison.OrdinalIgnoreCase))
                {
                    return flag;
                }
            }

            return null;
        }

        private static bool IsUnlockableCharacter(string label)
        {
            switch (label)
            {
                case "Robot":
                case "Bullet":
                case "Paradox":
                case "Gunslinger":
                    return true;
                default:
                    return false;
            }
        }
    }
}
