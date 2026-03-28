namespace RandomLoadout.Core.Tests
{
    internal static class GrantCommandParserTests
    {
        public static void ParsesGunCommand()
        {
            GrantCommandParser parser = new GrantCommandParser();
            GrantCommandParseResult result = parser.Parse("gun AK-47");

            AssertEx.True(result.Succeeded, "The parser should accept gun commands.");
            AssertEx.Equal(GrantCommandTarget.Gun, result.Request.Target, "The parser should detect the gun target.");
            AssertEx.Equal("AK-47", result.Request.PickupName, "The parser should preserve the pickup name.");
        }

        public static void ParsesAnyCommandAlias()
        {
            GrantCommandParser parser = new GrantCommandParser();
            GrantCommandParseResult result = parser.Parse("item Scope");

            AssertEx.True(result.Succeeded, "The parser should accept item commands.");
            AssertEx.Equal(GrantCommandTarget.Any, result.Request.Target, "The parser should map item to the any target.");
            AssertEx.Equal("Scope", result.Request.PickupName, "The parser should preserve the pickup name.");
        }

        public static void PreservesPastedPickupCatalogLine()
        {
            GrantCommandParser parser = new GrantCommandParser();
            GrantCommandParseResult result = parser.Parse("gun 541 Casey Baseball_Bat_Gun");

            AssertEx.True(result.Succeeded, "The parser should accept a command that starts with a pickup ID.");
            AssertEx.Equal(GrantCommandTarget.Gun, result.Request.Target, "The parser should detect the gun target.");
            AssertEx.Equal("541 Casey Baseball_Bat_Gun", result.Request.PickupName, "The parser should preserve the pasted value for downstream ID detection.");
        }

        public static void RejectsUnknownTarget()
        {
            GrantCommandParser parser = new GrantCommandParser();
            GrantCommandParseResult result = parser.Parse("wep AK-47");

            AssertEx.True(!result.Succeeded, "The parser should reject unknown targets.");
        }

        public static void RejectsMissingPickupName()
        {
            GrantCommandParser parser = new GrantCommandParser();
            GrantCommandParseResult result = parser.Parse("gun");

            AssertEx.True(!result.Succeeded, "The parser should reject commands without a pickup name.");
        }
    }
}
