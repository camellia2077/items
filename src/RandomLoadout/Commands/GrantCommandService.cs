using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class GrantCommandService
    {
        private readonly EtgPickupResolver _pickupResolver;
        private readonly EtgPickupGranter _pickupGranter;
        private readonly System.Random _random = new System.Random();

        public GrantCommandService(EtgPickupResolver pickupResolver, EtgPickupGranter pickupGranter)
        {
            _pickupResolver = pickupResolver;
            _pickupGranter = pickupGranter;
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
                return new GrantCommandExecutionResult(false, "Failed to grant " + resolveResult.PickupLabel + ": " + outcome.FailureReason);
            }

            return new GrantCommandExecutionResult(true, "Granted " + outcome.Category + ": " + outcome.PickupLabel + ".");
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
                return new GrantCommandExecutionResult(false, "Failed to grant random pickup " + resolveResult.PickupLabel + ": " + outcome.FailureReason);
            }

            return new GrantCommandExecutionResult(
                true,
                "Granted random " + outcome.Category + ": " + outcome.PickupLabel + " (ID " + outcome.PickupId + ").");
        }

        private EtgPickupResolveResult ResolvePickup(GrantCommandRequest request)
        {
            switch (request.Target)
            {
                case GrantCommandTarget.Gun:
                    return _pickupResolver.Resolve(PickupCategory.Gun, request.PickupName);
                case GrantCommandTarget.Passive:
                    return _pickupResolver.Resolve(PickupCategory.Passive, request.PickupName);
                case GrantCommandTarget.Active:
                    return _pickupResolver.Resolve(PickupCategory.Active, request.PickupName);
                case GrantCommandTarget.Any:
                    return _pickupResolver.ResolveAny(request.PickupName);
                default:
                    return new EtgPickupResolveResult(false, null, 0, string.Empty, new SelectionWarning(null, "CommandTargetUnsupported", "The command target was not supported."));
            }
        }
    }
}
