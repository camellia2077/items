using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class FoyerCharacterSwitchService
    {
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

    }
}
