using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    // ReSharper disable once PartialTypeWithSinglePart
    public partial class ResourceMappingPath : IEquatable<ResourceMappingPath>
    {
        private static string _baseModificationPath;
        private string _calculatedModificationPath;

        private string _resourcePath;
        private List<string> _resourcePathParts;

        private ResourceMappingPath(string resourcePath = null, string calculatedModificationPath = null,
            IEnumerable<string> resourcePathParts = null, bool allPathsNormalized = false)
        {
            var pathParts = resourcePathParts?.ToList();
            _resourcePathParts = pathParts.IsNullOrEmpty() ? null : pathParts;
            _resourcePath = resourcePath.IsNullOrEmpty() ? null :
                allPathsNormalized ? resourcePath : NormalizePathSeparators(resourcePath);
            _calculatedModificationPath = calculatedModificationPath.IsNullOrEmpty() ? null :
                allPathsNormalized ? calculatedModificationPath :
                NormalizePathSeparators(calculatedModificationPath);

            if (_resourcePath == null && _resourcePathParts == null && _calculatedModificationPath == null)
            {
                throw new ArgumentException("At least one parameter must have a value");
            }

            if (_baseModificationPath == null && _calculatedModificationPath != null)
            {
                // force BaseModificationPath to be calculated
                var _ = BaseModificationPath;
            }

            if (_calculatedModificationPath != null && _calculatedModificationPath[1] == ':' &&
                _calculatedModificationPath[2] != '\\')
            {
                TextResourceHelper.Logger.LogWarning($"{this}.{nameof(_calculatedModificationPath)} is corrupted {_calculatedModificationPath}");
            }
        }

        [PublicAPI]
        public string ResourcePath
        {
            get
            {
                if (_resourcePath != null) return _resourcePath;
                var result = CombinePaths(ResourcePathParts.ToArray());
                if (!result.IsNullOrEmpty()) return _resourcePath = result;
                throw new NullReferenceException($"{nameof(ResourcePath)} is unknown and can not be calculated");
            }
        }

        [PublicAPI]
        private string BaseModificationPath
        {
            get
            {
                try
                {
                    if (_baseModificationPath != null) return _baseModificationPath;

                    if (_calculatedModificationPath != null)
                    {
                        var parts = SplitPath(_calculatedModificationPath).ToList();
                        var end = _resourcePath != null ? parts.Count - ResourcePathParts.Count : -1;
                        end = end > 0 ? end : parts.LastIndexOf("abdata");
                        var partsArray = parts.Take(end).ToArray();
                        if (!partsArray.IsNullOrEmpty())
                        {
                            var result = CombinePaths(partsArray);
                            if (!result.IsNullOrEmpty()) return (_baseModificationPath = result);
                        }
                    }

                    throw new NullReferenceException(
                        $"{nameof(BaseModificationPath)} is unknown and can not be calculated");
                }
                finally
                {
                    if (_baseModificationPath != null && _baseModificationPath[1] == ':' &&
                        _baseModificationPath[2] != '\\')
                    {
                        TextResourceHelper.Logger.LogFatal($"{this}.{nameof(_baseModificationPath)} is corrupted {_baseModificationPath}");
                    }

                }
            }
        }

        public bool IsInAbdata()
        {
            return ResourcePath.StartsWith("abdata") ||
                   ResourcePathParts[0].Equals("abdata", StringComparison.OrdinalIgnoreCase);
        }

        [PublicAPI]
        public string CalculatedModificationPath
        {
            get
            {
                if (_calculatedModificationPath != null) return _calculatedModificationPath;
                var result = CombinePaths(BaseModificationPath, ResourcePath);
                if (!result.IsNullOrEmpty()) return _calculatedModificationPath = result;
                throw new NullReferenceException(
                    $"{nameof(CalculatedModificationPath)} is unknown and can not be calculated");
            }
        }

        [PublicAPI]
        public ReadOnlyCollection<string> ResourcePathParts
        {
            get
            {
                if (_resourcePathParts != null) return _resourcePathParts.AsReadOnly();
                if (_resourcePath != null)
                {
                    var result = SplitPath(_resourcePath).ToList();
                    if (!result.IsNullOrEmpty()) return (_resourcePathParts = result).AsReadOnly();
                }
                else if (_calculatedModificationPath != null)
                {
                    var baseParts = SplitPath(BaseModificationPath).ToList();
                    var parts = SplitPath(_calculatedModificationPath).ToList();
                    var result = parts.Skip(baseParts.Count).ToList();
                    if (!result.IsNullOrEmpty()) return (_resourcePathParts = result).AsReadOnly();
                }

                throw new NullReferenceException($"{nameof(ResourcePathParts)} is unknown and can not be calculated");
            }
        }

        public bool Equals(ResourceMappingPath other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ResourcePath == other.ResourcePath;
        }

        public static ResourceMappingPath FromPaths(string resourcePath,
            string calculatedModificationPath,
            bool allPathsNormalized = false)
        {
            if (resourcePath.IsNullOrEmpty())
            {
                throw new ArgumentException("must not be null or empty", nameof(resourcePath));
            }

            if (calculatedModificationPath.IsNullOrEmpty())
            {
                throw new ArgumentException("must not be null or empty", nameof(calculatedModificationPath));
            }

            return new ResourceMappingPath(resourcePath, calculatedModificationPath, null, allPathsNormalized);
        }

        public static ResourceMappingPath FromResourcePath(string path, bool normalized = false)
        {
            if (path.IsNullOrEmpty()) throw new ArgumentException("must not be null or empty", nameof(path));
            return new ResourceMappingPath(path, null, null, normalized);
        }

        public static ResourceMappingPath FromCalculatedModificationPath(string path, bool normalized = false)
        {
            if (path.IsNullOrEmpty()) throw new ArgumentException("must not be null or empty", nameof(path));
            return new ResourceMappingPath(null, path, null, normalized);
        }

        public static ResourceMappingPath FromParts(IEnumerable<string> parts)
        {
            return new ResourceMappingPath(null, null, parts);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ResourceMappingPath) obj);
        }

        public override int GetHashCode()
        {
            return ResourcePath != null ? ResourcePath.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return $"{GetType()}({ResourcePath})";
        }
    }
}
