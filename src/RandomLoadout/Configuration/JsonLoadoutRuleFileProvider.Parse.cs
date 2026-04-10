using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
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
