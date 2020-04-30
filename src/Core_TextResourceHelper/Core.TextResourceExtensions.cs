using System.Collections.Generic;
using BepInEx.Logging;

namespace IllusionMods
{
    internal static class TextResourceExtensions
    {
        public static T PopFront<T>(this IList<T> self)
        {
            if (self.IsNullOrEmpty())
            {
                return default;
            }

            var item = self[0];
            self.RemoveAt(0);
            return item;
        }


        public static void DebugLogDebug(this ManualLogSource logger, object data)
        {
#if DEBUG
            logger.LogDebug(data);
#endif
        }
    }
}
