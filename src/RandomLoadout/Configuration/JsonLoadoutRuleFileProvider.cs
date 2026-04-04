using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class JsonLoadoutRuleFileProvider
    {
        private readonly string _filePath;
        private readonly string _fallbackFilePath;

        public JsonLoadoutRuleFileProvider(string filePath)
            : this(filePath, string.Empty)
        {
        }

        public JsonLoadoutRuleFileProvider(string filePath, string fallbackFilePath)
        {
            _filePath = filePath;
            _fallbackFilePath = fallbackFilePath ?? string.Empty;
        }

        public LoadoutRuleFileLoadResult Load()
        {
            List<string> messages = new List<string>();
            List<string> warnings = new List<string>();
            LoadoutRuleFileModel fileModel = null;

            if (!File.Exists(_filePath))
            {
                warnings.Add("Loadout rule file was not found at '" + _filePath + "'.");
                TryLoadFallback(messages, warnings, "Primary rule file was missing", out fileModel);
            }
            else
            {
                try
                {
                    string rawJson = Json5TextNormalizer.Normalize(File.ReadAllText(_filePath, Encoding.UTF8));
                    fileModel = ParseRuleFile(rawJson);
                }
                catch (Exception exception)
                {
                    warnings.Add("Failed to parse loadout rule file '" + _filePath + "'. " + exception.Message);
                    TryLoadFallback(messages, warnings, "Primary rule file could not be parsed", out fileModel);
                }
            }

            if (fileModel == null)
            {
                warnings.Add("Falling back to built-in default rules for this session.");
                fileModel = CreateDefaultModel();
            }

            return new LoadoutRuleFileLoadResult(ConvertToDefinitions(fileModel, messages), messages.ToArray(), warnings.ToArray());
        }

        private void TryLoadFallback(List<string> messages, List<string> warnings, string reason, out LoadoutRuleFileModel fileModel)
        {
            fileModel = null;

            if (string.IsNullOrEmpty(_fallbackFilePath) || !File.Exists(_fallbackFilePath))
            {
                warnings.Add(
                    "Fallback full-pool rule file was not found at '" + _fallbackFilePath + "'. " +
                    "Run deploy_mod.py again, or copy the repository default RandomLoadout rules into the game config directory.");
                return;
            }

            try
            {
                string fallbackRawJson = Json5TextNormalizer.Normalize(File.ReadAllText(_fallbackFilePath, Encoding.UTF8));
                fileModel = ParseRuleFile(fallbackRawJson);
                messages.Add(reason + ", so RandomLoadout loaded fallback rules from '" + _fallbackFilePath + "'.");
            }
            catch (Exception exception)
            {
                warnings.Add(
                    "Failed to parse fallback loadout rule file '" + _fallbackFilePath + "'. Falling back to built-in default. " +
                    exception.Message);
            }
        }

        private static LoadoutRuleFileModel ParseRuleFile(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson))
            {
                return new LoadoutRuleFileModel();
            }

            if (string.IsNullOrEmpty(rawJson.Trim()))
            {
                return new LoadoutRuleFileModel();
            }

            if (!Regex.IsMatch(rawJson, "(?:\"rules\"|'rules'|\\brules\\b)\\s*:", RegexOptions.IgnoreCase))
            {
                throw new FormatException("Rule file must contain a 'rules' array.");
            }

            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>();
            MatchCollection ruleMatches = Regex.Matches(rawJson, "\\{(?<body>[\\s\\S]*?)\\}");
            for (int i = 0; i < ruleMatches.Count; i++)
            {
                string body = ruleMatches[i].Groups["body"].Value;
                if (!Regex.IsMatch(body, GetPropertyPrefixPattern("mode"), RegexOptions.IgnoreCase))
                {
                    continue;
                }

                LoadoutRuleFileRuleModel rule = new LoadoutRuleFileRuleModel();
                rule.Enabled = ParseBool(body, "enabled", true);
                rule.Mode = ParseString(body, "mode");
                rule.Category = ParseString(body, "category");
                rule.Count = ParseInt(body, "count", 1);
                rule.Id = ParseNullableInt(body, "id");
                rule.Alias = ParseString(body, "alias");
                rule.Name = ParseString(body, "name");
                rule.PoolIds = ParseIntArray(body, "poolIds");
                rule.PoolAliases = ParseStringArray(body, "poolAliases");
                rule.Pool = ParseStringArray(body, "pool");
                rules.Add(rule);
            }

            return new LoadoutRuleFileModel { Rules = rules.ToArray() };
        }

        private static LoadoutRuleDefinition[] ConvertToDefinitions(LoadoutRuleFileModel fileModel, List<string> messages)
        {
            List<LoadoutRuleDefinition> definitions = new List<LoadoutRuleDefinition>();
            LoadoutRuleFileRuleModel[] rules = fileModel != null && fileModel.Rules != null
                ? fileModel.Rules
                : new LoadoutRuleFileRuleModel[0];

            for (int i = 0; i < rules.Length; i++)
            {
                LoadoutRuleFileRuleModel rule = rules[i];
                if (rule == null || !rule.Enabled)
                {
                    continue;
                }

                PickupCategory category;
                if (!TryParseCategory(rule.Category, out category))
                {
                    messages.Add("Skipped rule #" + (i + 1) + " because category '" + rule.Category + "' was invalid.");
                    continue;
                }

                GrantMode mode;
                if (!TryParseMode(rule.Mode, out mode))
                {
                    messages.Add("Skipped rule #" + (i + 1) + " because mode '" + rule.Mode + "' was invalid.");
                    continue;
                }

                switch (mode)
                {
                    case GrantMode.Random:
                        int[] poolIds = rule.PoolIds ?? new int[0];
                        string[] poolAliases = rule.PoolAliases ?? new string[0];
                        string[] poolNames = rule.Pool ?? new string[0];
                        definitions.Add(
                            LoadoutRuleDefinition.Random(
                                category,
                                rule.Count > 0 ? rule.Count : 1,
                                poolIds,
                                poolAliases,
                                poolNames));

                        break;
                    case GrantMode.Specific:
                        if (rule.Id.HasValue)
                        {
                            definitions.Add(LoadoutRuleDefinition.Specific(category, rule.Id.Value));
                            continue;
                        }

                        if (!string.IsNullOrEmpty(rule.Alias))
                        {
                            definitions.Add(LoadoutRuleDefinition.SpecificByAlias(category, rule.Alias));
                            continue;
                        }

                        if (string.IsNullOrEmpty(rule.Name))
                        {
                            messages.Add("Skipped rule #" + (i + 1) + " because a specific rule did not define 'id', 'alias', or 'name'.");
                            continue;
                        }

                        definitions.Add(LoadoutRuleDefinition.Specific(category, rule.Name));
                        break;
                }
            }

            return definitions.ToArray();
        }

        private static bool TryParseCategory(string rawCategory, out PickupCategory category)
        {
            string normalized = rawCategory != null ? rawCategory.Trim().ToLowerInvariant() : string.Empty;
            switch (normalized)
            {
                case "gun":
                    category = PickupCategory.Gun;
                    return true;
                case "passive":
                    category = PickupCategory.Passive;
                    return true;
                case "active":
                    category = PickupCategory.Active;
                    return true;
                default:
                    category = PickupCategory.Gun;
                    return false;
            }
        }

        private static bool TryParseMode(string rawMode, out GrantMode mode)
        {
            string normalized = rawMode != null ? rawMode.Trim().ToLowerInvariant() : string.Empty;
            switch (normalized)
            {
                case "random":
                    mode = GrantMode.Random;
                    return true;
                case "specific":
                    mode = GrantMode.Specific;
                    return true;
                default:
                    mode = GrantMode.Random;
                    return false;
            }
        }

        private static LoadoutRuleFileModel CreateDefaultModel()
        {
            return new LoadoutRuleFileModel
            {
                Rules = new[]
                {
                    new LoadoutRuleFileRuleModel
                    {
                        Enabled = true,
                        Mode = "specific",
                        Category = "gun",
                        Count = 1,
                        Id = 541,
                        Pool = new string[0],
                    },
                    new LoadoutRuleFileRuleModel
                    {
                        Enabled = true,
                        Mode = "specific",
                        Category = "passive",
                        Count = 1,
                        Id = 118,
                        Pool = new string[0],
                    },
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
            return UnescapeJsonString(value);
        }

        private static bool ParseBool(string body, string propertyName, bool defaultValue)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "(?<value>true|false)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return defaultValue;
            }

            return string.Equals(match.Groups["value"].Value, "true", StringComparison.OrdinalIgnoreCase);
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

        private static int? ParseNullableInt(string body, string propertyName)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "(?<value>-?\\d+)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return null;
            }

            int value;
            return int.TryParse(match.Groups["value"].Value, out value) ? (int?)value : null;
        }

        private static int[] ParseIntArray(string body, string propertyName)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "\\[(?<value>[\\s\\S]*?)\\]",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return new int[0];
            }

            MatchCollection itemMatches = Regex.Matches(match.Groups["value"].Value, "-?\\d+");
            List<int> values = new List<int>();
            for (int i = 0; i < itemMatches.Count; i++)
            {
                int value;
                if (int.TryParse(itemMatches[i].Value, out value))
                {
                    values.Add(value);
                }
            }

            return values.ToArray();
        }

        private static string[] ParseStringArray(string body, string propertyName)
        {
            Match match = Regex.Match(
                body,
                GetPropertyPrefixPattern(propertyName) + "\\[(?<value>[\\s\\S]*?)\\]",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return new string[0];
            }

            MatchCollection itemMatches = Regex.Matches(
                match.Groups["value"].Value,
                "(?:\"(?<dq>(?:\\\\.|[^\"])*)\"|'(?<sq>(?:\\\\.|[^'])*)')");
            List<string> values = new List<string>();
            for (int i = 0; i < itemMatches.Count; i++)
            {
                string itemValue = itemMatches[i].Groups["dq"].Success
                    ? itemMatches[i].Groups["dq"].Value
                    : itemMatches[i].Groups["sq"].Value;
                values.Add(UnescapeJsonString(itemValue));
            }

            return values.ToArray();
        }

        private static string UnescapeJsonString(string value)
        {
            return value
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }

        private static string GetPropertyPrefixPattern(string propertyName)
        {
            string escaped = Regex.Escape(propertyName);
            return "(?:\"" + escaped + "\"|'" + escaped + "'|\\b" + escaped + "\\b)\\s*:\\s*";
        }
    }
}
