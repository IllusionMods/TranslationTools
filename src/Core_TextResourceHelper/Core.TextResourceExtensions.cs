using BepInEx.Logging;

namespace IllusionMods
{
    internal static class TextResourceExtensions
    {
        public static bool EnableTraces = false;


        /// <summary>
        /// Logs via <c>LogDebug</c> on DEBUG builds or if <c>EnableTraces</c> is <c>true</c>
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="data">The data.</param>
        public static void DebugLogDebug(this ManualLogSource logger, object data)
        {
#if !DEBUG
            if (!EnableTraces) return;
#endif
            logger.LogDebug(data);
        }
    }
}
