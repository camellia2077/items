using BepInEx.Configuration;
using RandomLoadout.Core;

namespace RandomLoadout
{
    public sealed partial class Plugin
    {
        private const string BreachSceneName = "tt_foyer";
        private const string LegacyBreachSceneName = "tt_breach";
        private const string LoadingSceneName = "LoadingDungeon";
        private const float GrantDelaySeconds = 1.5f;
        private const string PickupCatalogTextFileName = NAME + ".pickups.txt";
        private const string PickupCatalogJsonFileName = NAME + ".pickups.json";
        private const string PickupCatalogGroupedJsonFileName = NAME + ".pickups.by-category.json";
        private const string PickupCatalogRulePoolFileName = NAME + ".rules.full-pool.json5";

        private readonly EtgPickupResolver _pickupResolver = new EtgPickupResolver();
        private readonly EtgOwnedPickupReader _ownedPickupReader = new EtgOwnedPickupReader();
        private readonly EtgPickupGranter _pickupGranter = new EtgPickupGranter();
        private readonly LoadoutSelectionService _selectionService = new LoadoutSelectionService();
        private readonly ISeedProvider _seedProvider = new UtcTickSeedProvider();

        private ConfigEntry<bool> _enableRandomLoadoutConfig;
        private LoadoutRuleDefinition[] _ruleDefinitions;
        private LoadoutConfig _resolvedLoadoutConfig;
        private PickupAliasRegistry _aliasRegistry;
        private bool _hasLoadedAliasRegistry;
        private bool _hasResolvedLoadoutConfig;
        private JsonPickupAliasFileProvider _aliasFileProvider;
        private EtgLoadoutConfigResolver _configResolver;
        private EtgPickupCatalogExporter _pickupCatalogExporter;
        private JsonLoadoutRuleFileProvider _ruleFileProvider;
        private InGameCommandController _commandController;
        private RapidFireToggleService _rapidFireToggleService;
        private bool _hasExportedPickupCatalog;
        private string _lastPickupCatalogExportFailure;
        private RunGrantState _runState;
        private RunLifecycleTracker _runLifecycleTracker;
        private RunSceneWatcher _sceneWatcher;
    }
}
