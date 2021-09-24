using System.Collections.Generic;
using IllusionMods.Shared;
using JetBrains.Annotations;

namespace IllusionMods
{
    public class ResourceMappingCache<T> 
    {
        private readonly Dictionary<ResourceMappingMode, ResourceMappingModeCache<T>> _cache =
            new Dictionary<ResourceMappingMode, ResourceMappingModeCache<T>>();

        private readonly int _maxCacheSize;
        private readonly string _name;
        private readonly bool _delayCleaning;

        public ResourceMappingCache(string name, int maxCacheSize = -1, bool delayCleaning = false)
        {
            _name = name;
            _maxCacheSize = maxCacheSize;
            _delayCleaning = delayCleaning;
        }

        public ResourceMappingModeCache<T> this[ResourceMappingMode key] => _cache.GetOrInit(key,
            () => new ResourceMappingModeCache<T>($"{_name} ({key})", _maxCacheSize, _delayCleaning));

        public void Reset()
        {
            _cache.Clear();
        }

        [PublicAPI]
        public void CapacityCheck()
        {
            foreach (var entry in _cache)
            {
                entry.Value.CapacityCheck(true);
            }
        }
    }
}
