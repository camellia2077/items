namespace RandomLoadout
{
    internal sealed class LoadoutRuleFileModel
    {
        public LoadoutRuleFileModel()
        {
            Rules = new LoadoutRuleFileRuleModel[0];
        }

        public LoadoutRuleFileRuleModel[] Rules { get; set; }
    }

    internal sealed class LoadoutRuleFileRuleModel
    {
        public bool Enabled { get; set; }

        public string Mode { get; set; }

        public string Category { get; set; }

        public int Count { get; set; }

        public int? Id { get; set; }

        public string Alias { get; set; }

        public string Name { get; set; }

        public int[] PoolIds { get; set; }

        public string[] PoolAliases { get; set; }

        public string[] Pool { get; set; }
    }
}
