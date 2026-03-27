namespace RandomLoadout.Core
{
    public sealed class SelectedPickup
    {
        public SelectedPickup(PickupCategory category, int pickupId)
        {
            Category = category;
            PickupId = pickupId;
        }

        public PickupCategory Category { get; private set; }

        public int PickupId { get; private set; }
    }
}
