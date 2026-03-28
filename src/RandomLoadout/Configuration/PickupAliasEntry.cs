namespace RandomLoadout
{
    internal sealed class PickupAliasEntry
    {
        public PickupAliasEntry(string alias, int pickupId)
        {
            Alias = alias ?? string.Empty;
            PickupId = pickupId;
        }

        public string Alias { get; private set; }

        public int PickupId { get; private set; }
    }
}
