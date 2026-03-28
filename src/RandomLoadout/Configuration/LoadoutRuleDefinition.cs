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
            IEnumerable<string> poolAliases,
            IEnumerable<string> poolNames,
            string specificAlias,
            string specificName,
            int? specificPickupId)
        {
            Category = category;
            Mode = mode;
            Count = count;
            PoolIds = poolIds != null ? poolIds.ToArray() : new int[0];
            PoolAliases = poolAliases != null ? poolAliases.ToArray() : new string[0];
            PoolNames = poolNames != null ? poolNames.ToArray() : new string[0];
            SpecificAlias = specificAlias ?? string.Empty;
            SpecificName = specificName ?? string.Empty;
            SpecificPickupId = specificPickupId;
        }

        public PickupCategory Category { get; private set; }

        public GrantMode Mode { get; private set; }

        public int Count { get; private set; }

        public int[] PoolIds { get; private set; }

        public string[] PoolAliases { get; private set; }

        public string[] PoolNames { get; private set; }

        public string SpecificAlias { get; private set; }

        public string SpecificName { get; private set; }

        public int? SpecificPickupId { get; private set; }

        public static LoadoutRuleDefinition Random(PickupCategory category, int count, IEnumerable<int> poolIds)
        {
            if (poolIds == null)
            {
                throw new ArgumentNullException("poolIds");
            }

            return new LoadoutRuleDefinition(category, GrantMode.Random, count, poolIds, null, null, string.Empty, string.Empty, null);
        }

        public static LoadoutRuleDefinition Random(PickupCategory category, int count, IEnumerable<int> poolIds, IEnumerable<string> poolAliases, IEnumerable<string> poolNames)
        {
            if (poolIds == null)
            {
                throw new ArgumentNullException("poolIds");
            }

            if (poolAliases == null)
            {
                throw new ArgumentNullException("poolAliases");
            }

            if (poolNames == null)
            {
                throw new ArgumentNullException("poolNames");
            }

            return new LoadoutRuleDefinition(category, GrantMode.Random, count, poolIds, poolAliases, poolNames, string.Empty, string.Empty, null);
        }

        public static LoadoutRuleDefinition RandomByAlias(PickupCategory category, int count, IEnumerable<string> poolAliases)
        {
            if (poolAliases == null)
            {
                throw new ArgumentNullException("poolAliases");
            }

            return new LoadoutRuleDefinition(category, GrantMode.Random, count, null, poolAliases, null, string.Empty, string.Empty, null);
        }

        public static LoadoutRuleDefinition RandomByName(PickupCategory category, int count, IEnumerable<string> poolNames)
        {
            if (poolNames == null)
            {
                throw new ArgumentNullException("poolNames");
            }

            return new LoadoutRuleDefinition(category, GrantMode.Random, count, null, null, poolNames, string.Empty, string.Empty, null);
        }

        public static LoadoutRuleDefinition SpecificByAlias(PickupCategory category, string specificAlias)
        {
            if (specificAlias == null)
            {
                throw new ArgumentNullException("specificAlias");
            }

            return new LoadoutRuleDefinition(category, GrantMode.Specific, 1, null, null, null, specificAlias, string.Empty, null);
        }

        public static LoadoutRuleDefinition Specific(PickupCategory category, string specificName)
        {
            if (specificName == null)
            {
                throw new ArgumentNullException("specificName");
            }

            return new LoadoutRuleDefinition(category, GrantMode.Specific, 1, null, null, null, string.Empty, specificName, null);
        }

        public static LoadoutRuleDefinition Specific(PickupCategory category, int specificPickupId)
        {
            return new LoadoutRuleDefinition(category, GrantMode.Specific, 1, null, null, null, string.Empty, string.Empty, specificPickupId);
        }
    }
}
