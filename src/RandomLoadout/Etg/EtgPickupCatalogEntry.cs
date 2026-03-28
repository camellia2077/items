using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class EtgPickupCatalogEntry
    {
        public EtgPickupCatalogEntry(
            PickupCategory category,
            int pickupId,
            string displayName,
            string internalName,
            string encounterGuid,
            string quality,
            int purchasePrice,
            bool canBeDropped,
            bool canBeSold,
            bool suppressInInventory,
            string primaryDisplayName,
            string shortDescription,
            string longDescription,
            string contentSource,
            int forcedPositionInAmmonomicon,
            string gunClass,
            int ammo,
            bool canGainAmmo,
            bool infiniteAmmo,
            float reloadTime,
            int activeNumberOfUses,
            float activeTimeCooldown,
            float activeDamageCooldown,
            int activeRoomCooldown)
        {
            Category = category;
            PickupId = pickupId;
            DisplayName = displayName ?? string.Empty;
            InternalName = internalName ?? string.Empty;
            EncounterGuid = encounterGuid ?? string.Empty;
            Quality = quality ?? string.Empty;
            PurchasePrice = purchasePrice;
            CanBeDropped = canBeDropped;
            CanBeSold = canBeSold;
            SuppressInInventory = suppressInInventory;
            PrimaryDisplayName = primaryDisplayName ?? string.Empty;
            ShortDescription = shortDescription ?? string.Empty;
            LongDescription = longDescription ?? string.Empty;
            ContentSource = contentSource ?? string.Empty;
            ForcedPositionInAmmonomicon = forcedPositionInAmmonomicon;
            GunClass = gunClass ?? string.Empty;
            Ammo = ammo;
            CanGainAmmo = canGainAmmo;
            InfiniteAmmo = infiniteAmmo;
            ReloadTime = reloadTime;
            ActiveNumberOfUses = activeNumberOfUses;
            ActiveTimeCooldown = activeTimeCooldown;
            ActiveDamageCooldown = activeDamageCooldown;
            ActiveRoomCooldown = activeRoomCooldown;
        }

        public PickupCategory Category { get; private set; }

        public int PickupId { get; private set; }

        public string DisplayName { get; private set; }

        public string InternalName { get; private set; }

        public string EncounterGuid { get; private set; }

        public string Quality { get; private set; }

        public int PurchasePrice { get; private set; }

        public bool CanBeDropped { get; private set; }

        public bool CanBeSold { get; private set; }

        public bool SuppressInInventory { get; private set; }

        public string PrimaryDisplayName { get; private set; }

        public string ShortDescription { get; private set; }

        public string LongDescription { get; private set; }

        public string ContentSource { get; private set; }

        public int ForcedPositionInAmmonomicon { get; private set; }

        public string GunClass { get; private set; }

        public int Ammo { get; private set; }

        public bool CanGainAmmo { get; private set; }

        public bool InfiniteAmmo { get; private set; }

        public float ReloadTime { get; private set; }

        public int ActiveNumberOfUses { get; private set; }

        public float ActiveTimeCooldown { get; private set; }

        public float ActiveDamageCooldown { get; private set; }

        public int ActiveRoomCooldown { get; private set; }
    }
}
