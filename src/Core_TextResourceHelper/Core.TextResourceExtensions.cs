using System;
using BepInEx.Logging;

namespace IllusionMods
{
    internal static class TextResourceExtensions
    {
        private static bool _enableTraces = false;

        public static bool EnableTraces
        {
            get
            {
#if DEBUG
                return true;
#else
                return _enableTraces;
#endif
            }
            set
            {
                _enableTraces = value;
            }
        }


        /// <summary>
        ///     Logs via <c>LogDebug</c> on DEBUG builds or if <c>EnableTraces</c> is <c>true</c>
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="data">The data.</param>
        public static void DebugLogDebug(this ManualLogSource logger, object data)
        {
            if (!EnableTraces || logger == null) return;
            logger.LogDebug(data);
        }

        /// <summary>
        ///     Logs via <c>LogDebug</c> on DEBUG builds or if <c>EnableTraces</c> is <c>true</c>
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="format">A format string</param>
        /// <param name="args">Arguments to format string</param>
        public static void DebugLogDebug(this ManualLogSource logger, string format, params object[] args)
        {
            if (!EnableTraces || logger == null) return;
            try
            {
                logger.LogDebug(string.Format(format, args));
            }
            catch (Exception err)
            {
                logger.LogWarning($"{nameof(DebugLogDebug)}: error logging message ({format}): {err.Message}");
            }
        }
    }
}
