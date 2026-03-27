using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class EtgPickupGranter
    {
        public EtgGrantOutcome Grant(PlayerController player, SelectedPickup selection)
        {
            PickupObject pickup = PickupObjectDatabase.GetById(selection.PickupId);
            if ((object)pickup == null)
            {
                return new EtgGrantOutcome(selection.Category, selection.PickupId, "<missing>", false, "PickupObjectDatabase returned null.");
            }

            string pickupLabel = GetPickupLabel(pickup);
            if (!MatchesCategory(selection.Category, pickup))
            {
                return new EtgGrantOutcome(selection.Category, selection.PickupId, pickupLabel, false, "Selected pickup does not match the expected category.");
            }

            if (!GrantPickup(player, selection.Category, pickup))
            {
                return new EtgGrantOutcome(selection.Category, selection.PickupId, pickupLabel, false, "The ETG grant call returned false.");
            }

            return new EtgGrantOutcome(selection.Category, selection.PickupId, pickupLabel, true, string.Empty);
        }

        private static bool MatchesCategory(PickupCategory category, PickupObject pickup)
        {
            switch (category)
            {
                case PickupCategory.Gun:
                    return pickup is Gun;
                case PickupCategory.Passive:
                    return pickup is PassiveItem;
                case PickupCategory.Active:
                    return pickup is PlayerItem;
                default:
                    return false;
            }
        }

        private static bool GrantPickup(PlayerController player, PickupCategory category, PickupObject pickup)
        {
            switch (category)
            {
                case PickupCategory.Gun:
                    player.inventory.AddGunToInventory((Gun)pickup, false);
                    return true;
                case PickupCategory.Passive:
                    player.AcquirePassiveItem((PassiveItem)pickup);
                    return true;
                case PickupCategory.Active:
                    Component component = pickup as Component;
                    return component != null && LootEngine.TryGivePrefabToPlayer(component.gameObject, player, false);
                default:
                    return false;
            }
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
    }
}
