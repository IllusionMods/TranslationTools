using System.Collections.Generic;

namespace IllusionMods
{
    internal static class TextResourceExtensions
    {
        public static T PopFront<T>(this IList<T> self)
        {
            if (self.IsNullOrEmpty<T>())
            {
                return default(T);
            }
            T item = self[0];
            self.RemoveAt(0);
            return item;
        }
    }
}
