using System;
using Manager;

namespace IllusionMods
{
    partial class TextDump
    {
        public static Version GetGameVersion()
        {
            return Game.Version;
        }
    }
}
