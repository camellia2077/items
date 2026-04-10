using RandomLoadout.Core;
using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed partial class GrantCommandService
    {
        private static GrantCommandExecutionResult CreatePlayerNotReadyResult()
        {
            return new GrantCommandExecutionResult(false, "The player is not ready yet.");
        }

        private static GrantCommandExecutionResult CreateResolveFailureResult(EtgPickupResolveResult resolveResult, string fallbackMessage)
        {
            string message = resolveResult.Warning != null ? resolveResult.Warning.Message : fallbackMessage;
            return new GrantCommandExecutionResult(false, message);
        }

        private static GrantCommandExecutionResult CreateMissingCategoryResult(string message)
        {
            return new GrantCommandExecutionResult(false, message);
        }

        private static GrantCommandExecutionResult CreateGrantFailureResult(string pickupLabel, EtgGrantOutcome outcome)
        {
            return new GrantCommandExecutionResult(
                false,
                "Failed to grant " + pickupLabel + ": " + outcome.FailureReason +
                " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]");
        }

        private static GrantCommandExecutionResult CreateGrantSuccessResult(EtgGrantOutcome outcome, bool includePickupId)
        {
            string idSuffix = includePickupId ? " (ID " + outcome.PickupId + ")." : string.Empty;
            return new GrantCommandExecutionResult(
                true,
                "Granted " + outcome.Category + ": " + outcome.PickupLabel + idSuffix +
                " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]");
        }

        private static GrantCommandExecutionResult CreateRandomGrantFailureResult(string pickupLabel, EtgGrantOutcome outcome)
        {
            return new GrantCommandExecutionResult(
                false,
                "Failed to grant random pickup " + pickupLabel + ": " + outcome.FailureReason +
                " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]");
        }

        private static GrantCommandExecutionResult CreateRandomGrantSuccessResult(EtgGrantOutcome outcome)
        {
            return new GrantCommandExecutionResult(
                true,
                "Granted random " + outcome.Category + ": " + outcome.PickupLabel + " (ID " + outcome.PickupId + ")." +
                " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]");
        }

        private static string BuildAliasSuggestion(GrantCommandRequest request, PickupAliasRegistry aliasRegistry)
        {
            if (aliasRegistry == null || aliasRegistry.Count == 0 || string.IsNullOrEmpty(request.PickupName))
            {
                return " Try a configured alias or a pickup ID from RandomLoadout.pickups.txt, for example: gun casey_bat or gun 541.";
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
                return " Try a configured alias or a pickup ID from RandomLoadout.pickups.txt, for example: gun casey_bat or gun 541.";
            }

            matchingAliases.Sort(StringComparer.OrdinalIgnoreCase);
            return " Try " + string.Join(" or ", matchingAliases.ToArray()) + ", or use the pickup ID.";
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
