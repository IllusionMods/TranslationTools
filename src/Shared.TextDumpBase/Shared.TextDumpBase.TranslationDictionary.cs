using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IllusionMods.Shared;
using UnityEngine;

namespace IllusionMods.Shared.TextDumpBase
{
    public class TranslationDictionary<TKey, TValue> : IScopedTranslations<IDictionary<TKey, TValue>>,
        IDictionary<TKey, TValue>
    {
        private readonly Dictionary<int, OrderedDictionary<TKey, TValue>> _scopedDictionaries;
        private readonly IEqualityComparer<TKey> _comparer;

        public TranslationDictionary() : this(0, null) { }
        public TranslationDictionary(int capacity) : this(capacity, null) {}
        public TranslationDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }

        public TranslationDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _scopedDictionaries = new Dictionary<int, OrderedDictionary<TKey, TValue>>()
            {
                {-1, CreateInternalScopedDictionary(capacity)}
            };

            GetScope(-1);
        }
        public TranslationDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null) {}

        public TranslationDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) :
            this(dictionary is IScopedTranslations<IDictionary<TKey, TValue>> scopedDict
                    ? scopedDict.GetScope(-1).Count
                    : dictionary?.Count ?? 0,
                comparer)
        {
            switch (dictionary)
            {
                case null:
                    throw new ArgumentNullException(nameof(dictionary));
                case IScopedTranslations<IDictionary<TKey, TValue>> scopedSrc:
                {
                    foreach (var scope in scopedSrc.Scopes)
                    {
                        _scopedDictionaries[scope] = CreateInternalScopedDictionary(scopedSrc.GetScope(scope));
                    }

                    break;
                }
                default:
                {

                    var defaultScope = GetScope(-1);
                    foreach (var entry in dictionary)
                    {
                        defaultScope.Add(entry.Key, entry.Value);
                    }

                    break;
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var seen = new HashSet<TKey>();
            foreach (var scope in Scopes)
            {
                foreach (var entry in GetScope(scope))
                {
                    if (seen.Contains(entry.Key)) continue;
                    seen.Add(entry.Key);
                    yield return entry;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _scopedDictionaries[-1].Add(item);
        }

        public void Clear()
        {
            foreach (var scope in Scopes)
            {
                var dict = _scopedDictionaries[scope];
                if (scope != -1) _scopedDictionaries.Remove(scope);
                dict.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _scopedDictionaries[-1].Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _scopedDictionaries[-1].CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return _scopedDictionaries[-1].Remove(item);
        }

        public int Count => _scopedDictionaries.Sum(d => d.Value.Count);

        public bool IsReadOnly => false;

        public bool ContainsKey(TKey key)
        {
            return _scopedDictionaries[-1].ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            _scopedDictionaries[-1].Add(key, value);
        }

        public bool Remove(TKey key)
        {
            return _scopedDictionaries[-1].Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _scopedDictionaries[-1].TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => _scopedDictionaries[-1][key];
            set => _scopedDictionaries[-1][key] = value;
        }

        public ICollection<TKey> Keys => _scopedDictionaries[-1].Keys;

        public ICollection<TValue> Values => _scopedDictionaries[-1].Values;

        public IEnumerable<int> Scopes
        {
            get
            {
                var scopes = _scopedDictionaries.Keys.ToList();
                scopes.Sort();
                return scopes;
            }
        }

        private OrderedDictionary<TKey, TValue> CreateInternalScopedDictionary(IDictionary<TKey, TValue> source)
        {
            return CreateInternalScopedDictionary(source, source.Count);
        }
        private OrderedDictionary<TKey, TValue> CreateInternalScopedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source, int size=0)
        {
            var result = CreateInternalScopedDictionary(size);
            foreach (var entry in source)
            {
                result.Add(entry.Key, entry.Value);
            }
            return result;
        }
        private OrderedDictionary<TKey, TValue> CreateInternalScopedDictionary(int size=0)
        {
            return new OrderedDictionary<TKey, TValue>(size, _comparer);
        }
        //private List<int> _scopes = null;
        public IDictionary<TKey, TValue> GetScope(int scope)
        {
            if (!_scopedDictionaries.TryGetValue(scope, out var result))
            {
                _scopedDictionaries[scope] = result = CreateInternalScopedDictionary();
            }

            return result;
        }
    }

    public class TranslationDictionary : TranslationDictionary<string, string>
    {

        public TranslationDictionary() : this(0, null) { }
        public TranslationDictionary(int capacity) : this(capacity, null) { }
        protected TranslationDictionary(IEqualityComparer<string> comparer) : this(0, comparer) { }

        public TranslationDictionary(IDictionary<string, string> dictionary) : this(dictionary, null) { }

        protected TranslationDictionary(IDictionary<string, string> dictionary, IEqualityComparer<string> comparer) :
            this(dictionary?.Count ?? 0, comparer) { }

        protected TranslationDictionary(int capacity, IEqualityComparer<string> comparer) :
            base(capacity, comparer ?? new TrimmedStringComparer()) { }

        public void Merge(IEnumerable<KeyValuePair<string, string>> translationsToMerge,
            TextResourceHelper textResourceHelper)
        {
            var sourceTranslations = translationsToMerge.ToTranslationDictionary();
            foreach (var scope in sourceTranslations.Scopes)
            {
                var scopeSrc = sourceTranslations.GetScope(scope);
                var scopeDest = GetScope(scope);
                foreach (var localization in scopeSrc)
                {
                    textResourceHelper.AddLocalizationToResults(scopeDest, localization);
                }
            }
        }
    }

    public static class TranslationDictionaryExtensions
    {
        internal static T Wrap<T, TKey, TValue>(IDictionary<TKey, TValue> defaultScopeDictionary)
            where T : TranslationDictionary<TKey, TValue>, new()
        {
            var result = new T();
            var defaultScope = result.GetScope(-1);
            foreach (var entry in defaultScopeDictionary)
            {
                if (defaultScope.ContainsKey(entry.Key)) continue;
                defaultScope.Add(entry.Key, entry.Value);
            }

            return result;
        }

        public static TranslationDictionary ToTranslationDictionary(this IEnumerable<KeyValuePair<string, string>> obj)
        {
            return obj.ToTranslationDictionary<TranslationDictionary, string, string>();
        }

        public static ResizerCollection ToTranslationDictionary(this IEnumerable<KeyValuePair<string, List<string>>> obj)
        {
            return obj.ToTranslationDictionary<ResizerCollection, string, List<string>>();
        }


        public static T ToTranslationDictionary<T, TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> obj)
            where T : TranslationDictionary<TKey, TValue>, new()
        {
            switch (obj)
            {
                case T translationDictionary:
                    return translationDictionary;
                case IDictionary<TKey, TValue> dict:
                    return Wrap<T, TKey, TValue>(dict);
            }

            var result = new T();

            foreach (var entry in obj)
            {
                result.Add(entry);
            }

            return result;
        }
    }
}
