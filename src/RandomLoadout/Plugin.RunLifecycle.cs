using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    public sealed partial class Plugin
    {
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
    }
}
