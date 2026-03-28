using System;
using System.IO;

namespace RandomLoadout.Core.Tests
{
    internal static class RuleFileProviderTests
    {
        public static void ParsesSpecificAliasRule()
        {
            string filePath = CreateTempFile(
                "{\n" +
                "  \"rules\": [\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"specific\",\n" +
                "      \"category\": \"gun\",\n" +
                "      \"alias\": \"casey_bat\"\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(filePath);
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(1, result.Definitions.Length, "The rule file should produce one definition.");
                AssertEx.Equal("casey_bat", result.Definitions[0].SpecificAlias, "The specific alias should be preserved.");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        public static void ParsesRandomPoolAliasesAlongsideIdsAndNames()
        {
            string filePath = CreateTempFile(
                "{\n" +
                "  \"rules\": [\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"random\",\n" +
                "      \"category\": \"gun\",\n" +
                "      \"count\": 1,\n" +
                "      \"poolIds\": [541],\n" +
                "      \"poolAliases\": [\"casey_bat\"],\n" +
                "      \"pool\": [\"Casey\"]\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(filePath);
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(1, result.Definitions.Length, "The random rule should be preserved.");
                AssertEx.SequenceEqual(new[] { 541 }, result.Definitions[0].PoolIds, "The random rule should preserve pickup IDs.");
                AssertEx.SequenceEqual(new[] { "casey_bat" }, result.Definitions[0].PoolAliases, "The random rule should preserve pool aliases.");
                AssertEx.SequenceEqual(new[] { "Casey" }, result.Definitions[0].PoolNames, "The random rule should preserve pool names.");
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        public static void MissingAliasFileFallsBackToBuiltInDefaults()
        {
            string filePath = Path.Combine(Path.GetTempPath(), "RandomLoadout.aliases.tests." + Guid.NewGuid().ToString("N") + ".json");
            JsonPickupAliasFileProvider provider = new JsonPickupAliasFileProvider(filePath);
            AliasLoadResult result = provider.Load(delegate { return true; });

            int pickupId;
            AssertEx.True(result.Registry.TryResolve("casey_bat", out pickupId), "The built-in alias registry should include casey_bat.");
            AssertEx.Equal(541, pickupId, "The built-in alias registry should map casey_bat to 541.");
            AssertEx.True(result.Warnings.Length > 0, "Falling back to built-in aliases should produce a warning.");
        }

        public static void MissingPrimaryRuleFileFallsBackToFullPoolFile()
        {
            string missingPrimaryPath = Path.Combine(Path.GetTempPath(), "RandomLoadout.rules.tests." + Guid.NewGuid().ToString("N") + ".json");
            string fallbackPath = CreateTempFile(
                "{\n" +
                "  \"rules\": [\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"random\",\n" +
                "      \"category\": \"gun\",\n" +
                "      \"count\": 1,\n" +
                "      \"poolIds\": [541, 616]\n" +
                "    },\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"random\",\n" +
                "      \"category\": \"passive\",\n" +
                "      \"count\": 1,\n" +
                "      \"poolIds\": [118]\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(missingPrimaryPath, fallbackPath);
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(2, result.Definitions.Length, "The fallback full-pool file should provide rules when the primary file is missing.");
                AssertEx.Equal(GrantMode.Random, result.Definitions[0].Mode, "The fallback rule should preserve random mode.");
                AssertEx.SequenceEqual(new[] { 541, 616 }, result.Definitions[0].PoolIds, "The fallback rule should preserve the pool IDs.");
                AssertEx.True(result.Messages.Length > 0, "Using the fallback full-pool file should produce an informational message.");
                AssertEx.True(result.Warnings.Length > 0, "A missing primary rule file should still produce a warning.");
            }
            finally
            {
                File.Delete(fallbackPath);
            }
        }

        public static void InvalidPrimaryRuleFileFallsBackToFullPoolFile()
        {
            string invalidPrimaryPath = CreateTempFile("{ invalid json");
            string fallbackPath = CreateTempFile(
                "{\n" +
                "  \"rules\": [\n" +
                "    {\n" +
                "      \"enabled\": true,\n" +
                "      \"mode\": \"random\",\n" +
                "      \"category\": \"active\",\n" +
                "      \"count\": 1,\n" +
                "      \"poolIds\": [120]\n" +
                "    }\n" +
                "  ]\n" +
                "}\n");

            try
            {
                JsonLoadoutRuleFileProvider provider = new JsonLoadoutRuleFileProvider(invalidPrimaryPath, fallbackPath);
                LoadoutRuleFileLoadResult result = provider.Load();

                AssertEx.Equal(1, result.Definitions.Length, "The fallback full-pool file should provide rules when the primary file is invalid.");
                AssertEx.Equal(PickupCategory.Active, result.Definitions[0].Category, "The fallback rule category should be preserved.");
                AssertEx.SequenceEqual(new[] { 120 }, result.Definitions[0].PoolIds, "The fallback pool IDs should be preserved.");
                AssertEx.True(result.Messages.Length > 0, "Using the fallback full-pool file should produce an informational message.");
                AssertEx.True(result.Warnings.Length > 0, "An invalid primary rule file should still produce a warning.");
            }
            finally
            {
                File.Delete(invalidPrimaryPath);
                File.Delete(fallbackPath);
            }
        }

        private static string CreateTempFile(string content)
        {
            string filePath = Path.Combine(Path.GetTempPath(), "RandomLoadout.rules.tests." + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(filePath, content);
            return filePath;
        }
    }
}
