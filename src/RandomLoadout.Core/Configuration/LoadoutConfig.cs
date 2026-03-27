using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core
{
    public sealed class LoadoutConfig
    {
        public LoadoutConfig(IEnumerable<LoadoutRuleConfig> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException("rules");
            }

            Rules = rules.ToArray();
        }

        public LoadoutRuleConfig[] Rules { get; private set; }
    }
}
