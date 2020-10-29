using BepInEx;

namespace IllusionMods
{
    // empty plugin to ensure older version with old GUID doesn't load
    [BepInPlugin(GUID, PluginName, Version)]
    internal class RandomNameProviderCompat : BaseUnityPlugin
    {
        public const string GUID = "random_name_provider";
        public const string PluginName = RandomNameProvider.PluginName + " Compat";
        public const string Version = RandomNameProvider.Version;
    }
}
