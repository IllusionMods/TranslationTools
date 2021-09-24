using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public static bool Not<T>(this Predicate<T> self, T value)
        {
            return !self(value);
        }

        public static Predicate<T> NegatePredicate<T>(this Predicate<T> self)
        {
            bool NegatedPredicate(T value)
            {
                return !self(value);
            }

            return NegatedPredicate;
        }

        public static bool SafeProc<T>(this ReadOnlyCollection<T> args, int index, Action<T> act)
        {
            if (args.Count <= index + 1 || args[index] == null) return false;
            act?.Invoke(args[index]);
            return true;
        }

        public static TValue GetOrInit<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key,
            Func<TValue> initializer)
        {
            if (dict.TryGetValue(key, out var value)) return value;
            return dict[key] = initializer();
        }

        public static TValue GetOrInit<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key,
            TValue defaultValue)
        {
            TValue Initializer() => defaultValue;
            return GetOrInit(dict, key, Initializer);
        }

        public static TValue GetOrInit<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            TValue Initializer() => new TValue();
            return GetOrInit(dict, key, Initializer);
        }

        public static T Identity<T>(T obj)
        {
            return obj;
        }

        public static IOrderedEnumerable<T> Ordered<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.OrderBy(Identity);
        }

        public static IOrderedEnumerable<T> OrderedDescending<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.OrderByDescending(Identity);
        }

        public static string ToSingleLineString(this string input)
        {
            return input.Replace(Environment.NewLine, " ").Replace("   ", " ").Replace("  ", " ");
        }

        public static bool Contains(this string input, string value, System.StringComparison comparison)
        {
            return input.IndexOf(value, comparison) >= 0;
        }


    }
}
