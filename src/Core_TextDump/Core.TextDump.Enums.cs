using System;

namespace IllusionMods
{
    public partial class TextDump
    {
        [Flags]
        public enum AssetDumpMode
        {
            Always = 0,
            CustomLevels,
            FirstOnly,
            LastOnly,
            FirstAndLastOnly = FirstOnly | LastOnly
        }

        public enum ExecutionMode
        {
            BeforeFirstLoad = 0,
            Startup = 1,
            Other = 2
        }
    }
}
