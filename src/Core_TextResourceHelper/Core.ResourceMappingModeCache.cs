using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using IllusionMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionMods
{
    public class ResourceMappingModeCache<T> : IDictionary<ResourceMappingPath, T>
    {
        private readonly OrderedDictionary<ResourceMappingPath, T> _cache;
        private readonly int _maxCacheSize;
        private readonly string _name;
        private bool _cleaningPending;

        private int _lastCheckedFrame;
        private float _lastCleared;
        private readonly bool _delayCleaning;

        public ResourceMappingModeCache(string name, int maxCacheSize = -1, bool delayCleaning = false)
        {
            _name = name;
            _maxCacheSize = maxCacheSize;
            _cache = maxCacheSize > 0
                ? new OrderedDictionary<ResourceMappingPath, T>(maxCacheSize)
                : new OrderedDictionary<ResourceMappingPath, T>();
            _lastCheckedFrame = Time.frameCount;
            _lastCleared = Time.realtimeSinceStartup;
#if HS
            _delayCleaning = false;
#else
            _delayCleaning = delayCleaning;
            if (delayCleaning)
            {
                SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
            }
#endif
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            CapacityCheck(true);
        }

        protected ManualLogSource Logger => TextResourceHelper.Logger;

        public IEnumerator<KeyValuePair<ResourceMappingPath, T>> GetEnumerator()
        {
            return _cache.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _cache).GetEnumerator();
        }

        public void Add(KeyValuePair<ResourceMappingPath, T> item)
        {
            CapacityCheck();
            _cache.Add(item);
        }

        public void Clear()
        {
            _cache.Clear();
            _lastCleared = Time.realtimeSinceStartup;
            _lastCheckedFrame = Time.frameCount;
        }

        public bool Contains(KeyValuePair<ResourceMappingPath, T> item)
        {
            return _cache.Contains(item);
        }

        public void CopyTo(KeyValuePair<ResourceMappingPath, T>[] array, int arrayIndex)
        {
            _cache.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<ResourceMappingPath, T> item)
        {
            return _cache.Remove(item);
        }

        public int Count => _cache.Count;

        public bool IsReadOnly => _cache.IsReadOnly;

        public bool ContainsKey(ResourceMappingPath key)
        {
            return _cache.ContainsKey(key);
        }

        public void Add(ResourceMappingPath key, T value)
        {
            CapacityCheck();
            _cache.Add(key, value);
        }

        public bool Remove(ResourceMappingPath key)
        {
            return _cache.Remove(key);
        }

        public bool TryGetValue(ResourceMappingPath key, out T value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public T this[ResourceMappingPath key]
        {
            get => _cache[key];
            set
            {
                CapacityCheck();
                _cache[key] = value;
            }
        }

        public ICollection<ResourceMappingPath> Keys => _cache.Keys;

        public ICollection<T> Values => _cache.Values;

        internal void CapacityCheck(bool isDelayedCheck = false)
        {
            if (_maxCacheSize < 1) return;
            if (_cleaningPending)
            {
                _cleaningPending = false;
                var start = Time.realtimeSinceStartup;
                _cleaningPending = false;
                var goal = (_maxCacheSize * 3) / 4;
                var toRemove = _cache.Take(_cache.Count - goal).ToList();
                var removed = toRemove.Count(entry => _cache.Remove(entry));
                Logger.DebugLogDebug(
                    $"{nameof(CapacityCheck)}: {_name}: discarded {removed} elements, time since last cleaning {Time.realtimeSinceStartup - _lastCleared:000.0000}s, new size: {_cache.Count}/{_maxCacheSize}");
                _lastCleared = Time.realtimeSinceStartup;
                return;
            }

            if (!isDelayedCheck && _delayCleaning) return;

            if (Time.frameCount <= _lastCheckedFrame) return;
            _lastCheckedFrame = Time.frameCount;
            if (Count < _maxCacheSize) return;
            _cleaningPending = true;
        }
    }
}
