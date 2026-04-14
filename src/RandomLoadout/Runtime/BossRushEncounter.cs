namespace RandomLoadout
{
    internal sealed class BossRushEncounter
    {
        public BossRushEncounter(string floorKey, string sceneName)
        {
            FloorKey = floorKey ?? string.Empty;
            SceneName = sceneName ?? string.Empty;
        }

        public string FloorKey { get; private set; }

        public string SceneName { get; private set; }
    }
}
