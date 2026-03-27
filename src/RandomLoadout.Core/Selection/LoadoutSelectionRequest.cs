using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core
{
    public sealed class LoadoutSelectionRequest
    {
        public LoadoutSelectionRequest(int seed, LoadoutConfig config, IEnumerable<int> ownedPickupIds)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            Seed = seed;
            Config = config;
            OwnedPickupIds = ownedPickupIds != null ? ownedPickupIds.ToArray() : new int[0];
        }

        public int Seed { get; private set; }

        public LoadoutConfig Config { get; private set; }

        public int[] OwnedPickupIds { get; private set; }
    }
}
