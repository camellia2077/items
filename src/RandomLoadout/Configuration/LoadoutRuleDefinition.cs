using System;
using System.Collections.Generic;
using System.Linq;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class LoadoutRuleDefinition
    {
        private LoadoutRuleDefinition(
            PickupCategory category,
            GrantMode mode,
            int count,
            IEnumerable<int> poolIds,
            string specificName)
        {
            Category = category;
            Mode = mode;
            Count = count;
            PoolIds = poolIds != null ? poolIds.ToArray() : new int[0];
            SpecificName = specificName ?? string.Empty;
        }

        public PickupCategory Category { get; private set; }

        public GrantMode Mode { get; private set; }

        public int Count { get; private set; }

        public int[] PoolIds { get; private set; }

        public string SpecificName { get; private set; }

        public static LoadoutRuleDefinition Random(PickupCategory category, int count, IEnumerable<int> poolIds)
        {
            if (poolIds == null)
            {
                throw new ArgumentNullException("poolIds");
            }

            return new LoadoutRuleDefinition(category, GrantMode.Random, count, poolIds, string.Empty);
        }

        public static LoadoutRuleDefinition Specific(PickupCategory category, string specificName)
        {
            if (specificName == null)
            {
                throw new ArgumentNullException("specificName");
            }

            return new LoadoutRuleDefinition(category, GrantMode.Specific, 1, null, specificName);
        }
    }
}
