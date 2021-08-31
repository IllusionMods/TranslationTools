using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using IllusionMods.Shared;
using JetBrains.Annotations;
using UnityEngine;
using BepInExLogLevel = BepInEx.Logging.LogLevel;
using UnityEngineDebug = UnityEngine.Debug;

namespace IllusionMods
{
    [Flags]
    public enum ResourceMappingMode
    {
        [PublicAPI] None = 0,
        [PublicAPI] Sync = 1,
        [PublicAPI] Replacement = 2,
        [PublicAPI] SyncAndReplacement = Sync | Replacement
    }

    [Flags]
    public enum ResourceGameMode
    {
        [PublicAPI] None = 0,
        [PublicAPI] GameOnly = 1,
        [PublicAPI] StudioOnly = 2,
        [PublicAPI] GameAndStudio = GameOnly | StudioOnly,
        [PublicAPI] All = GameAndStudio
    }

    public delegate bool ResourceMappingChecker(ResourceMappingPath path, ResourceMappingMode mode);

    public delegate IEnumerable<ResourceMappingPath> ResourceMappingMapper(ResourceMappingPath path,
        ResourceMappingMode mode);

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ResourceMappingHelper : IPathListBoundHandler
    {
        private const int MaxMapperIdCacheSize = 384;
        private const int MaxMappingPathsCacheSize = 128;

        private static int _nextMapperId;

        protected static readonly bool IsStudio = Application.productName == Constants.StudioProcessName;

        private readonly Dictionary<ResourceMappingChecker, ResourceMappingMode> _checkerModes =
            new Dictionary<ResourceMappingChecker, ResourceMappingMode>();

        private readonly OrderedDictionary<ResourceMappingChecker, List<MapperEntry>> _checkers =
            new OrderedDictionary<ResourceMappingChecker, List<MapperEntry>>();

        private readonly ResourceGameMode _currentModeFlag;

        private readonly Dictionary<int, ResourceMappingMapper> _mappers =
            new Dictionary<int, ResourceMappingMapper>();

        private readonly ResourceMappingCache<List<int>> _pathToMapperIdCache =
            new ResourceMappingCache<List<int>>("PathToMapperIdCache", MaxMapperIdCacheSize, true);

        private readonly ResourceMappingCache<List<ResourceMappingPath>> _pathToMappingPathCache =
            new ResourceMappingCache<List<ResourceMappingPath>>("PathToMappingPathCache", MaxMappingPathsCacheSize, true);

        private readonly Dictionary<ResourceMappingMode, HashSet<ResourceMappingPath>> _unmappedPathCache =
            new Dictionary<ResourceMappingMode, HashSet<ResourceMappingPath>>();

        protected readonly HashSet<int> KnownLevels = new HashSet<int>();

        public bool HasReplacementMappingsForCurrentGameMode { get; private set; } = false;

        public ResourceMappingHelper()
        {
            WhiteListPaths.ValueChanged += PathListChanged;
            BlackListPaths.ValueChanged += PathListChanged;
            _currentModeFlag = IsStudio ? ResourceGameMode.StudioOnly : ResourceGameMode.GameOnly;
        }

        protected ManualLogSource Logger => TextResourceHelper.Logger;

        protected IEnumerable<int> EnumerateLevels(int start=99, int end =0)
        {
            var decrementing = start > end;
            var increment = decrementing ? -1 : 1;
            var knownLevelsPopulated = KnownLevels.Any();

            var maxKnown = knownLevelsPopulated ? KnownLevels.Max() : 0;
            var minKnown = knownLevelsPopulated ? KnownLevels.Min() : 0;


            for (var i = start; decrementing ? i >= end : i <= end; i += increment)
            {
                if (knownLevelsPopulated && (i >= minKnown || i <= maxKnown))
                {
                    if (KnownLevels.Contains(i)) yield return i;
                    continue;
                }
                yield return i;
            }
        }

        protected void AddKnownLevels(params int[] levels)
        {
            foreach (var level in levels) KnownLevels.Add(level);
        }

        public IEnumerable<ResourceMappingPath> GetMappingForPath(ResourceMappingPath path, ResourceMappingMode mode)
        {
            Logger.LogDebug($"{nameof(GetMappingForPath)}: path={path}");

            if (IsPathUnmapped(path, mode))
            {
                Logger.LogFatal($"{nameof(GetMappingForPath)}: {path}: unmapped");
                yield break;
            }

            if (TryGetCachedMappingPaths(path, mode, out var cachedMappedPaths))
            {
                foreach (var mappedPath in cachedMappedPaths) yield return mappedPath;
                Logger.LogFatal($"{nameof(GetMappingForPath)}: {path}: used cached");
                yield break;
            }

            cachedMappedPaths = new List<ResourceMappingPath>();
            var seenPaths = new HashSet<ResourceMappingPath> {path};

            using (var mappersForPath = GetMappersForPath(path, mode).GetEnumerator())
            {
                while (true)
                {
                    try
                    {
                        if (!mappersForPath.MoveNext()) break;
                    }
                    catch (Exception err)
                    {
                        Logger.LogDebug($"{nameof(GetMappingForPath)}: {err.Message}");
                        UnityEngineDebug.LogException(err);
                        continue;
                    }

                    var mapper = mappersForPath.Current;
                    // shouldn't happen
                    if (mapper == null) continue;

                    using (var mappedPaths = mapper(path, mode).GetEnumerator())
                    {
                        while (true)
                        {
                            try
                            {
                                if (!mappedPaths.MoveNext()) break;
                            }
                            catch (Exception err)
                            {
                                Logger.LogDebug($"{nameof(GetMappingForPath)}: {mapper.Method.Name}: {err.Message}");
                                UnityEngineDebug.LogException(err);
                                continue;
                            }

                            var mappedPath = mappedPaths.Current;
                            if (mappedPath == null || seenPaths.Contains(mappedPath)) continue;

                            yield return mappedPath;
                            seenPaths.Add(mappedPath);
                            cachedMappedPaths.Add(mappedPath);
                        }
                    }
                }
            }
            
            if (cachedMappedPaths.Count == 0)
            {
                SetPathUnmapped(path, mode);
            }
            else
            {
                SetCachedMappingPaths(path, mode, cachedMappedPaths);
                Logger.LogDebug(
                    $"{nameof(GetMappingForPath)}: {path} => {string.Join(", ", cachedMappedPaths.Select(p=>p.ResourcePath).ToArray())}");
            }
        }

        public virtual void ResetCaches()
        {
            _unmappedPathCache.Clear();
            _pathToMappingPathCache.Reset();
            _pathToMapperIdCache.Reset();
        }

        public virtual bool IsCurrentGameMode(ResourceGameMode mode)
        {
            return (mode & _currentModeFlag) == _currentModeFlag;
        }

        protected void RegisterMapping(ResourceMappingChecker checker, ResourceMappingMapper mapper,
            ResourceMappingMode mappingMode, ResourceGameMode replacementGameMode = ResourceGameMode.All,
            string pathToWhitelist = null)
        {
            // turn off replacement if not supported by current mode
            if (!IsCurrentGameMode(replacementGameMode)) mappingMode &= ~ResourceMappingMode.Replacement;

            if (mappingMode == ResourceMappingMode.None) return;
            var mapperId = GetMapperId(mapper);
            var mapperEntries = _checkers.GetOrInit(checker);

            var entry = mapperEntries.FirstOrDefault(e => e.MapperId == mapperId);
            if (entry == null)
            {
                mapperEntries.Add(new MapperEntry(mapperId, mappingMode));
            }
            else
            {
                entry.Mode |= mappingMode;
            }

            if (_checkerModes.ContainsKey(checker))
            {
                _checkerModes[checker] |= mappingMode;
            }
            else
            {
                _checkerModes[checker] = mappingMode;
            }

            if (!pathToWhitelist.IsNullOrEmpty()) WhiteListPaths.Add(pathToWhitelist);

            if (!HasReplacementMappingsForCurrentGameMode &&
                (mappingMode & ResourceMappingMode.Replacement) == ResourceMappingMode.Replacement &&
                (replacementGameMode & _currentModeFlag) == _currentModeFlag)
            {
                HasReplacementMappingsForCurrentGameMode = true;
            }

            ResetCaches();
        }

        private void PathListChanged(object sender, EventArgs e)
        {
            ResetCaches();
        }

        private IEnumerable<ResourceMappingMapper> GetMappersForPath(ResourceMappingPath path,
            ResourceMappingMode mode)
        {
            if (_checkers.Count == 0 || IsPathUnmapped(path, mode)) yield break;

            if (this.IsPathBlocked(path.ResourcePath))
            {
                SetPathUnmapped(path, mode);
                yield break;
            }


            if (TryGetCachedMapperIds(path, mode, out var mapperIds))
            {
                foreach (var mapperId in mapperIds)
                {
                    if (_mappers.TryGetValue(mapperId, out var mapper)) yield return mapper;
                }
                yield break;
            }

            mapperIds = new List<int>();
            using (var checkers = GetCheckers(mode).GetEnumerator())
            {
                while (true)
                {
                    try
                    {
                        if (!checkers.MoveNext()) break;
                    }
                    catch (Exception err)
                    {
                        Logger.LogDebug($"{nameof(GetMappersForPath)}: {err.Message}");
                        UnityEngineDebug.LogException(err);
                        continue;
                    }

                    var checker = checkers.Current;
                    if (checker == null) continue;

                    try
                    {
                        if (!checker(path, mode)) continue;
                    }
                    catch (Exception err)
                    {
                        Logger.LogDebug($"{nameof(GetMappersForPath)}: {checker.Method.Name}: {err.Message}");
                        UnityEngineDebug.LogException(err);
                        continue;
                    }

                    foreach (var id in _checkers[checker]
                        .Where(e => (e.Mode & mode) == mode && !mapperIds.Contains(e.MapperId))
                        .Select(e => e.MapperId))
                    {
                        if (!_mappers.TryGetValue(id, out var mapper)) continue;
                        yield return mapper;
                        mapperIds.Add(id);
                    }
                }
            }

            if (mapperIds.Count == 0)
            {
                SetPathUnmapped(path, mode);
            }
            else
            {
                SetCachedMapperIds(path, mode, mapperIds);
            }
        }

        private int GetMapperId(ResourceMappingMapper mapper)
        {
            foreach (var entry in _mappers)
            {
                if (entry.Value == mapper) return entry.Key;
            }

            var result = _nextMapperId++;
            _mappers[result] = mapper;
            return result;
        }

        private IEnumerable<ResourceMappingMapper> GetMappers(ResourceMappingChecker checker,
            ResourceMappingMode mode)
        {
            if (!_checkers.TryGetValue(checker, out var mapperEntries)) yield break;

            foreach (var id in mapperEntries.Where(e => (e.Mode & mode) == mode).Select(e => e.MapperId))
            {
                if (_mappers.TryGetValue(id, out var mapper)) yield return mapper;
            }
        }


        private bool IsPathUnmapped(ResourceMappingPath path, ResourceMappingMode mode)
        {
            return _unmappedPathCache.TryGetValue(mode, out var unmappedPathsForMode) &&
                   unmappedPathsForMode.Contains(path);
        }

        private void SetPathUnmapped(ResourceMappingPath path, ResourceMappingMode mode)
        {
            _unmappedPathCache.GetOrInit(mode).Add(path);
        }

        private bool TryGetCachedMappingPaths(ResourceMappingPath path, ResourceMappingMode mode,
            out List<ResourceMappingPath> paths)
        {
            return _pathToMappingPathCache[mode].TryGetValue(path, out paths);
        }

        private void SetCachedMappingPaths(ResourceMappingPath path, ResourceMappingMode mode,
            IEnumerable<ResourceMappingPath> paths)
        {
            _pathToMappingPathCache[mode][path] = paths.ToList();
        }

        private bool TryGetCachedMapperIds(ResourceMappingPath path, ResourceMappingMode mode,
            out List<int> mapperIds)
        {
            return _pathToMapperIdCache[mode].TryGetValue(path, out mapperIds);
        }

        private void SetCachedMapperIds(ResourceMappingPath path, ResourceMappingMode mode,
            IEnumerable<int> mapperIds)
        {
            _pathToMapperIdCache[mode][path] = mapperIds.ToList();
        }

        private IEnumerable<ResourceMappingChecker> GetCheckers(ResourceMappingMode mode)
        {
            return _checkers.Where(e => (_checkerModes[e.Key] & mode) == mode).Select(e => e.Key);
        }

        internal class MapperEntry
        {
            internal MapperEntry(int mapperId, ResourceMappingMode mode)
            {
                MapperId = mapperId;
                Mode = mode;
            }

            internal int MapperId { get; }
            internal ResourceMappingMode Mode { get; set; }
        }

        #region IPathListBoundHandler

        public PathList WhiteListPaths { get; } = new PathList();
        public PathList BlackListPaths { get; } = new PathList();

        #endregion IPathListBoundHandler
    }
}
