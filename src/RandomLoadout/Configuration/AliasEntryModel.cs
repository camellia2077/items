namespace RandomLoadout
{
    internal sealed class AliasFileModel
    {
        public AliasFileModel()
        {
            Aliases = new AliasEntryModel[0];
        }

        public AliasEntryModel[] Aliases { get; set; }
    }

    internal sealed class AliasEntryModel
    {
        public string Alias { get; set; }

        public int Id { get; set; }
    }
}
