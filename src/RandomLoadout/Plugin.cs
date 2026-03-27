using System.Collections;
using BepInEx;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string GUID = "randomgun.randomloadout";
        public const string NAME = "RandomLoadout";
        public const string VERSION = "0.1.0";

        private const string BreachSceneName = "tt_breach";
        private const float GrantDelaySeconds = 1.5f;

        private readonly EtgPickupResolver _pickupResolver = new EtgPickupResolver();
        private readonly EtgOwnedPickupReader _ownedPickupReader = new EtgOwnedPickupReader();
        private readonly EtgPickupGranter _pickupGranter = new EtgPickupGranter();
        private readonly LoadoutSelectionService _selectionService = new LoadoutSelectionService();
        private readonly ISeedProvider _seedProvider = new UtcTickSeedProvider();

        private LoadoutRuleDefinition[] _ruleDefinitions;
        private LoadoutConfig _resolvedLoadoutConfig;
        private bool _hasResolvedLoadoutConfig;
        private EtgLoadoutConfigResolver _configResolver;
        private InGameCommandController _commandController;
        private RunGrantState _runState;
        private RunSceneWatcher _sceneWatcher;

        private void Awake()
        {
            _configResolver = new EtgLoadoutConfigResolver(_pickupResolver);
            _commandController = new InGameCommandController(new GrantCommandService(_pickupResolver, _pickupGranter));
            _ruleDefinitions = DefaultLoadoutRuleDefinitionFactory.CreateDefault();
            _runState = new RunGrantState(BreachSceneName);
            _sceneWatcher = new RunSceneWatcher(BreachSceneName);

            Logger.LogInfo(RandomLoadoutLog.Init("Waiting for GameManager startup."));
            StartCoroutine(WaitForGameManagerAndSubscribe());
        }

        private void OnDestroy()
        {
            if (_sceneWatcher != null)
            {
                _sceneWatcher.Unsubscribe(OnNewLevelFullyLoaded);
            }
        }

        private void Update()
        {
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

            _sceneWatcher.Subscribe(GameManager.Instance, OnNewLevelFullyLoaded);
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

            string previousSceneName = _runState.LastObservedSceneName;
            bool sceneChanged = _runState.ObserveScene(sceneName);
            if (sceneChanged)
            {
                Logger.LogInfo(RandomLoadoutLog.Run("Observed scene change via " + source + ": " + previousSceneName + " -> " + sceneName));
            }

            if (_sceneWatcher.IsBreachScene(sceneName))
            {
                if (_runState.HasGrantedThisRun || _runState.CurrentSeed != 0)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Entered breach. Resetting run grant state."));
                }

                _runState.ResetForBreach();
                return;
            }

            if (sceneChanged)
            {
                _runState.ScheduleGrant(Time.unscaledTime, GrantDelaySeconds);
                Logger.LogInfo(RandomLoadoutLog.Run("Scene " + sceneName + " entered. Delaying loadout grant by " + GrantDelaySeconds + " seconds."));
            }

            if (_runState.HasGrantedThisRun)
            {
                return;
            }

            PlayerController player = gameManager.PrimaryPlayer;
            if ((object)player == null)
            {
                if (sceneChanged)
                {
                    Logger.LogInfo(RandomLoadoutLog.Run("Scene " + sceneName + " is active, but PrimaryPlayer is not ready yet."));
                }

                return;
            }

            if (!_runState.IsGrantReady(Time.unscaledTime))
            {
                return;
            }

            GrantConfiguredLoadout(player, sceneName);
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
                    Logger.LogInfo(RandomLoadoutLog.Grant("Granted " + outcome.Category + ": " + outcome.PickupLabel + " (ID " + outcome.PickupId + ")."));
                    continue;
                }

                Logger.LogWarning(
                    RandomLoadoutLog.Grant(
                        "Failed to grant " + outcome.Category + " pickup ID " + outcome.PickupId + " (" + outcome.PickupLabel + "): " +
                        outcome.FailureReason));
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

            LoadoutConfigResolutionResult resolutionResult = _configResolver.Resolve(_ruleDefinitions);
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
    }
}
