using System.Collections;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string GUID = "randomgun.randomloadout";
        public const string NAME = "RandomLoadout";
        public const string VERSION = "0.2.0";

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

        private void Awake()
        {
            _enableRandomLoadoutConfig = Config.Bind(
                "General",
                "EnableRandomLoadout",
                true,
                "Enable or disable the automatic start-of-run loadout grant.");
            _aliasRegistry = PickupAliasRegistry.Empty;
            _configResolver = new EtgLoadoutConfigResolver(_pickupResolver);
            _pickupCatalogExporter = new EtgPickupCatalogExporter(
                Path.Combine(Paths.ConfigPath, PickupCatalogTextFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogGroupedJsonFileName),
                Path.Combine(Paths.ConfigPath, PickupCatalogRulePoolFileName));
            _aliasFileProvider = new JsonPickupAliasFileProvider(Path.Combine(Paths.ConfigPath, NAME + ".aliases.json5"));
            _rapidFireToggleService = new RapidFireToggleService();
            _commandController = new InGameCommandController(
                new GrantCommandService(_pickupResolver, _pickupGranter, GetAliasRegistry),
                new PlayerDebugCommandService(),
                new FoyerCharacterSwitchService(),
                _rapidFireToggleService);
            _ruleFileProvider = new JsonLoadoutRuleFileProvider(
                Path.Combine(Paths.ConfigPath, NAME + ".rules.json5"),
                Path.Combine(Paths.ConfigPath, PickupCatalogRulePoolFileName));
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _runState = new RunGrantState();
            _runLifecycleTracker = new RunLifecycleTracker(BreachSceneName, LegacyBreachSceneName, LoadingSceneName);
            _sceneWatcher = new RunSceneWatcher(BreachSceneName);

            Logger.LogInfo(RandomLoadoutLog.Init("Waiting for GameManager startup."));
            Logger.LogInfo(RandomLoadoutLog.Init("Automatic random loadout is " + (_enableRandomLoadoutConfig.Value ? "enabled" : "disabled") + "."));
            StartCoroutine(WaitForGameManagerAndSubscribe());
        }

        private void OnDestroy()
        {
            if (_rapidFireToggleService != null)
            {
                _rapidFireToggleService.Reset();
            }

            if (_sceneWatcher != null)
            {
                _sceneWatcher.Unsubscribe(OnNewLevelFullyLoaded);
            }
        }

        private void Update()
        {
            TryExportPickupCatalogOnce();

            PlayerController player = null;
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager != null)
            {
                player = gameManager.PrimaryPlayer;
            }

            if (_commandController != null)
            {
                _commandController.Update();
            }

            if (_rapidFireToggleService != null)
            {
                _rapidFireToggleService.Update(player);
            }

            if (_sceneWatcher == null || !_sceneWatcher.IsPollDue(Time.unscaledTime))
            {
                return;
            }

            _sceneWatcher.MarkPolled(Time.unscaledTime);
            TryHandleCurrentScene("poll");
        }

        private IEnumerator WaitForGameManagerAndSubscribe()
        {
            while ((object)GameManager.Instance == null)
            {
                yield return null;
            }

            EnsureAliasRegistryLoaded();
            _sceneWatcher.Subscribe(GameManager.Instance, OnNewLevelFullyLoaded);
            TryExportPickupCatalogOnce();
            Logger.LogInfo(RandomLoadoutLog.Init(NAME + " v" + VERSION + " started successfully."));
        }

        private void OnNewLevelFullyLoaded()
        {
            TryHandleCurrentScene("event");
        }

        private void TryHandleCurrentScene(string source)
        {
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager == null)
            {
                return;
            }

            string sceneName;
            if (!_sceneWatcher.TryGetCurrentSceneName(gameManager, out sceneName))
            {
                return;
            }
            PlayerController player = gameManager.PrimaryPlayer;
            int playerInstanceId = (object)player != null ? player.GetInstanceID() : 0;
            RunLifecycleObservation lifecycle = _runLifecycleTracker.Observe(sceneName, playerInstanceId);
            if (lifecycle.SceneChanged)
            {
                Logger.LogInfo(RandomLoadoutLog.Run("Observed scene change via " + source + ": " + lifecycle.PreviousSceneName + " -> " + lifecycle.SceneName));
            }

            if (lifecycle.ResetKind == RunLifecycleResetKind.PrimaryPlayerChanged)
            {
                if (_runState.HasGrantedThisRun || _runState.CurrentSeed != 0)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Detected a new PrimaryPlayer instance in scene " + lifecycle.SceneName + ". Resetting run grant state."));
                }

                _runState.Reset();
            }

            if (lifecycle.ResetKind == RunLifecycleResetKind.EnteredBreach)
            {
                if (_runState.HasGrantedThisRun || _runState.CurrentSeed != 0)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Entered breach. Resetting run grant state."));
                }

                _runState.Reset();
                return;
            }

            if (!lifecycle.IsGrantableDungeonScene)
            {
                return;
            }

            if (lifecycle.ShouldScheduleGrant)
            {
                _runState.ScheduleGrant(Time.unscaledTime, GrantDelaySeconds);
                if (lifecycle.PlayerChanged && !lifecycle.SceneChanged)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("PrimaryPlayer changed inside scene " + lifecycle.SceneName + ". Delaying loadout grant by " + GrantDelaySeconds + " seconds."));
                }
                else
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Scene " + lifecycle.SceneName + " entered. Delaying loadout grant by " + GrantDelaySeconds + " seconds."));
                }
            }

            if (_runState.HasGrantedThisRun)
            {
                return;
            }

            if (!_enableRandomLoadoutConfig.Value)
            {
                if (lifecycle.SceneChanged)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Automatic random loadout is disabled by config."));
                }

                return;
            }

            if ((object)player == null)
            {
                if (lifecycle.SceneChanged)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Scene " + lifecycle.SceneName + " is active, but PrimaryPlayer is not ready yet."));
                }

                return;
            }

            if (!_runState.IsGrantReady(Time.unscaledTime))
            {
                return;
            }

            GrantConfiguredLoadout(player, lifecycle.SceneName);
        }

        private void GrantConfiguredLoadout(PlayerController player, string sceneName)
        {
            EnsureResolvedLoadoutConfig();

            int seed = _seedProvider.CreateSeed();
            LoadoutSelectionResult selectionResult = _selectionService.SelectLoadout(
                new LoadoutSelectionRequest(seed, _resolvedLoadoutConfig, _ownedPickupReader.CollectOwnedPickupIds(player)));

            _runState.MarkGranted(selectionResult.Seed);
            Logger.LogInfo(RandomLoadoutLog.Grant("Granting configured loadout. Scene=" + sceneName + ", Seed=" + selectionResult.Seed));

            LogSelectionWarnings(selectionResult.Warnings);

            for (int i = 0; i < selectionResult.Selections.Length; i++)
            {
                SelectedPickup selection = selectionResult.Selections[i];
                EtgGrantOutcome outcome = _pickupGranter.Grant(player, selection);
                if (outcome.Succeeded)
                {
                    Logger.LogInfo(
                        RandomLoadoutLog.Grant(
                            "Granted " + outcome.Category + ": " + outcome.PickupLabel + " (ID " + outcome.PickupId + "). " +
                            "[Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]"));
                    continue;
                }

                Logger.LogWarning(
                    RandomLoadoutLog.Grant(
                        "Failed to grant " + outcome.Category + " pickup ID " + outcome.PickupId + " (" + outcome.PickupLabel + "): " +
                        outcome.FailureReason + " [Path=" + outcome.GrantPath + "; Detail=" + outcome.GrantDetail + "]"));
            }

            if (selectionResult.Selections.Length == 0)
            {
                Logger.LogWarning(RandomLoadoutLog.Grant("No pickups were selected for this run."));
            }
        }

        private void OnGUI()
        {
            if (_commandController != null)
            {
                PlayerController player = null;
                GameManager gameManager = GameManager.Instance;
                if ((object)gameManager != null)
                {
                    player = gameManager.PrimaryPlayer;
                }

                _commandController.OnGUI(player, Logger);
            }
        }

        private void EnsureResolvedLoadoutConfig()
        {
            if (_hasResolvedLoadoutConfig)
            {
                return;
            }

            EnsureAliasRegistryLoaded();

            LoadoutRuleFileLoadResult ruleFileLoadResult = _ruleFileProvider.Load();
            _ruleDefinitions = ruleFileLoadResult.Definitions;
            for (int i = 0; i < ruleFileLoadResult.Messages.Length; i++)
            {
                Logger.LogInfo(RandomLoadoutLog.Init(ruleFileLoadResult.Messages[i]));
            }

            for (int i = 0; i < ruleFileLoadResult.Warnings.Length; i++)
            {
                Logger.LogWarning(RandomLoadoutLog.Init(ruleFileLoadResult.Warnings[i]));
            }

            LoadoutConfigResolutionResult resolutionResult = _configResolver.Resolve(_ruleDefinitions, _aliasRegistry);
            _resolvedLoadoutConfig = resolutionResult.Config;
            _hasResolvedLoadoutConfig = true;

            LogSelectionWarnings(resolutionResult.Warnings);
        }

        private void LogSelectionWarnings(SelectionWarning[] warnings)
        {
            for (int i = 0; i < warnings.Length; i++)
            {
                SelectionWarning warning = warnings[i];
                string categoryPrefix = warning.Category.HasValue ? warning.Category.Value + ": " : string.Empty;
                Logger.LogWarning(RandomLoadoutLog.Grant(categoryPrefix + warning.Message + " [Code=" + warning.Code + "]"));
            }
        }

        private PickupAliasRegistry GetAliasRegistry()
        {
            if (!_hasLoadedAliasRegistry)
            {
                EnsureAliasRegistryLoaded();
            }

            return _aliasRegistry ?? PickupAliasRegistry.Empty;
        }

        private void EnsureAliasRegistryLoaded()
        {
            if (_hasLoadedAliasRegistry || _aliasFileProvider == null)
            {
                return;
            }

            if ((object)GameManager.Instance == null)
            {
                return;
            }

            AliasLoadResult aliasLoadResult = _aliasFileProvider.Load(IsSupportedGrantablePickupId);
            _aliasRegistry = aliasLoadResult.Registry ?? PickupAliasRegistry.Empty;
            _hasLoadedAliasRegistry = true;

            for (int i = 0; i < aliasLoadResult.Messages.Length; i++)
            {
                Logger.LogInfo(RandomLoadoutLog.Alias(aliasLoadResult.Messages[i]));
            }

            for (int i = 0; i < aliasLoadResult.Warnings.Length; i++)
            {
                Logger.LogWarning(RandomLoadoutLog.Alias(aliasLoadResult.Warnings[i]));
            }
        }

        private bool IsSupportedGrantablePickupId(int pickupId)
        {
            return _pickupResolver.ResolveAny(pickupId).Succeeded;
        }

        private void TryExportPickupCatalogOnce()
        {
            if (_hasExportedPickupCatalog || _pickupCatalogExporter == null)
            {
                return;
            }

            EtgPickupCatalogExportResult exportResult = _pickupCatalogExporter.Export(_pickupResolver);
            if (exportResult.Succeeded)
            {
                _hasExportedPickupCatalog = true;
                _lastPickupCatalogExportFailure = null;
                Logger.LogInfo(
                    RandomLoadoutLog.Init(
                        "Exported grantable pickup catalog to '" + exportResult.TextOutputPath + "', '" + exportResult.JsonOutputPath + "', '" + exportResult.GroupedJsonOutputPath + "', and '" + exportResult.RulePoolOutputPath + "' (" + exportResult.EntryCount + " entries)."));
                return;
            }

            if (!string.Equals(_lastPickupCatalogExportFailure, exportResult.FailureReason, System.StringComparison.Ordinal))
            {
                _lastPickupCatalogExportFailure = exportResult.FailureReason;
                Logger.LogWarning(RandomLoadoutLog.Init("Failed to export grantable pickup catalog: " + exportResult.FailureReason));
            }
        }

    }
}
