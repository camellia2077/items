using System.Collections;
using System.IO;
using BepInEx;
using HarmonyLib;
using RandomLoadout.Core;

namespace RandomLoadout
{
    public sealed partial class Plugin
    {
        private void Awake()
        {
            GuiText.Initialize(Paths.ConfigPath);
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
            _bossRushService = new BossRushService(Logger);
            _commandController = new InGameCommandController(
                new GrantCommandService(_pickupResolver, _pickupGranter, GetAliasRegistry),
                new PlayerDebugCommandService(),
                new FoyerCharacterSwitchService(),
                _bossRushService,
                _rapidFireToggleService,
                _pickupResolver.GetGrantablePickupCatalog,
                GetAliasRegistry);
            _ruleFileProvider = new JsonLoadoutRuleFileProvider(
                Path.Combine(Paths.ConfigPath, NAME + ".rules.json5"),
                Path.Combine(Paths.ConfigPath, PickupCatalogRulePoolFileName));
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _runState = new RunGrantState();
            _runLifecycleTracker = new RunLifecycleTracker(CharacterSelectSceneName, LegacyCharacterSelectSceneName, LoadingSceneName);
            _sceneWatcher = new RunSceneWatcher(CharacterSelectSceneName);
            _bossRushHarmony = new Harmony(GUID + ".bossrush");

            Logger.LogInfo(RandomLoadoutLog.Init("Waiting for GameManager startup."));
            Logger.LogInfo(RandomLoadoutLog.Init("Automatic random loadout is " + (_enableRandomLoadoutConfig.Value ? "enabled" : "disabled") + "."));
            Logger.LogInfo(RandomLoadoutLog.Init("Boss Rush service initialized. Startup self-check is running."));
            LogBossRushHookSelfCheck(BossRushHooks.Install(_bossRushHarmony, Logger));
            StartCoroutine(WaitForGameManagerAndSubscribe());
        }

        private void OnDestroy()
        {
            if (_rapidFireToggleService != null)
            {
                _rapidFireToggleService.Reset();
            }

            if (_bossRushService != null)
            {
                _bossRushService.Dispose();
            }

            if (_bossRushHarmony != null)
            {
                _bossRushHarmony.UnpatchSelf();
            }

            if (_sceneWatcher != null)
            {
                _sceneWatcher.Unsubscribe(OnNewLevelFullyLoaded);
            }
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
            Logger.LogInfo(RandomLoadoutLog.Init("GameManager startup detected. Scene watcher subscribed and GUI controller is ready."));
            Logger.LogInfo(RandomLoadoutLog.Init(NAME + " v" + VERSION + " started successfully."));
        }

        private void LogBossRushHookSelfCheck(BossRushHookInstallReport report)
        {
            if (report == null)
            {
                Logger.LogWarning(RandomLoadoutLog.Init("Boss Rush startup self-check did not produce a hook report."));
                return;
            }

            Logger.LogInfo(
                RandomLoadoutLog.Init(
                    "Boss Rush startup self-check complete. Applied hooks=" +
                    report.AppliedCount +
                    ", Skipped hooks=" +
                    report.SkippedCount +
                    "."));

            if (!report.HasSkippedHooks)
            {
                Logger.LogInfo(RandomLoadoutLog.Init("Boss Rush startup self-check passed."));
                return;
            }

            string[] skippedHooks = report.SkippedHooks;
            for (int i = 0; i < skippedHooks.Length; i++)
            {
                Logger.LogWarning(RandomLoadoutLog.Init("Boss Rush startup self-check warning: " + skippedHooks[i]));
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
    }
}
