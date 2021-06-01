using System;
using BepInEx;
using Manager;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ModTextDump
    {
        public const string PluginNameInternal = "KK_ModTextDump";

        public ModTextDump()
        {
            SetTextResourceHelper(CreateHelper<KK_TextResourceHelper>());
        }

        protected override Version GetGameVersion()
        {
            return Game.Version;
        }
    }
}
