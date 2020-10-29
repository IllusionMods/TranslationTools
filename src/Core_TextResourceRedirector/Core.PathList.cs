using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace IllusionMods
{
    public class PathList : ICollection<string>
    {
        private readonly HashSet<string> _paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static ManualLogSource Logger => TextResourceRedirector.Logger;
        public int Count => _paths.Count;

        public bool IsReadOnly => ((ICollection<string>) _paths).IsReadOnly;

        public void Add(string item)
        {
            _paths.Add(Normalize(item));
        }

        public void Clear()
        {
            _paths.Clear();
        }

        public bool Contains(string item)
        {
            return _paths.Contains(Normalize(item));
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _paths.CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _paths.GetEnumerator();
        }

        public bool Remove(string item)
        {
            return _paths.Remove(Normalize(item));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _paths.GetEnumerator();
        }

        public static string Normalize(string item)
        {
            return TextResourceHelper.Helpers.NormalizePathSeparators(item);
        }


        /// <summary>
        ///     Determines whether path starts with one of the PathList entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isPathNormalized">if set to <c>true</c> assumes path is normalized</param>
        /// <returns>
        ///     <c>true</c> if the path starts with one of the PathList entries; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPathListed(string path, bool isPathNormalized = false)
        {
            if (_paths.Count == 0) return false;
            var search = isPathNormalized ? path : Normalize(path);
            //Logger.LogError($"searching for {search} in [{string.Join(", ", _paths.ToArray())}]");
            return Array.Find(_paths.ToArray(), p => search.StartsWith(p, StringComparison.OrdinalIgnoreCase)) != null;
        }
    }
}
