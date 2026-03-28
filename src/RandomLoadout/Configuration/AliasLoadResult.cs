namespace RandomLoadout
{
    internal sealed class AliasLoadResult
    {
        public AliasLoadResult(PickupAliasRegistry registry, string[] messages, string[] warnings)
        {
            Registry = registry;
            Messages = messages ?? new string[0];
            Warnings = warnings ?? new string[0];
        }

        public PickupAliasRegistry Registry { get; private set; }

        public string[] Messages { get; private set; }

        public string[] Warnings { get; private set; }
    }
}
