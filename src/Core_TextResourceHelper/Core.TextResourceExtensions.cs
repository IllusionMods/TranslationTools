using BepInEx.Logging;

namespace IllusionMods
{
    internal static class TextResourceExtensions
    {
        public static void DebugLogDebug(this ManualLogSource logger, object data)
        {
#if DEBUG
            logger.LogDebug(data);
#endif
        }
    }
}
