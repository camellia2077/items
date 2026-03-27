using System;
using System.Collections.Generic;

namespace RandomLoadout.Core.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            KeyValuePair<string, Action>[] tests =
            {
                new KeyValuePair<string, Action>("ParsesGunCommand", GrantCommandParserTests.ParsesGunCommand),
                new KeyValuePair<string, Action>("ParsesAnyCommandAlias", GrantCommandParserTests.ParsesAnyCommandAlias),
                new KeyValuePair<string, Action>("RejectsUnknownTarget", GrantCommandParserTests.RejectsUnknownTarget),
                new KeyValuePair<string, Action>("RejectsMissingPickupName", GrantCommandParserTests.RejectsMissingPickupName),
                new KeyValuePair<string, Action>("FixedSeedProducesRepeatableSelections", LoadoutSelectionServiceTests.FixedSeedProducesRepeatableSelections),
                new KeyValuePair<string, Action>("OwnedPickupsAreFiltered", LoadoutSelectionServiceTests.OwnedPickupsAreFiltered),
                new KeyValuePair<string, Action>("DuplicateIdsAcrossCategoriesAreNotSelectedTwice", LoadoutSelectionServiceTests.DuplicateIdsAcrossCategoriesAreNotSelectedTwice),
                new KeyValuePair<string, Action>("CategoryWithoutCandidatesDoesNotBlockOthers", LoadoutSelectionServiceTests.CategoryWithoutCandidatesDoesNotBlockOthers),
                new KeyValuePair<string, Action>("RequestedCountGreaterThanAvailableDoesNotCrash", LoadoutSelectionServiceTests.RequestedCountGreaterThanAvailableDoesNotCrash),
                new KeyValuePair<string, Action>("EmptyPoolProducesWarning", LoadoutSelectionServiceTests.EmptyPoolProducesWarning),
                new KeyValuePair<string, Action>("EmptyConfigProducesWarning", LoadoutSelectionServiceTests.EmptyConfigProducesWarning),
                new KeyValuePair<string, Action>("SpecificRuleReturnsConfiguredPickup", LoadoutSelectionServiceTests.SpecificRuleReturnsConfiguredPickup),
                new KeyValuePair<string, Action>("SpecificRuleWarnsWhenPickupAlreadyOwned", LoadoutSelectionServiceTests.SpecificRuleWarnsWhenPickupAlreadyOwned),
                new KeyValuePair<string, Action>("SpecificRulesRespectConfigOrderForDuplicateSelections", LoadoutSelectionServiceTests.SpecificRulesRespectConfigOrderForDuplicateSelections),
                new KeyValuePair<string, Action>("MixedRulesRespectConfigOrder", LoadoutSelectionServiceTests.MixedRulesRespectConfigOrder),
                new KeyValuePair<string, Action>("InvalidSpecificRuleProducesWarning", LoadoutSelectionServiceTests.InvalidSpecificRuleProducesWarning),
            };

            int failures = 0;
            for (int i = 0; i < tests.Length; i++)
            {
                KeyValuePair<string, Action> test = tests[i];
                try
                {
                    test.Value();
                    Console.WriteLine("[PASS] " + test.Key);
                }
                catch (Exception ex)
                {
                    failures++;
                    Console.Error.WriteLine("[FAIL] " + test.Key + ": " + ex.Message);
                    Console.Error.WriteLine(ex);
                }
            }

            if (failures > 0)
            {
                Console.Error.WriteLine("Test failures: " + failures);
                return 1;
            }

            Console.WriteLine("All tests passed: " + tests.Length);
            return 0;
        }
    }
}
