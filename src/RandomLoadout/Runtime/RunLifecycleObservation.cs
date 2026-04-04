namespace RandomLoadout
{
    internal enum RunLifecycleResetKind
    {
        None,
        EnteredBreach,
        PrimaryPlayerChanged
    }

    internal sealed class RunLifecycleObservation
    {
        public RunLifecycleObservation(
            string sceneName,
            string previousSceneName,
            bool sceneChanged,
            bool playerChanged,
            bool isGrantableDungeonScene,
            RunLifecycleResetKind resetKind)
        {
            SceneName = sceneName;
            PreviousSceneName = previousSceneName;
            SceneChanged = sceneChanged;
            PlayerChanged = playerChanged;
            IsGrantableDungeonScene = isGrantableDungeonScene;
            ResetKind = resetKind;
        }

        public string SceneName { get; private set; }

        public string PreviousSceneName { get; private set; }

        public bool SceneChanged { get; private set; }

        public bool PlayerChanged { get; private set; }

        public bool IsGrantableDungeonScene { get; private set; }

        public RunLifecycleResetKind ResetKind { get; private set; }

        public bool ShouldScheduleGrant
        {
            get { return IsGrantableDungeonScene && (SceneChanged || PlayerChanged); }
        }
    }
}
