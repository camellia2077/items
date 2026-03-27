using System.Linq;

namespace RandomLoadout.Core.Tests
{
    internal static class LoadoutSelectionServiceTests
    {
        public static void FixedSeedProducesRepeatableSelections()
        {
            LoadoutConfig config = CreateConfig(
                LoadoutRuleConfig.CreateRandom(PickupCategory.Gun, 1, new[] { 1, 2, 3 }),
                LoadoutRuleConfig.CreateRandom(PickupCategory.Passive, 1, new[] { 10, 11, 12 }),
                LoadoutRuleConfig.CreateRandom(PickupCategory.Active, 1, new[] { 20, 21, 22 }));

            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult first = service.SelectLoadout(new LoadoutSelectionRequest(123456, config, new int[0]));
            LoadoutSelectionResult second = service.SelectLoadout(new LoadoutSelectionRequest(123456, config, new int[0]));

            AssertEx.SequenceEqual(first.Selections.Select(FormatSelection), second.Selections.Select(FormatSelection), "Selections should be reproducible for the same seed.");
            AssertEx.SequenceEqual(first.Warnings.Select(warning => warning.Code), second.Warnings.Select(warning => warning.Code), "Warnings should be reproducible for the same seed.");
        }

        public static void OwnedPickupsAreFiltered()
        {
            LoadoutConfig config = CreateConfig(LoadoutRuleConfig.CreateRandom(PickupCategory.Gun, 1, new[] { 1, 2 }));
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(new LoadoutSelectionRequest(1, config, new[] { 1 }));

            AssertEx.Equal(1, result.Selections.Length, "Exactly one gun should be selected.");
            AssertEx.Equal(2, result.Selections[0].PickupId, "Owned pickup IDs should be filtered out.");
        }

        public static void DuplicateIdsAcrossCategoriesAreNotSelectedTwice()
        {
            LoadoutConfig config = CreateConfig(
                LoadoutRuleConfig.CreateRandom(PickupCategory.Gun, 1, new[] { 5 }),
                LoadoutRuleConfig.CreateRandom(PickupCategory.Passive, 1, new[] { 5, 6 }));

            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(new LoadoutSelectionRequest(7, config, new int[0]));

            AssertEx.SequenceEqual(new[] { 5, 6 }, result.Selections.Select(selection => selection.PickupId), "Duplicate pickup IDs should only be selected once across categories.");
        }

        public static void CategoryWithoutCandidatesDoesNotBlockOthers()
        {
            LoadoutConfig config = CreateConfig(
                LoadoutRuleConfig.CreateRandom(PickupCategory.Gun, 1, new[] { 1 }),
                LoadoutRuleConfig.CreateRandom(PickupCategory.Passive, 1, new[] { 10 }));

            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(new LoadoutSelectionRequest(99, config, new[] { 1 }));

            AssertEx.Equal(1, result.Selections.Length, "Other categories should continue selecting pickups.");
            AssertEx.Equal(PickupCategory.Passive, result.Selections[0].Category, "The remaining category should still be selected.");
            AssertEx.True(result.Warnings.Any(warning => warning.Code == "NoCandidates" && warning.Category == PickupCategory.Gun), "A no-candidates warning should be emitted for the exhausted category.");
        }

        public static void RequestedCountGreaterThanAvailableDoesNotCrash()
        {
            LoadoutConfig config = CreateConfig(LoadoutRuleConfig.CreateRandom(PickupCategory.Gun, 2, new[] { 1 }));
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(new LoadoutSelectionRequest(5, config, new int[0]));

            AssertEx.Equal(1, result.Selections.Length, "The selector should return all available pickups without crashing.");
            AssertEx.True(result.Warnings.Any(warning => warning.Code == "InsufficientCandidates"), "The selector should emit an insufficient-candidates warning.");
        }

