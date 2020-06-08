using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


// Keep code .NET 3.5 friendly
namespace IllusionMods.Shared
{
    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly List<KeyValuePair<TKey, TValue>> _list;

        public OrderedDictionary() : this(0, null) { }
        public OrderedDictionary(int capacity) : this(capacity, null) { }
        public OrderedDictionary(IEqualityComparer<TKey> comparer) : this(0, comparer) { }
 

        public OrderedDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity, comparer ?? EqualityComparer<TKey>.Default);
            _list = new List<KeyValuePair<TKey, TValue>>();
        }

        public OrderedDictionary(IDictionary<TKey, TValue> dictionary) : this(dictionary, null) { }

        public OrderedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : 
            this(dictionary?.Count ?? 0, comparer)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            foreach (var entry in dictionary)
            {
                Add(entry.Key, entry.Value);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
            _list.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var result = _list.Remove(item);
            _dictionary.Remove(item.Key);
            return result;
        }

        public int Count => _dictionary.Count;

        public bool IsReadOnly => false;

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            _list.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public bool Remove(TKey key)
        {
            return _dictionary.TryGetValue(key, out var value) && Remove(new KeyValuePair<TKey, TValue>(key, value));
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                if (_dictionary.ContainsKey(key))
                {
                    _dictionary[key] = value;
                    _list[_list.IndexOf(_list.Find(e => e.Key.Equals(key)))] =
                        new KeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public ICollection<TKey> Keys => _list.Select(e => e.Key).ToArray();

        public ICollection<TValue> Values => _list.Select(e => e.Value).ToArray();
    }
}

