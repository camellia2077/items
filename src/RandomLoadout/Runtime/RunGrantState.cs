using System;

namespace RandomLoadout
{
    internal sealed class RunGrantState
    {
        private readonly string _breachSceneName;

        public RunGrantState(string breachSceneName)
        {
            _breachSceneName = breachSceneName;
            LastObservedSceneName = string.Empty;
        }

        public bool HasGrantedThisRun { get; private set; }

        public string LastObservedSceneName { get; private set; }

        public int CurrentSeed { get; private set; }

        public float GrantReadyAtTime { get; private set; }

        public bool ObserveScene(string sceneName)
        {
            bool sceneChanged = !string.Equals(LastObservedSceneName, sceneName, StringComparison.Ordinal);
            LastObservedSceneName = sceneName;
            return sceneChanged;
        }

        public void ScheduleGrant(float currentTime, float delaySeconds)
        {
            GrantReadyAtTime = currentTime + delaySeconds;
        }

        public bool IsGrantReady(float currentTime)
        {
            return currentTime >= GrantReadyAtTime;
        }

        public void MarkGranted(int seed)
        {
            HasGrantedThisRun = true;
            CurrentSeed = seed;
        }

        public void ResetForBreach()
        {
            HasGrantedThisRun = false;
            CurrentSeed = 0;
            GrantReadyAtTime = 0f;
            LastObservedSceneName = _breachSceneName;
        }
    }
}
