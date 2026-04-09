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

        public LoadoutConfigResolutionResult Resolve(LoadoutRuleDefinition[] definitions, PickupAliasRegistry aliasRegistry)
        {
            PickupAliasRegistry effectiveAliasRegistry = aliasRegistry ?? PickupAliasRegistry.Empty;
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
                        List<int> resolvedPoolIds = new List<int>();
                        HashSet<int> seenPoolIds = new HashSet<int>();

                        for (int poolIdIndex = 0; poolIdIndex < definition.PoolIds.Length; poolIdIndex++)
                        {
                            EtgPickupResolveResult idResolveResult = _pickupResolver.Resolve(definition.Category, definition.PoolIds[poolIdIndex]);
                            if (idResolveResult.Succeeded)
                            {
                                if (seenPoolIds.Add(idResolveResult.PickupId))
                                {
                                    resolvedPoolIds.Add(idResolveResult.PickupId);
                                }
                            }
                            else if (idResolveResult.Warning != null)
                            {
                                warnings.Add(idResolveResult.Warning);
                            }
                        }

                        for (int poolAliasIndex = 0; poolAliasIndex < definition.PoolAliases.Length; poolAliasIndex++)
                        {
                            string pickupAlias = definition.PoolAliases[poolAliasIndex];
                            int resolvedAliasPickupId;
                            if (!effectiveAliasRegistry.TryResolve(pickupAlias, out resolvedAliasPickupId))
                            {
                                warnings.Add(
                                    new SelectionWarning(
                                        definition.Category,
                                        "RandomAliasNotFound",
                                        "No pickup alias matched '" + pickupAlias + "'."));
                                continue;
                            }

                            EtgPickupResolveResult aliasResolveResult = _pickupResolver.Resolve(definition.Category, resolvedAliasPickupId);
                            if (aliasResolveResult.Succeeded)
                            {
                                if (seenPoolIds.Add(aliasResolveResult.PickupId))
                                {
                                    resolvedPoolIds.Add(aliasResolveResult.PickupId);
                                }
                            }
                            else if (aliasResolveResult.Warning != null)
                            {
                                warnings.Add(
                                    new SelectionWarning(
                                        definition.Category,
                                        aliasResolveResult.Warning.Code,
                                        "Alias '" + pickupAlias + "' resolved to pickup ID " + resolvedAliasPickupId + ", but " +
                                        aliasResolveResult.Warning.Message));
                            }
                        }

                        for (int poolIndex = 0; poolIndex < definition.PoolNames.Length; poolIndex++)
                        {
                            string pickupName = definition.PoolNames[poolIndex];
                            // String pools now follow give-style resolution: internal name first,
                            // display name as a compatibility fallback.
                            EtgPickupResolveResult randomResolveResult = _pickupResolver.Resolve(definition.Category, pickupName);
                            if (randomResolveResult.Succeeded)
                            {
                                if (seenPoolIds.Add(randomResolveResult.PickupId))
                                {
                                    resolvedPoolIds.Add(randomResolveResult.PickupId);
                                }
                            }
                            else if (randomResolveResult.Warning != null)
                            {
                                warnings.Add(randomResolveResult.Warning);
                            }
                        }

                        rules.Add(LoadoutRuleConfig.CreateRandom(definition.Category, definition.Count, resolvedPoolIds));
                        break;
                    case GrantMode.Specific:
                        EtgPickupResolveResult resolveResult = ResolveSpecificDefinition(definition, effectiveAliasRegistry);
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

        private EtgPickupResolveResult ResolveSpecificDefinition(LoadoutRuleDefinition definition, PickupAliasRegistry aliasRegistry)
        {
            if (definition.SpecificPickupId.HasValue)
            {
                return _pickupResolver.Resolve(definition.Category, definition.SpecificPickupId.Value);
            }

            if (!string.IsNullOrEmpty(definition.SpecificAlias))
            {
                int resolvedAliasPickupId;
                if (!aliasRegistry.TryResolve(definition.SpecificAlias, out resolvedAliasPickupId))
                {
                    return new EtgPickupResolveResult(
                        false,
                        definition.Category,
                        0,
                        string.Empty,
                        new SelectionWarning(
                            definition.Category,
                            "SpecificAliasNotFound",
                            "No pickup alias matched '" + definition.SpecificAlias + "'."));
                }

                EtgPickupResolveResult aliasResolveResult = _pickupResolver.Resolve(definition.Category, resolvedAliasPickupId);
                if (!aliasResolveResult.Succeeded && aliasResolveResult.Warning != null)
                {
                    return new EtgPickupResolveResult(
                        false,
                        definition.Category,
                        0,
                        string.Empty,
                        new SelectionWarning(
                            definition.Category,
                            aliasResolveResult.Warning.Code,
                            "Alias '" + definition.SpecificAlias + "' resolved to pickup ID " + resolvedAliasPickupId + ", but " +
                            aliasResolveResult.Warning.Message));
                }

                return aliasResolveResult;
            }

            return _pickupResolver.Resolve(definition.Category, definition.SpecificName);
        }
    }
}
