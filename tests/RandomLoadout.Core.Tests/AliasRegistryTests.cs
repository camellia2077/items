using System;

namespace RandomLoadout.Core.Tests
{
    internal static class AliasRegistryTests
    {
        public static void AliasLookupIsCaseInsensitive()
        {
            string[] warnings = new string[0];
            PickupAliasRegistry registry = PickupAliasRegistry.Create(
                new[]
                {
                    new AliasEntryModel { Alias = "casey_bat", Id = 541 },
                },
                warnings,
                delegate(int candidatePickupId) { return candidatePickupId == 541; });

            int pickupId;
            AssertEx.True(registry.TryResolve("CASEY_BAT", out pickupId), "The alias registry should resolve aliases case-insensitively.");
            AssertEx.Equal(541, pickupId, "The alias registry should return the configured pickup ID.");
        }

        public static void DuplicateAliasKeepsFirstDefinition()
        {
            System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();
            PickupAliasRegistry registry = PickupAliasRegistry.Create(
                new[]
                {
                    new AliasEntryModel { Alias = "casey_bat", Id = 541 },
                    new AliasEntryModel { Alias = "casey_bat", Id = 616 },
                },
                warnings,
                delegate { return true; });

            int pickupId;
            AssertEx.True(registry.TryResolve("casey_bat", out pickupId), "The first duplicate alias should remain available.");
            AssertEx.Equal(541, pickupId, "The first alias definition should win.");
            AssertEx.True(warnings.Count == 1, "A duplicate alias should produce one warning.");
        }

        public static void NumericAliasIsRejected()
        {
            System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();
            PickupAliasRegistry registry = PickupAliasRegistry.Create(
                new[]
                {
                    new AliasEntryModel { Alias = "541", Id = 541 },
                },
                warnings,
                delegate { return true; });

            int pickupId;
            AssertEx.True(!registry.TryResolve("541", out pickupId), "Pure numeric aliases should be rejected.");
            AssertEx.True(warnings.Count == 1, "Rejecting a numeric alias should produce one warning.");
        }

        public static void UnsupportedPickupIdIsRejected()
        {
            System.Collections.Generic.List<string> warnings = new System.Collections.Generic.List<string>();
            PickupAliasRegistry registry = PickupAliasRegistry.Create(
                new[]
                {
                    new AliasEntryModel { Alias = "bad_alias", Id = 9999 },
                },
                warnings,
                delegate(int candidatePickupId) { return candidatePickupId == 541; });

            int pickupId;
            AssertEx.True(!registry.TryResolve("bad_alias", out pickupId), "Aliases with unsupported pickup IDs should be rejected.");
            AssertEx.True(warnings.Count == 1, "Rejecting an unsupported pickup ID should produce one warning.");
        }
    }
}
