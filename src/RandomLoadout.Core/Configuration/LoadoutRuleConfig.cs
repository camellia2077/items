using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core
{
    public sealed class LoadoutRuleConfig
    {
        private LoadoutRuleConfig(
            PickupCategory category,
            GrantMode mode,
            int count,
            IEnumerable<int> poolIds,
            int specificPickupId)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            Category = category;
            Mode = mode;
            Count = count;
            PoolIds = poolIds != null ? poolIds.ToArray() : new int[0];
            SpecificPickupId = specificPickupId;
        }

        public PickupCategory Category { get; private set; }

        public GrantMode Mode { get; private set; }

        public int Count { get; private set; }

        public int[] PoolIds { get; private set; }

        public int SpecificPickupId { get; private set; }

        public static LoadoutRuleConfig CreateRandom(PickupCategory category, int count, IEnumerable<int> poolIds)
        {
            if (poolIds == null)
            {
                throw new ArgumentNullException("poolIds");
            }

            return new LoadoutRuleConfig(category, GrantMode.Random, count, poolIds, 0);
        }

        public static LoadoutRuleConfig CreateSpecific(PickupCategory category, int pickupId)
        {
            return new LoadoutRuleConfig(category, GrantMode.Specific, 1, null, pickupId);
        }
    }
}
