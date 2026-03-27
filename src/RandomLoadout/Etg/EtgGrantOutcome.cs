using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class EtgGrantOutcome
    {
        public EtgGrantOutcome(PickupCategory category, int pickupId, string pickupLabel, bool succeeded, string failureReason)
        {
            Category = category;
            PickupId = pickupId;
            PickupLabel = pickupLabel;
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public PickupCategory Category { get; private set; }

        public int PickupId { get; private set; }

        public string PickupLabel { get; private set; }

        public bool Succeeded { get; private set; }

        public string FailureReason { get; private set; }
    }
}
