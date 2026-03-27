using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class LoadoutConfigResolutionResult
    {
        public LoadoutConfigResolutionResult(LoadoutConfig config, SelectionWarning[] warnings)
        {
            Config = config;
            Warnings = warnings;
        }

        public LoadoutConfig Config { get; private set; }

        public SelectionWarning[] Warnings { get; private set; }
    }
}
