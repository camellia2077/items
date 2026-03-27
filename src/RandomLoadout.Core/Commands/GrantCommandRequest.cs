namespace RandomLoadout.Core
{
    public sealed class GrantCommandRequest
    {
        public GrantCommandRequest(GrantCommandTarget target, string pickupName)
        {
            Target = target;
            PickupName = pickupName;
        }

        public GrantCommandTarget Target { get; private set; }

        public string PickupName { get; private set; }
    }
}
