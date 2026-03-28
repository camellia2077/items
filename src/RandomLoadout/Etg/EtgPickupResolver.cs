using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class EtgPickupResolver
    {
        private const int FallbackScanLimit = 2048;

        public EtgPickupCatalogEntry[] GetGrantablePickupCatalog()
        {
            List<EtgPickupCatalogEntry> entries = new List<EtgPickupCatalogEntry>();
            foreach (PickupObject pickup in EnumeratePickups())
            {
                if ((object)pickup == null)
                {
                    continue;
                }

                PickupCategory? category = GetPickupCategory(pickup);
                if (!category.HasValue)
                {
                    continue;
                }

                entries.Add(
                    new EtgPickupCatalogEntry(
                        category.Value,
                        pickup.PickupObjectId,
                        GetPickupLabel(pickup),
                        pickup.name ?? string.Empty,
                        GetEncounterGuid(pickup),
                        GetItemQualityLabel(pickup),
                        pickup.PurchasePrice,
                        pickup.CanBeDropped,
                        pickup.CanBeSold,
                        pickup.encounterTrackable != null && pickup.encounterTrackable.SuppressInInventory,
                        GetPrimaryDisplayName(pickup),
                        GetNotificationDescription(pickup),
                        GetAmmonomiconFullEntry(pickup),
                        GetContentSourceLabel(pickup),
                        pickup.ForcedPositionInAmmonomicon,
                        GetGunClassLabel(pickup as Gun),
                        GetIntMemberValue(pickup, "ammo", 0),
                        GetBoolMemberValue(pickup, "CanGainAmmo", false),
                        GetBoolMemberValue(pickup, "LocalInfiniteAmmo", false),
                        GetFloatMemberValue(pickup, "reloadTime", 0f),
                        GetIntMemberValue(pickup, "numberOfUses", 0),
                        GetFloatMemberValue(pickup, "timeCooldown", 0f),
                        GetFloatMemberValue(pickup, "damageCooldown", 0f),
                        GetIntMemberValue(pickup, "roomCooldown", 0)));
            }

            entries.Sort(CompareCatalogEntries);
            return entries.ToArray();
        }

        public EtgPickupResolveResult Resolve(PickupCategory category, string pickupName)
        {
            return ResolveInternal(category, pickupName, false);
        }

        public EtgPickupResolveResult Resolve(PickupCategory category, int pickupId)
        {
            return ResolveByIdInternal(category, pickupId, false);
        }

        public EtgPickupResolveResult ResolveAny(string pickupName)
        {
            return ResolveInternal(null, pickupName, true);
        }

        public EtgPickupResolveResult ResolveAny(int pickupId)
        {
            return ResolveByIdInternal(null, pickupId, true);
        }

        public EtgPickupResolveResult ResolveRandomGrantable(int seed)
        {
            List<PickupObject> candidates = new List<PickupObject>();
            foreach (PickupObject pickup in EnumeratePickups())
            {
                if ((object)pickup == null)
                {
                    continue;
                }

                PickupCategory? category = GetPickupCategory(pickup);
                if (!category.HasValue)
                {
                    continue;
                }

                candidates.Add(pickup);
            }

            if (candidates.Count == 0)
            {
                return Failure(null, "RandomPickupUnavailable", "No supported pickups were available for random grant.");
            }

            Random random = new Random(seed);
            PickupObject match = candidates[random.Next(candidates.Count)];
            PickupCategory? resolvedCategory = GetPickupCategory(match);
            return new EtgPickupResolveResult(true, resolvedCategory, match.PickupObjectId, GetPickupLabel(match), null);
        }

        private static EtgPickupResolveResult ResolveInternal(PickupCategory? category, string pickupName, bool allowAnyCategory)
        {
            if (string.IsNullOrEmpty(pickupName))
            {
                return Failure(category, "SpecificNameNotFound", "The configured specific pickup name was empty.");
            }

            List<PickupObject> matches = new List<PickupObject>();
            foreach (PickupObject pickup in EnumeratePickups())
            {
                if ((object)pickup == null)
                {
                    continue;
                }

                if (!string.Equals(GetPickupLabel(pickup), pickupName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!allowAnyCategory && category.HasValue && !MatchesCategory(category.Value, pickup))
                {
                    continue;
                }

                matches.Add(pickup);
            }

            if (matches.Count == 0)
            {
                return Failure(category, "SpecificNameNotFound", "No pickup matched the configured specific pickup name '" + pickupName + "'.");
            }

            if (matches.Count > 1)
            {
                return Failure(category, "SpecificNameAmbiguous", "Multiple pickups matched the configured specific pickup name '" + pickupName + "'. Use an alias or pickup ID instead.");
            }

            PickupObject match = matches[0];
            PickupCategory? resolvedCategory = GetPickupCategory(match);
            if (!resolvedCategory.HasValue)
            {
                return Failure(category, "SpecificCategoryMismatch", "The configured specific pickup '" + pickupName + "' was not a supported grantable category.");
            }

            if (!allowAnyCategory && category.HasValue && resolvedCategory.Value != category.Value)
            {
                return Failure(category, "SpecificCategoryMismatch", "The configured specific pickup '" + pickupName + "' did not match the expected category.");
            }

            return new EtgPickupResolveResult(true, resolvedCategory, match.PickupObjectId, GetPickupLabel(match), null);
        }

        private static EtgPickupResolveResult ResolveByIdInternal(PickupCategory? category, int pickupId, bool allowAnyCategory)
        {
            PickupObject pickup = PickupObjectDatabase.GetById(pickupId);
            if ((object)pickup == null)
            {
                return Failure(category, "InvalidPickupId", "No pickup matched the configured pickup ID '" + pickupId + "'.");
            }

            PickupCategory? resolvedCategory = GetPickupCategory(pickup);
            if (!resolvedCategory.HasValue)
            {
                return Failure(category, "InvalidPickupId", "The configured pickup ID '" + pickupId + "' was not a supported grantable category.");
            }

            if (!allowAnyCategory && category.HasValue && resolvedCategory.Value != category.Value)
            {
                return Failure(category, "PickupCategoryMismatch", "The configured pickup ID '" + pickupId + "' did not match the expected category.");
            }

            return new EtgPickupResolveResult(true, resolvedCategory, pickup.PickupObjectId, GetPickupLabel(pickup), null);
        }

        private static EtgPickupResolveResult Failure(PickupCategory? category, string code, string message)
        {
            return new EtgPickupResolveResult(false, category, 0, string.Empty, new SelectionWarning(category, code, message));
        }

        private static IEnumerable<PickupObject> EnumeratePickups()
        {
            HashSet<int> seenIds = new HashSet<int>();
            IEnumerable objects = GetDatabaseObjects();
            if (objects != null)
            {
                foreach (object entry in objects)
                {
                    PickupObject pickup = entry as PickupObject;
                    if ((object)pickup == null || !seenIds.Add(pickup.PickupObjectId))
                    {
                        continue;
                    }

                    yield return pickup;
                }

                yield break;
            }

            for (int pickupId = 0; pickupId < FallbackScanLimit; pickupId++)
            {
                PickupObject pickup = PickupObjectDatabase.GetById(pickupId);
                if ((object)pickup == null || !seenIds.Add(pickup.PickupObjectId))
                {
                    continue;
                }

                yield return pickup;
            }
        }

        private static IEnumerable GetDatabaseObjects()
        {
            object database = GetStaticMemberValue(typeof(PickupObjectDatabase), "Instance");
            if (database == null)
            {
                return null;
            }

            object objects = GetInstanceMemberValue(database, "Objects");
            if (objects == null)
            {
                objects = GetInstanceMemberValue(database, "objects");
            }

            return objects as IEnumerable;
        }

        private static object GetStaticMemberValue(Type type, string memberName)
        {
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(null, null);
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(null);
            }

            return null;
        }

        private static object GetInstanceMemberValue(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(target, null);
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(target);
            }

            return null;
        }

        private static bool MatchesCategory(PickupCategory category, PickupObject pickup)
        {
            PickupCategory? resolvedCategory = GetPickupCategory(pickup);
            return resolvedCategory.HasValue && resolvedCategory.Value == category;
        }

        private static int CompareCatalogEntries(EtgPickupCatalogEntry left, EtgPickupCatalogEntry right)
        {
            int categoryComparison = left.Category.CompareTo(right.Category);
            if (categoryComparison != 0)
            {
                return categoryComparison;
            }

            int labelComparison = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            if (labelComparison != 0)
            {
                return labelComparison;
            }

            return left.PickupId.CompareTo(right.PickupId);
        }

        private static PickupCategory? GetPickupCategory(PickupObject pickup)
        {
            if (pickup is Gun)
            {
                return PickupCategory.Gun;
            }

            if (pickup is PassiveItem)
            {
                return PickupCategory.Passive;
            }

            if (pickup is PlayerItem)
            {
                return PickupCategory.Active;
            }

            return null;
        }

        private static string GetPickupLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return "<null>";
            }

            if (pickup.encounterTrackable != null)
            {
                string modifiedDisplayName = pickup.encounterTrackable.GetModifiedDisplayName();
                if (!string.IsNullOrEmpty(modifiedDisplayName))
                {
                    return ResolveLocalizedLabel(modifiedDisplayName);
                }

                if (pickup.encounterTrackable.journalData != null &&
                    !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
                {
                    return ResolveLocalizedLabel(pickup.encounterTrackable.journalData.PrimaryDisplayName);
                }
            }

            if (!string.IsNullOrEmpty(pickup.DisplayName))
            {
                return ResolveLocalizedLabel(pickup.DisplayName);
            }

            return ResolveLocalizedLabel(pickup.name);
        }

        private static string GetEncounterGuid(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null)
            {
                return string.Empty;
            }

            return pickup.encounterTrackable.EncounterGuid ?? string.Empty;
        }

        private static string GetPrimaryDisplayName(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null || pickup.encounterTrackable.journalData == null)
            {
                return string.Empty;
            }

            return ResolveLocalizedLabel(pickup.encounterTrackable.journalData.PrimaryDisplayName);
        }

        private static string GetNotificationDescription(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null || pickup.encounterTrackable.journalData == null)
            {
                return string.Empty;
            }

            return ResolveLocalizedLabel(pickup.encounterTrackable.journalData.NotificationPanelDescription);
        }

        private static string GetAmmonomiconFullEntry(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null || pickup.encounterTrackable.journalData == null)
            {
                return string.Empty;
            }

            return ResolveLocalizedLabel(pickup.encounterTrackable.journalData.AmmonomiconFullEntry);
        }

        private static string GetItemQualityLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return string.Empty;
            }

            return pickup.quality.ToString();
        }

        private static string GetContentSourceLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return string.Empty;
            }

            return pickup.contentSource.ToString();
        }

        private static string GetGunClassLabel(Gun gun)
        {
            if ((object)gun == null)
            {
                return string.Empty;
            }

            return gun.gunClass.ToString();
        }

        private static int GetIntMemberValue(object target, string memberName, int defaultValue)
        {
            object value = GetInstanceMemberValue(target, memberName);
            return value is int ? (int)value : defaultValue;
        }

        private static float GetFloatMemberValue(object target, string memberName, float defaultValue)
        {
            object value = GetInstanceMemberValue(target, memberName);
            return value is float ? (float)value : defaultValue;
        }

        private static bool GetBoolMemberValue(object target, string memberName, bool defaultValue)
        {
            object value = GetInstanceMemberValue(target, memberName);
            return value is bool ? (bool)value : defaultValue;
        }

        private static string ResolveLocalizedLabel(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel))
            {
                return string.Empty;
            }

            if (!rawLabel.StartsWith("#", StringComparison.Ordinal))
            {
                return rawLabel;
            }

            string localized = StringTableManager.GetString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, StringComparison.Ordinal))
            {
                return localized;
            }

            localized = StringTableManager.GetItemsString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, StringComparison.Ordinal))
            {
                return localized;
            }

            return rawLabel;
        }
    }
}
