using System;
using Manager;

namespace IllusionMods
{
    public partial class TextDump
    {
        protected override Version GetGameVersion()
        {
            return Game.Version;
        }
    }
}
