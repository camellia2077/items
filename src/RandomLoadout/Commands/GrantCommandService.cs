using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class GrantCommandService
    {
        private readonly EtgPickupResolver _pickupResolver;
        private readonly EtgPickupGranter _pickupGranter;
        private readonly System.Func<PickupAliasRegistry> _aliasRegistryProvider;
        private readonly System.Random _random = new System.Random();

        public GrantCommandService(
            EtgPickupResolver pickupResolver,
            EtgPickupGranter pickupGranter,
            System.Func<PickupAliasRegistry> aliasRegistryProvider)
        {
            _pickupResolver = pickupResolver;
            _pickupGranter = pickupGranter;
            _aliasRegistryProvider = aliasRegistryProvider;
        }

        public GrantCommandExecutionResult Execute(PlayerController player, GrantCommandRequest request)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            EtgPickupResolveResult resolveResult = ResolvePickup(request);
            if (!resolveResult.Succeeded)
            {
                string message = resolveResult.Warning != null ? resolveResult.Warning.Message : "Failed to resolve the pickup.";
                return new GrantCommandExecutionResult(false, message);
            }

            if (!resolveResult.Category.HasValue)
            {
                return new GrantCommandExecutionResult(false, "The resolved pickup category was missing.");
            }

            EtgGrantOutcome outcome = _pickupGranter.Grant(player, new SelectedPickup(resolveResult.Category.Value, resolveResult.PickupId));
            if (!outcome.Succeeded)
            {
                return new GrantCommandExecutionResult(
                    false,
                    "Failed to grant " + resolveResult.PickupLabel + ": " + outcome.FailureReason +
                    " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]");
            }

            return new GrantCommandExecutionResult(
                true,
                "Granted " + outcome.Category + ": " + outcome.PickupLabel +
                " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]");
        }

        public GrantCommandExecutionResult ExecuteRandom(PlayerController player)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            EtgPickupResolveResult resolveResult = _pickupResolver.ResolveRandomGrantable(_random.Next());
            if (!resolveResult.Succeeded)
            {
                string message = resolveResult.Warning != null ? resolveResult.Warning.Message : "Failed to resolve a random pickup.";
                return new GrantCommandExecutionResult(false, message);
            }

            if (!resolveResult.Category.HasValue)
            {
                return new GrantCommandExecutionResult(false, "The resolved random pickup category was missing.");
            }

            EtgGrantOutcome outcome = _pickupGranter.Grant(player, new SelectedPickup(resolveResult.Category.Value, resolveResult.PickupId));
            if (!outcome.Succeeded)
            {
                return new GrantCommandExecutionResult(
                    false,
                    "Failed to grant random pickup " + resolveResult.PickupLabel + ": " + outcome.FailureReason +
                    " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]");
            }

            return new GrantCommandExecutionResult(
                true,
                "Granted random " + outcome.Category + ": " + outcome.PickupLabel + " (ID " + outcome.PickupId + ")." +
                " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]");
        }

        private EtgPickupResolveResult ResolvePickup(GrantCommandRequest request)
        {
            int pickupId;
            if (TryParseLeadingPickupId(request.PickupName, out pickupId))
            {
                return ResolvePickupById(request.Target, pickupId);
            }

            PickupAliasRegistry aliasRegistry = _aliasRegistryProvider != null ? _aliasRegistryProvider() : PickupAliasRegistry.Empty;
            if (aliasRegistry == null)
            {
                aliasRegistry = PickupAliasRegistry.Empty;
            }

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
                return new EtgPickupResolveResult(
                    false,
                    resolveResult.Category,
                    0,
                    string.Empty,
                    new SelectionWarning(
                        resolveResult.Warning.Category,
                        resolveResult.Warning.Code,
                        resolveResult.Warning.Message + " Try a configured alias or a pickup ID from RandomLoadout.pickups.txt, for example: gun casey_bat or gun 541."));
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
    }
}
