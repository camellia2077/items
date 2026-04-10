using BepInEx;

namespace RandomLoadout
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        public const string GUID = "randomgun.randomloadout";
        public const string NAME = "RandomLoadout";
        public const string VERSION = "0.2.0";
    }
}
