using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed partial class GrantCommandService
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
                return CreatePlayerNotReadyResult();
            }

            EtgPickupResolveResult resolveResult = ResolvePickup(request);
            if (!resolveResult.Succeeded)
            {
                return CreateResolveFailureResult(resolveResult, "Failed to resolve the pickup.");
            }

            if (!resolveResult.Category.HasValue)
            {
                return CreateMissingCategoryResult("The resolved pickup category was missing.");
            }

            EtgGrantOutcome outcome = _pickupGranter.Grant(player, new SelectedPickup(resolveResult.Category.Value, resolveResult.PickupId));
            return outcome.Succeeded
                ? CreateGrantSuccessResult(outcome, false)
                : CreateGrantFailureResult(resolveResult.PickupLabel, outcome);
        }

        public GrantCommandExecutionResult ExecuteRandom(PlayerController player)
        {
            if ((object)player == null)
            {
                return CreatePlayerNotReadyResult();
            }

            EtgPickupResolveResult resolveResult = _pickupResolver.ResolveRandomGrantable(_random.Next());
            if (!resolveResult.Succeeded)
            {
                return CreateResolveFailureResult(resolveResult, "Failed to resolve a random pickup.");
            }

            if (!resolveResult.Category.HasValue)
            {
                return CreateMissingCategoryResult("The resolved random pickup category was missing.");
            }

            EtgGrantOutcome outcome = _pickupGranter.Grant(player, new SelectedPickup(resolveResult.Category.Value, resolveResult.PickupId));
            return outcome.Succeeded
                ? CreateRandomGrantSuccessResult(outcome)
                : CreateRandomGrantFailureResult(resolveResult.PickupLabel, outcome);
        }

        public GrantCommandExecutionResult ExecuteCatalogEntry(PlayerController player, EtgPickupCatalogEntry entry)
        {
            if ((object)player == null)
            {
                return CreatePlayerNotReadyResult();
            }

            if (entry == null)
            {
                return new GrantCommandExecutionResult(false, "The selected pickup entry was missing.");
            }

            EtgGrantOutcome outcome = _pickupGranter.Grant(player, new SelectedPickup(entry.Category, entry.PickupId));
            return outcome.Succeeded
                ? CreateGrantSuccessResult(outcome, true)
                : CreateGrantFailureResult(entry.DisplayName, outcome);
        }
    }
}
