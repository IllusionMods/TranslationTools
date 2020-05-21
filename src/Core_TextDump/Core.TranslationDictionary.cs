using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IllusionMods.Shared;

namespace IllusionMods
{
    public class TranslationDictionary : IDictionary<string, string>
    {
        private readonly Dictionary<int, OrderedDictionary<string, string>> _scopedDictionaries =
            new Dictionary<int, OrderedDictionary<string, string>>
            {
                {-1, new OrderedDictionary<string, string>()}
            };

        public IEnumerable<int> Scopes
        {
            get
            {
                var scopes = _scopedDictionaries.Keys.ToList();
                scopes.Sort();
                return scopes;
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return Scopes.SelectMany(scope => _scopedDictionaries[scope]).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
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

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _scopedDictionaries[-1].Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _scopedDictionaries[-1].CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return _scopedDictionaries[-1].Remove(item);
        }

        public int Count => _scopedDictionaries.Sum(d => d.Value.Count);

        public bool IsReadOnly => false;

        public bool ContainsKey(string key)
        {
            return _scopedDictionaries[-1].ContainsKey(key);
        }

        public void Add(string key, string value)
        {
            _scopedDictionaries[-1].Add(key, value);
        }

        public bool Remove(string key)
        {
            return _scopedDictionaries[-1].Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _scopedDictionaries[-1].TryGetValue(key, out value);
        }

        public string this[string key]
        {
            get => _scopedDictionaries[-1][key];
            set => _scopedDictionaries[-1][key] = value;
        }

        public ICollection<string> Keys => _scopedDictionaries[-1].Keys;

        public ICollection<string> Values => _scopedDictionaries[-1].Values;

        internal static TranslationDictionary Wrap(OrderedDictionary<string, string> defaultScopeDictionary)
        {
            var translationDictionary = new TranslationDictionary();
            translationDictionary._scopedDictionaries[-1] = defaultScopeDictionary;
            return translationDictionary;
        }

        //private List<int> _scopes = null;
        public IDictionary<string, string> GetScope(int scope)
        {
            if (!_scopedDictionaries.TryGetValue(scope, out var result))
            {
                _scopedDictionaries[scope] = result = new OrderedDictionary<string, string>();
            }

            return result;
        }

        public void MergeTranslations(IEnumerable<KeyValuePair<string, string>> translationsToMerge,
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
        public static TranslationDictionary ToTranslationDictionary(this IEnumerable<KeyValuePair<string, string>> obj)
        {
            switch (obj)
            {
                case TranslationDictionary translationDictionary:
                    return translationDictionary;
                case OrderedDictionary<string, string> ordered:
                    return TranslationDictionary.Wrap(ordered);
            }

            var result = new TranslationDictionary();

            foreach (var entry in obj)
            {
                result.Add(entry);
            }

            return result;
        }
    }
}
