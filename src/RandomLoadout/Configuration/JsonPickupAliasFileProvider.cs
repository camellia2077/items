using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RandomLoadout
{
    internal sealed class JsonPickupAliasFileProvider
    {
        private readonly string _filePath;

        public JsonPickupAliasFileProvider(string filePath)
        {
            _filePath = filePath;
        }

        public AliasLoadResult Load(Func<int, bool> isSupportedPickupId)
        {
            List<string> messages = new List<string>();
            List<string> warnings = new List<string>();
            AliasFileModel fileModel = null;
            bool usedBuiltInDefault = false;

            if (!File.Exists(_filePath))
            {
                warnings.Add(
                    "Pickup alias file was not found at '" + _filePath + "'. " +
                    "This build now expects the repository default config to be deployed into BepInEx\\config. " +
                    "Run deploy_mod.py again, or copy the repository default RandomLoadout.aliases.json5 into the game config directory. " +
                    "Falling back to built-in default aliases for this session.");
                usedBuiltInDefault = true;
            }
            else
            {
                try
                {
                    string rawJson = Json5TextNormalizer.Normalize(File.ReadAllText(_filePath, Encoding.UTF8));
                    fileModel = ParseAliasFile(rawJson);
                }
                catch (Exception exception)
                {
                    warnings.Add(
                        "Failed to parse pickup alias file '" + _filePath + "'. Falling back to built-in default aliases. " +
                        exception.Message);
                    usedBuiltInDefault = true;
                }
            }

            if (fileModel == null)
            {
                fileModel = CreateDefaultModel();
            }

            PickupAliasRegistry registry = PickupAliasRegistry.Create(fileModel.Aliases, warnings, isSupportedPickupId);
            if (usedBuiltInDefault)
            {
                messages.Add("Using built-in pickup alias registry (" + registry.Count + " aliases).");
            }
            else
            {
                messages.Add("Loaded pickup alias registry from '" + _filePath + "' (" + registry.Count + " aliases).");
            }

            return new AliasLoadResult(registry, messages.ToArray(), warnings.ToArray());
        }

        private static AliasFileModel ParseAliasFile(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson))
            {
                return new AliasFileModel();
            }

            List<AliasEntryModel> aliases = new List<AliasEntryModel>();
            MatchCollection aliasMatches = Regex.Matches(rawJson, "\\{(?<body>[\\s\\S]*?)\\}");
            for (int i = 0; i < aliasMatches.Count; i++)
            {
                string body = aliasMatches[i].Groups["body"].Value;
                if (!Regex.IsMatch(body, GetPropertyPrefixPattern("alias"), RegexOptions.IgnoreCase))
                {
                    continue;
                }

                aliases.Add(
                    new AliasEntryModel
                    {
                        Alias = ParseString(body, "alias"),
                        Id = ParseInt(body, "id", 0),
                    });
            }

            return new AliasFileModel { Aliases = aliases.ToArray() };
        }

        private static AliasFileModel CreateDefaultModel()
        {
            return new AliasFileModel
            {
                Aliases = new[]
                {
                    new AliasEntryModel { Alias = "casey_bat", Id = 541 },
                    new AliasEntryModel { Alias = "casey_nail", Id = 616 },
                    new AliasEntryModel { Alias = "eyepatch", Id = 118 },
                },
            };
        }

        private static string ParseString(string body, string propertyName)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "(?:\"(?<dq>(?:\\\\.|[^\"])*)\"|'(?<sq>(?:\\\\.|[^'])*)')",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return string.Empty;
            }

            string value = match.Groups["dq"].Success
                ? match.Groups["dq"].Value
                : match.Groups["sq"].Value;
            return value
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        private static int ParseInt(string body, string propertyName, int defaultValue)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "(?<value>-?\\d+)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return defaultValue;
            }

            int value;
            return int.TryParse(match.Groups["value"].Value, out value) ? value : defaultValue;
        }

        private static string GetPropertyPrefixPattern(string propertyName)
        {
            string escaped = Regex.Escape(propertyName);
            return "(?:\"" + escaped + "\"|'" + escaped + "'|\\b" + escaped + "\\b)\\s*:\\s*";
        }
    }
}
