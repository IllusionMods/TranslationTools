using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace IllusionMods.Shared
{
    [PublicAPI]
    public static class Extensions
    {
        public static IEnumerable<KeyValuePair<int, T>> Enumerate<T>(this IEnumerable<T> self, int start = 0)
        {
            var i = start;
            foreach (var entry in self) yield return new KeyValuePair<int, T>(i++, entry);
        }

        public static T PopFront<T>(this IList<T> self)
        {
            if (!(self?.Count > 0)) return default;
            var item = self[0];
            self.RemoveAt(0);
            return item;

        }

        public static bool Not<T>(this Predicate<T> self, T value) => !self(value);

        public static Predicate<T> NegatePredicate<T>(this Predicate<T> self)
        {
            bool NegatedPredicate(T value) => !self(value);
            return NegatedPredicate;

        }
    }
}
