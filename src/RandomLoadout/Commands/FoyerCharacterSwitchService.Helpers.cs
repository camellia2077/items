using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class FoyerCharacterSwitchService
    {
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
