using RandomLoadout.Core;

namespace RandomLoadout
{
    internal static class DefaultLoadoutRuleDefinitionFactory
    {
        public static LoadoutRuleDefinition[] CreateDefault()
        {
            return new[]
            {
                LoadoutRuleDefinition.Random(
                    PickupCategory.Gun,
                    1,
                    new[]
                    {
                        15,
                        61,
                        80,
                        98,
                        181,
                        223,
                        251,
                    }),
                LoadoutRuleDefinition.Random(
                    PickupCategory.Passive,
                    1,
                    new[]
                    {
                        102,
                        111,
                        131,
                        134,
                        165,
                        204,
                        213,
                    }),
                LoadoutRuleDefinition.Random(
                    PickupCategory.Active,
                    1,
                    new[]
                    {
                        64,
                        69,
                        71,
                        77,
                        201,
                        250,
                    }),
            };
        }

        public static LoadoutRuleDefinition[] CreateMixedExample()
        {
            return new[]
            {
                LoadoutRuleDefinition.Random(
                    PickupCategory.Gun,
                    1,
                    new[]
                    {
                        15,
                        61,
                        80,
                        98,
                        181,
                        223,
                        251,
                    }),
                LoadoutRuleDefinition.Specific(PickupCategory.Passive, "Scope"),
                LoadoutRuleDefinition.Specific(PickupCategory.Active, "Bullet Time"),
            };
        }
    }
}