        public static void EmptyPoolProducesWarning()
        {
            LoadoutConfig config = CreateConfig(LoadoutRuleConfig.CreateRandom(PickupCategory.Active, 1, new int[0]));
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(new LoadoutSelectionRequest(4, config, new int[0]));

            AssertEx.Equal(0, result.Selections.Length, "Empty pools should not produce selections.");
            AssertEx.True(result.Warnings.Any(warning => warning.Code == "PoolEmpty" && warning.Category == PickupCategory.Active), "An empty pool warning should be emitted.");
        }

        public static void EmptyConfigProducesWarning()
        {
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(new LoadoutSelectionRequest(4, new LoadoutConfig(new LoadoutRuleConfig[0]), new int[0]));

            AssertEx.Equal(0, result.Selections.Length, "Empty configs should not produce selections.");
            AssertEx.True(result.Warnings.Any(warning => warning.Code == "ConfigEmpty" && !warning.Category.HasValue), "An empty config warning should be emitted.");
        }

        public static void SpecificRuleReturnsConfiguredPickup()
        {
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(
                new LoadoutSelectionRequest(1, CreateConfig(LoadoutRuleConfig.CreateSpecific(PickupCategory.Passive, 42)), new int[0]));

            AssertEx.Equal(1, result.Selections.Length, "A specific rule should produce exactly one pickup.");
            AssertEx.Equal(42, result.Selections[0].PickupId, "A specific rule should return the configured pickup ID.");
        }

        public static void SpecificRuleWarnsWhenPickupAlreadyOwned()
        {
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(
                new LoadoutSelectionRequest(1, CreateConfig(LoadoutRuleConfig.CreateSpecific(PickupCategory.Passive, 42)), new[] { 42 }));

            AssertEx.Equal(0, result.Selections.Length, "Owned specific pickups should be skipped.");
            AssertEx.True(result.Warnings.Any(warning => warning.Code == "SpecificAlreadyOwned"), "Specific rules should warn when the pickup is already owned.");
        }

        public static void SpecificRulesRespectConfigOrderForDuplicateSelections()
        {
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(
                new LoadoutSelectionRequest(
                    1,
                    CreateConfig(
                        LoadoutRuleConfig.CreateSpecific(PickupCategory.Passive, 42),
                        LoadoutRuleConfig.CreateSpecific(PickupCategory.Active, 42)),
                    new int[0]));

            AssertEx.Equal(1, result.Selections.Length, "Later specific rules should not duplicate earlier selections.");
            AssertEx.Equal(PickupCategory.Passive, result.Selections[0].Category, "The earlier rule should keep the slot.");
            AssertEx.True(result.Warnings.Any(warning => warning.Code == "SpecificAlreadySelected"), "A duplicate specific rule should emit a warning.");
        }

        public static void MixedRulesRespectConfigOrder()
        {
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(
                new LoadoutSelectionRequest(
                    7,
                    CreateConfig(
                        LoadoutRuleConfig.CreateSpecific(PickupCategory.Gun, 5),
                        LoadoutRuleConfig.CreateRandom(PickupCategory.Passive, 1, new[] { 5, 6 })),
                    new int[0]));

            AssertEx.SequenceEqual(new[] { 5, 6 }, result.Selections.Select(selection => selection.PickupId), "Earlier rules should reserve pickup IDs for later rules.");
        }

        public static void InvalidSpecificRuleProducesWarning()
        {
            LoadoutSelectionService service = new LoadoutSelectionService();
            LoadoutSelectionResult result = service.SelectLoadout(
                new LoadoutSelectionRequest(1, CreateConfig(LoadoutRuleConfig.CreateSpecific(PickupCategory.Active, 0)), new int[0]));

            AssertEx.Equal(0, result.Selections.Length, "Invalid specific rules should not produce selections.");
            AssertEx.True(result.Warnings.Any(warning => warning.Code == "SpecificInvalidPickup"), "Invalid specific rules should emit a warning.");
        }

        private static LoadoutConfig CreateConfig(params LoadoutRuleConfig[] rules)
        {
            return new LoadoutConfig(rules);
        }

        private static string FormatSelection(SelectedPickup selection)
        {
            return selection.Category + ":" + selection.PickupId;
        }
    }
}
