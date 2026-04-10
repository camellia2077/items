using System;
using System.Collections.Generic;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed partial class EtgPickupResolver
    {
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
    }
}
