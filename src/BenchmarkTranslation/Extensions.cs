using System;
using System.Collections.Generic;

namespace IllusionMods
{
    internal static class Extensions
    {
        public static double Percent<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var total = 0;
            var count = 0;

            foreach (var item in source)
            {
                if (predicate(item)) total += 1;
                count++;
            }

            return 100.0 * total / (1.0 * count);
        }
    }
}
