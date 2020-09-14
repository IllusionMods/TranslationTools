using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class RandomNameProvider
    {
        public const string PluginNameInternal = "KK_RandomNameProvider";

        public static bool IsRandomNameAsset(string assetBunndlePath, string assetName)
        {
            return assetName == "random_name";
        }
    }
}
