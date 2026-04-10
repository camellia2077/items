using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed partial class GrantCommandService
    {
        private EtgPickupResolveResult ResolvePickup(GrantCommandRequest request)
        {
            int pickupId;
            if (TryParseLeadingPickupId(request.PickupName, out pickupId))
            {
                return ResolvePickupById(request.Target, pickupId);
            }

            PickupAliasRegistry aliasRegistry = GetAliasRegistry();
            if (aliasRegistry.TryResolve(request.PickupName, out pickupId))
            {
                return ResolvePickupById(request.Target, pickupId);
            }

            EtgPickupResolveResult resolveResult;
            switch (request.Target)
            {
                case GrantCommandTarget.Gun:
                    resolveResult = _pickupResolver.Resolve(PickupCategory.Gun, request.PickupName);
                    break;
                case GrantCommandTarget.Passive:
                    resolveResult = _pickupResolver.Resolve(PickupCategory.Passive, request.PickupName);
                    break;
                case GrantCommandTarget.Active:
                    resolveResult = _pickupResolver.Resolve(PickupCategory.Active, request.PickupName);
                    break;
                case GrantCommandTarget.Any:
                    resolveResult = _pickupResolver.ResolveAny(request.PickupName);
                    break;
                default:
                    return new EtgPickupResolveResult(false, null, 0, string.Empty, new SelectionWarning(null, "CommandTargetUnsupported", "The command target was not supported."));
            }

            if (!resolveResult.Succeeded &&
                resolveResult.Warning != null &&
                (string.Equals(resolveResult.Warning.Code, "InternalNameAmbiguous", System.StringComparison.Ordinal) ||
                 string.Equals(resolveResult.Warning.Code, "DisplayNameAmbiguous", System.StringComparison.Ordinal)))
            {
                string aliasSuggestion = BuildAliasSuggestion(request, aliasRegistry);
                return new EtgPickupResolveResult(
                    false,
                    resolveResult.Category,
                    0,
                    string.Empty,
                    new SelectionWarning(
                        resolveResult.Warning.Category,
                        resolveResult.Warning.Code,
                        resolveResult.Warning.Message + aliasSuggestion));
            }

            return resolveResult;
        }

        private EtgPickupResolveResult ResolvePickupById(GrantCommandTarget target, int pickupId)
        {
            switch (target)
            {
                case GrantCommandTarget.Gun:
                    return _pickupResolver.Resolve(PickupCategory.Gun, pickupId);
                case GrantCommandTarget.Passive:
                    return _pickupResolver.Resolve(PickupCategory.Passive, pickupId);
                case GrantCommandTarget.Active:
                    return _pickupResolver.Resolve(PickupCategory.Active, pickupId);
                case GrantCommandTarget.Any:
                    return _pickupResolver.ResolveAny(pickupId);
                default:
                    return new EtgPickupResolveResult(false, null, 0, string.Empty, new SelectionWarning(null, "CommandTargetUnsupported", "The command target was not supported."));
            }
        }

        private static bool TryParseLeadingPickupId(string rawValue, out int pickupId)
        {
            pickupId = 0;
            if (string.IsNullOrEmpty(rawValue))
            {
                return false;
            }

            string trimmed = rawValue.Trim();
            if (trimmed.Length == 0)
            {
                return false;
            }

            string[] parts = trimmed.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            return int.TryParse(parts[0], out pickupId);
        }

        private PickupAliasRegistry GetAliasRegistry()
        {
            PickupAliasRegistry aliasRegistry = _aliasRegistryProvider != null ? _aliasRegistryProvider() : PickupAliasRegistry.Empty;
            return aliasRegistry ?? PickupAliasRegistry.Empty;
        }
    }
}
