using System;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class RunSceneWatcher
    {
        private const float PollIntervalSeconds = 0.5f;

        private readonly string _breachSceneName;
        private bool _isSubscribed;
        private float _nextScenePollTime;
        private GameManager _subscribedGameManager;

        public RunSceneWatcher(string breachSceneName)
        {
            _breachSceneName = breachSceneName;
        }

        public void Subscribe(GameManager gameManager, Action onNewLevelLoaded)
        {
            if ((object)gameManager == null)
            {
                throw new ArgumentNullException("gameManager");
            }

            if (onNewLevelLoaded == null)
            {
                throw new ArgumentNullException("onNewLevelLoaded");
            }

            if (_isSubscribed)
            {
                return;
            }

            gameManager.OnNewLevelFullyLoaded += onNewLevelLoaded;
            _subscribedGameManager = gameManager;
            _isSubscribed = true;
        }

        public void Unsubscribe(Action onNewLevelLoaded)
        {
            if (!_isSubscribed || onNewLevelLoaded == null)
            {
                return;
            }

            if ((object)_subscribedGameManager != null)
            {
                _subscribedGameManager.OnNewLevelFullyLoaded -= onNewLevelLoaded;
            }

            _subscribedGameManager = null;
            _isSubscribed = false;
        }

        public bool IsPollDue(float unscaledTime)
        {
            return unscaledTime >= _nextScenePollTime;
        }

        public void MarkPolled(float unscaledTime)
        {
            _nextScenePollTime = unscaledTime + PollIntervalSeconds;
        }

        public bool TryGetCurrentSceneName(GameManager gameManager, out string sceneName)
        {
            sceneName = string.Empty;

            if ((object)gameManager != null)
            {
                try
                {
                    GameLevelDefinition levelDefinition = gameManager.GetLastLoadedLevelDefinition();
                    if (levelDefinition != null && !string.IsNullOrEmpty(levelDefinition.dungeonSceneName))
                    {
                        sceneName = levelDefinition.dungeonSceneName;
                        return true;
                    }
                }
                catch (NullReferenceException)
                {
                    // ETG can briefly expose a GameManager instance before its level definition state is ready.
                    // Fall back to the loaded Unity scene name during that startup window.
                }
            }

#pragma warning disable 618
            sceneName = Application.loadedLevelName;
#pragma warning restore 618
            return !string.IsNullOrEmpty(sceneName);
        }

        public bool IsBreachScene(string sceneName)
        {
            return string.Equals(sceneName, _breachSceneName, StringComparison.Ordinal);
        }
    }
}
