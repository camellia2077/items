using RandomLoadout.Core;
using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed partial class GrantCommandService
    {
        private static GrantCommandExecutionResult CreatePlayerNotReadyResult()
        {
            return GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
        }

        private GrantCommandExecutionResult CreateResolveFailureResult(
            GrantCommandRequest request,
            EtgPickupResolveResult resolveResult,
            string fallbackKey,
            string fallbackEnglishMessage)
        {
            if (resolveResult == null || resolveResult.Warning == null)
            {
                return new GrantCommandExecutionResult(false, GuiText.Get(fallbackKey), GuiText.GetEnglish(fallbackKey));
            }

            string lookupValue = request != null ? request.PickupName : string.Empty;
            switch (resolveResult.Warning.Code)
            {
                case "PickupLookupEmpty":
                    return GrantCommandExecutionResult.Localized(false, "result.error.pickup_lookup_empty");
                case "InternalNameNotFound":
                case "DisplayNameNotFound":
                    return GrantCommandExecutionResult.Localized(false, "result.error.no_pickup_matched", lookupValue);
                case "InternalNameAmbiguous":
                case "DisplayNameAmbiguous":
                    return CreateAmbiguousMatchResult(request, lookupValue);
                case "InvalidPickupId":
                    return GrantCommandExecutionResult.Localized(false, "result.error.invalid_pickup_id", lookupValue);
                case "PickupCategoryMismatch":
                    return GrantCommandExecutionResult.Localized(false, "result.error.pickup_category_mismatch");
                case "RandomPickupUnavailable":
                    return GrantCommandExecutionResult.Localized(false, "result.error.random_pickup_unavailable");
                case "CommandTargetUnsupported":
                    return GrantCommandExecutionResult.Localized(false, "result.error.command_target_unsupported");
                default:
                    return new GrantCommandExecutionResult(
                        false,
                        GuiText.Get(fallbackKey),
                        !string.IsNullOrEmpty(resolveResult.Warning.Message) ? resolveResult.Warning.Message : fallbackEnglishMessage);
            }
        }

        private static GrantCommandExecutionResult CreateMissingCategoryResult(string key, string englishMessage)
        {
            return new GrantCommandExecutionResult(false, GuiText.Get(key), englishMessage);
        }

        private static GrantCommandExecutionResult CreateGrantFailureResult(string pickupLabel, EtgGrantOutcome outcome)
        {
            return new GrantCommandExecutionResult(
                false,
                GuiText.Get("result.grant.failure", pickupLabel, outcome.FailureReason, outcome.GrantPath, outcome.GrantDetail),
                GuiText.GetEnglish("result.grant.failure", pickupLabel, outcome.FailureReason, outcome.GrantPath, outcome.GrantDetail));
        }

        private static GrantCommandExecutionResult CreateGrantSuccessResult(EtgGrantOutcome outcome, bool includePickupId)
        {
            string categoryLabel = GuiText.GetCategoryLabel(outcome.Category);
            string englishCategoryLabel = GuiText.GetEnglishCategoryLabel(outcome.Category);
            if (includePickupId)
            {
                return new GrantCommandExecutionResult(
                    true,
                    GuiText.Get("result.grant.success_with_id", categoryLabel, outcome.PickupLabel, outcome.PickupId, outcome.GrantPath, outcome.GrantDetail),
                    GuiText.GetEnglish("result.grant.success_with_id", englishCategoryLabel, outcome.PickupLabel, outcome.PickupId, outcome.GrantPath, outcome.GrantDetail));
            }

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.grant.success", categoryLabel, outcome.PickupLabel, outcome.GrantPath, outcome.GrantDetail),
                GuiText.GetEnglish("result.grant.success", englishCategoryLabel, outcome.PickupLabel, outcome.GrantPath, outcome.GrantDetail));
        }

        private static GrantCommandExecutionResult CreateRandomGrantFailureResult(string pickupLabel, EtgGrantOutcome outcome)
        {
            return new GrantCommandExecutionResult(
                false,
                GuiText.Get("result.grant.random_failure", pickupLabel, outcome.FailureReason, outcome.GrantPath, outcome.GrantDetail),
                GuiText.GetEnglish("result.grant.random_failure", pickupLabel, outcome.FailureReason, outcome.GrantPath, outcome.GrantDetail));
        }

        private static GrantCommandExecutionResult CreateRandomGrantSuccessResult(EtgGrantOutcome outcome)
        {
            string categoryLabel = GuiText.GetCategoryLabel(outcome.Category);
            string englishCategoryLabel = GuiText.GetEnglishCategoryLabel(outcome.Category);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.grant.random_success", categoryLabel, outcome.PickupLabel, outcome.PickupId, outcome.GrantPath, outcome.GrantDetail),
                GuiText.GetEnglish("result.grant.random_success", englishCategoryLabel, outcome.PickupLabel, outcome.PickupId, outcome.GrantPath, outcome.GrantDetail));
        }

        private GrantCommandExecutionResult CreateAmbiguousMatchResult(GrantCommandRequest request, string lookupValue)
        {
            string displayMessage = GuiText.Get("result.error.multiple_pickups_matched", lookupValue);
            string logMessage = GuiText.GetEnglish("result.error.multiple_pickups_matched", lookupValue);
            string aliasSuggestion = BuildAliasSuggestion(request, GetAliasRegistry(), false);
            string englishAliasSuggestion = BuildAliasSuggestion(request, GetAliasRegistry(), true);
            return new GrantCommandExecutionResult(false, displayMessage + aliasSuggestion, logMessage + englishAliasSuggestion);
        }

        private static string BuildAliasSuggestion(GrantCommandRequest request, PickupAliasRegistry aliasRegistry, bool englishOnly)
        {
            if (aliasRegistry == null || aliasRegistry.Count == 0 || request == null || string.IsNullOrEmpty(request.PickupName))
            {
                return englishOnly
                    ? GuiText.GetEnglish("result.error.alias_suggestion_example")
                    : GuiText.Get("result.error.alias_suggestion_example");
            }

            List<string> matchingAliases = new List<string>();
            string normalizedLookup = NormalizeLookupValue(request.PickupName);
            for (int index = 0; index < aliasRegistry.Entries.Length; index++)
            {
                PickupAliasEntry entry = aliasRegistry.Entries[index];
                PickupObject pickup = PickupObjectDatabase.GetById(entry.PickupId);
                if ((object)pickup == null || !MatchesRequestedTarget(request.Target, pickup))
                {
                    continue;
                }

                if (!MatchesLookupValue(pickup, request.PickupName, normalizedLookup))
                {
                    continue;
                }

                matchingAliases.Add(entry.Alias);
            }

            if (matchingAliases.Count == 0)
            {
                return englishOnly
                    ? GuiText.GetEnglish("result.error.alias_suggestion_example")
                    : GuiText.Get("result.error.alias_suggestion_example");
            }

            matchingAliases.Sort(StringComparer.OrdinalIgnoreCase);
            string aliasText = string.Join(" or ", matchingAliases.ToArray());
            return englishOnly
                ? GuiText.GetEnglish("result.error.alias_suggestion_matches", aliasText)
                : GuiText.Get("result.error.alias_suggestion_matches", aliasText);
        }

        private static bool MatchesRequestedTarget(GrantCommandTarget target, PickupObject pickup)
        {
            switch (target)
            {
                case GrantCommandTarget.Gun:
                    return pickup is Gun;
                case GrantCommandTarget.Passive:
                    return pickup is PassiveItem;
                case GrantCommandTarget.Active:
                    return pickup is PlayerItem;
                case GrantCommandTarget.Any:
                    return pickup is Gun || pickup is PassiveItem || pickup is PlayerItem;
                default:
                    return false;
            }
        }

        private static bool MatchesLookupValue(PickupObject pickup, string rawLookup, string normalizedLookup)
        {
            if ((object)pickup == null)
            {
                return false;
            }

            if (string.Equals(pickup.name ?? string.Empty, rawLookup, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(NormalizeLookupValue(pickup.name), normalizedLookup, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string displayLabel = GetPickupLabel(pickup);
            return string.Equals(displayLabel, rawLookup, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(NormalizeLookupValue(displayLabel), normalizedLookup, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetPickupLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return string.Empty;
            }

            if (pickup.encounterTrackable != null)
            {
                string modifiedDisplayName = pickup.encounterTrackable.GetModifiedDisplayName();
                if (!string.IsNullOrEmpty(modifiedDisplayName))
                {
                    return ResolveLocalizedLabel(modifiedDisplayName);
                }

                if (pickup.encounterTrackable.journalData != null &&
                    !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
                {
                    return ResolveLocalizedLabel(pickup.encounterTrackable.journalData.PrimaryDisplayName);
                }
            }

            if (!string.IsNullOrEmpty(pickup.DisplayName))
            {
                return ResolveLocalizedLabel(pickup.DisplayName);
            }

            return ResolveLocalizedLabel(pickup.name);
        }

        private static string NormalizeLookupValue(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(rawValue.Length);
            for (int i = 0; i < rawValue.Length; i++)
            {
                char current = rawValue[i];
                if (char.IsLetterOrDigit(current))
                {
                    builder.Append(char.ToLowerInvariant(current));
                }
            }

            return builder.ToString();
        }

        private static string ResolveLocalizedLabel(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel))
            {
                return string.Empty;
            }

            if (!rawLabel.StartsWith("#", StringComparison.Ordinal))
            {
                return rawLabel;
            }

            string localized = StringTableManager.GetString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, StringComparison.Ordinal))
            {
                return localized;
            }

            localized = StringTableManager.GetItemsString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, StringComparison.Ordinal))
            {
                return localized;
            }

            return rawLabel;
        }
    }
}
