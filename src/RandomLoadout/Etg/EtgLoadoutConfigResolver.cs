using System.Collections.Generic;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class EtgLoadoutConfigResolver
    {
        private readonly EtgPickupResolver _pickupResolver;

        public EtgLoadoutConfigResolver(EtgPickupResolver pickupResolver)
        {
            _pickupResolver = pickupResolver;
        }

        public LoadoutConfigResolutionResult Resolve(LoadoutRuleDefinition[] definitions)
        {
            List<LoadoutRuleConfig> rules = new List<LoadoutRuleConfig>();
            List<SelectionWarning> warnings = new List<SelectionWarning>();

            if (definitions == null)
            {
                warnings.Add(new SelectionWarning(null, "ConfigEmpty", "No loadout rules were configured."));
                return new LoadoutConfigResolutionResult(new LoadoutConfig(new LoadoutRuleConfig[0]), warnings.ToArray());
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                LoadoutRuleDefinition definition = definitions[i];
                if (definition == null)
                {
                    warnings.Add(new SelectionWarning(null, "NullRuleDefinition", "Encountered a null raw loadout rule definition."));
                    continue;
                }

                switch (definition.Mode)
                {
                    case GrantMode.Random:
                        rules.Add(LoadoutRuleConfig.CreateRandom(definition.Category, definition.Count, definition.PoolIds));
                        break;
                    case GrantMode.Specific:
                        EtgPickupResolveResult resolveResult = _pickupResolver.Resolve(definition.Category, definition.SpecificName);
                        if (resolveResult.Succeeded)
                        {
                            rules.Add(LoadoutRuleConfig.CreateSpecific(definition.Category, resolveResult.PickupId));
                        }
                        else if (resolveResult.Warning != null)
                        {
                            warnings.Add(resolveResult.Warning);
                        }

                        break;
                    default:
                        warnings.Add(new SelectionWarning(definition.Category, "UnsupportedGrantMode", "The raw loadout rule definition used an unsupported grant mode."));
                        break;
                }
            }

            return new LoadoutConfigResolutionResult(new LoadoutConfig(rules), warnings.ToArray());
        }
    }
}
