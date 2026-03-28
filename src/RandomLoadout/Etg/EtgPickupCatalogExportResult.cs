namespace RandomLoadout
{
    internal sealed class EtgPickupCatalogExportResult
    {
        public EtgPickupCatalogExportResult(
            bool succeeded,
            string textOutputPath,
            string jsonOutputPath,
            string groupedJsonOutputPath,
            string rulePoolOutputPath,
            int entryCount,
            string failureReason)
        {
            Succeeded = succeeded;
            TextOutputPath = textOutputPath ?? string.Empty;
            JsonOutputPath = jsonOutputPath ?? string.Empty;
            GroupedJsonOutputPath = groupedJsonOutputPath ?? string.Empty;
            RulePoolOutputPath = rulePoolOutputPath ?? string.Empty;
            EntryCount = entryCount;
            FailureReason = failureReason ?? string.Empty;
        }

        public bool Succeeded { get; private set; }

        public string TextOutputPath { get; private set; }

        public string JsonOutputPath { get; private set; }

        public string GroupedJsonOutputPath { get; private set; }

        public string RulePoolOutputPath { get; private set; }

        public int EntryCount { get; private set; }

        public string FailureReason { get; private set; }
    }
}
