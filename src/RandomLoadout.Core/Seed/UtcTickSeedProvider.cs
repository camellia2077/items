using System;

namespace RandomLoadout.Core
{
    public sealed class UtcTickSeedProvider : ISeedProvider
    {
        public int CreateSeed()
        {
            return unchecked((int)DateTime.UtcNow.Ticks);
        }
    }
}
