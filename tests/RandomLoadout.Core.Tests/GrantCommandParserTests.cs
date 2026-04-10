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

        public static void ParsesUnknownPrefixAsAny()
        {
            GrantCommandParser parser = new GrantCommandParser();
            GrantCommandParseResult result = parser.Parse("wep AK-47");

            AssertEx.True(result.Succeeded, "The parser should treat unknown prefixes as item/any input.");
            AssertEx.Equal(GrantCommandTarget.Any, result.Request.Target, "The parser should fall back to the any target for unknown prefixes.");
            AssertEx.Equal("wep AK-47", result.Request.PickupName, "The parser should preserve the full input for downstream pickup lookup.");
        }

        public static void ParsesBareInputAsAny()
        {
            GrantCommandParser parser = new GrantCommandParser();
            GrantCommandParseResult result = parser.Parse("PlatinumBullets");

            AssertEx.True(result.Succeeded, "The parser should accept bare pickup input.");
            AssertEx.Equal(GrantCommandTarget.Any, result.Request.Target, "Bare pickup input should default to the any target.");
            AssertEx.Equal("PlatinumBullets", result.Request.PickupName, "The parser should preserve bare pickup input.");
        }

        public static void RejectsMissingPickupName()
        {
            GrantCommandParser parser = new GrantCommandParser();
            GrantCommandParseResult result = parser.Parse("gun");

            AssertEx.True(!result.Succeeded, "The parser should reject commands without a pickup name.");
        }
    }
}
