using System;
using System.IO;
using BepInEx;
using Manager;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class ModTextDump
    {
        public const string PluginNameInternal = "KKS_ModTextDump";

        public ModTextDump()
        {
            SetTextResourceHelper(CreateHelper<KKS_TextResourceHelper>());
        }

        protected override Version GetGameVersion()
        {
            return GameSystem.GameVersion;
        }
    }
}
