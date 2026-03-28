namespace RandomLoadout
{
    internal sealed class LoadoutRuleFileLoadResult
    {
        public LoadoutRuleFileLoadResult(LoadoutRuleDefinition[] definitions, string[] messages, string[] warnings)
        {
            Definitions = definitions;
            Messages = messages;
            Warnings = warnings;
        }

        public LoadoutRuleDefinition[] Definitions { get; private set; }

        public string[] Messages { get; private set; }

        public string[] Warnings { get; private set; }
    }
}
