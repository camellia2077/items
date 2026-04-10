using System;
using System.Collections.Generic;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
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
                        // JSON count explicitly allows 0, and 0 means "do not grant from this rule".
                        // Only negative values fall back to 1 to preserve a safe default for invalid input.
                        definitions.Add(
                            LoadoutRuleDefinition.Random(
                                category,
                                rule.Count >= 0 ? rule.Count : 1,
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
    }
}
