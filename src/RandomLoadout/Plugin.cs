using BepInEx;

namespace RandomLoadout
{
    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInPlugin(GUID, NAME, VERSION)]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        public const string GUID = "randomgun.randomloadout";
        public const string NAME = "RandomLoadout";
        public const string VERSION = BuildVersionInfo.Version;
    }
}
