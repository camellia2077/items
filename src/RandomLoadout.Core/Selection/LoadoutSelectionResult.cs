using System;
using System.Collections.Generic;
using System.Linq;

namespace RandomLoadout.Core
{
    public sealed class LoadoutSelectionResult
    {
        public LoadoutSelectionResult(int seed, IEnumerable<SelectedPickup> selections, IEnumerable<SelectionWarning> warnings)
        {
            if (selections == null)
            {
                throw new ArgumentNullException("selections");
            }

            if (warnings == null)
            {
                throw new ArgumentNullException("warnings");
            }

            Seed = seed;
            Selections = selections.ToArray();
            Warnings = warnings.ToArray();
        }

        public int Seed { get; private set; }

        public SelectedPickup[] Selections { get; private set; }

        public SelectionWarning[] Warnings { get; private set; }
    }
}
