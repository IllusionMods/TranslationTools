using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace IllusionMods.Shared
{
    [PublicAPI]
    internal static class Constants
    {
#if AI
        internal const string GameName = "AI Girl";
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string MainGameProcessName = "AI-Syoujyo";
        internal const string MainGameProcessNameSteam = "AI-Shoujo";
        internal const RegexOptions DefaultRegexOptions = RegexOptions.Compiled;
#elif EC
        internal const string GameName = "Emotion Creators";
        internal const string MainGameProcessName = "EmotionCreators";
        internal const RegexOptions DefaultRegexOptions = RegexOptions.None;
#elif HS
        internal const string GameName = "Honey Select";
        internal const string StudioProcessName = "StudioNEO_64";
        internal const string MainGameProcessName = "HoneySelect_64";
        internal const string BattleArenaProcessName = "BattleArena_64";
        internal const RegexOptions DefaultRegexOptions = RegexOptions.None;
#elif KK
        internal const string GameName = "Koikatsu";
        internal const string StudioProcessName = "CharaStudio";
        internal const string MainGameProcessName = "Koikatu";
        internal const string MainGameProcessNameSteam = "Koikatsu Party";
        internal const string VRProcessName = "KoikatuVR";
        internal const string VRProcessNameSteam = "Koikatsu Party VR";
        internal const RegexOptions DefaultRegexOptions = RegexOptions.None;
#elif HS2
        internal const string GameName = "Honey Select 2";
        internal const string StudioProcessName = "StudioNEOV2";
        internal const string MainGameProcessName = "HoneySelect2";
        internal const RegexOptions DefaultRegexOptions = RegexOptions.Compiled;
#elif KKS
        internal const string GameName = "Koikatsu Sunshine";
        internal const string StudioProcessName = "CharaStudioV2";
        internal const string MainGameProcessName = "Koikatsu Sunshine";
        internal const RegexOptions DefaultRegexOptions = RegexOptions.Compiled;
#else
        // generic
        internal const string GameName = "Illusion Games";
        internal const RegexOptions DefaultRegexOptions = RegexOptions.None;
#endif

    }

}
