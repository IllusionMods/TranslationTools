using BepInEx.Logging;

namespace IllusionMods
{
    internal static class TextResourceExtensions
    {
        public static bool EnableTraces = false;
        public static void DebugLogDebug(this ManualLogSource logger, object data)
        {
            if (
#if DEBUG
                true ||
#endif
                EnableTraces) logger.LogDebug(data);
        }
    }
}
