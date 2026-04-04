using System;

namespace RandomLoadout
{
    internal sealed class RunLifecycleTracker
    {
        private readonly string _breachSceneName;
        private readonly string _legacyBreachSceneName;
        private readonly string _loadingSceneName;

        private string _lastObservedSceneName;
        private int _lastObservedPlayerInstanceId;

        public RunLifecycleTracker(string breachSceneName, string legacyBreachSceneName, string loadingSceneName)
        {
            _breachSceneName = breachSceneName;
            _legacyBreachSceneName = legacyBreachSceneName;
            _loadingSceneName = loadingSceneName;
            _lastObservedSceneName = string.Empty;
        }

        public RunLifecycleObservation Observe(string sceneName, int playerInstanceId)
        {
            string normalizedSceneName = sceneName ?? string.Empty;
            string previousSceneName = _lastObservedSceneName;
            bool sceneChanged = !string.Equals(previousSceneName, normalizedSceneName, StringComparison.Ordinal);
            _lastObservedSceneName = normalizedSceneName;

            bool playerChanged = false;
            if (playerInstanceId != 0)
            {
                playerChanged = _lastObservedPlayerInstanceId != 0 && _lastObservedPlayerInstanceId != playerInstanceId;
                _lastObservedPlayerInstanceId = playerInstanceId;
            }

            RunLifecycleResetKind resetKind = RunLifecycleResetKind.None;
            if (IsResetScene(normalizedSceneName))
            {
                resetKind = RunLifecycleResetKind.EnteredBreach;
                _lastObservedSceneName = _breachSceneName;
                _lastObservedPlayerInstanceId = 0;
                playerChanged = false;
            }
            else if (playerChanged)
            {
                resetKind = RunLifecycleResetKind.PrimaryPlayerChanged;
            }

            return new RunLifecycleObservation(
                normalizedSceneName,
                previousSceneName,
                sceneChanged,
                playerChanged,
                IsGrantableDungeonScene(normalizedSceneName),
                resetKind);
        }

        private bool IsResetScene(string sceneName)
        {
            return string.Equals(sceneName, _breachSceneName, StringComparison.Ordinal) ||
                   string.Equals(sceneName, _legacyBreachSceneName, StringComparison.Ordinal);
        }

        private bool IsGrantableDungeonScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            if (IsResetScene(sceneName))
            {
                return false;
            }

            if (string.Equals(sceneName, _loadingSceneName, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }
    }
}
