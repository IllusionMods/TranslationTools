using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace IllusionMods
{
    [PublicAPI]
    public partial class TextResourceHelper
    {
        public static class Helpers
        {
            private static char[] _directorySeparatorsToReplace;

            private const int NormalizedPathLimit = 200;
            private static readonly HashSet<string> _alreadyNormalizedPaths = new HashSet<string>();
            private static readonly Dictionary<string, string> _recentlyNormalizedPaths = new Dictionary<string, string>();

            private static readonly Encoding Latin1Encoding = Encoding.GetEncoding("ISO-8859-1",
                new EncoderExceptionFallback(), new DecoderExceptionFallback());

            private static IEnumerable<char> DirectorySeparatorsToReplace
            {
                get
                {
                    if (_directorySeparatorsToReplace != null) return _directorySeparatorsToReplace;

                    var dirSeparators = new HashSet<char>();
                    if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
                    {
                        dirSeparators.Add(Path.AltDirectorySeparatorChar);
                    }

                    switch (Path.DirectorySeparatorChar)
                    {
                        case '\\':
                            dirSeparators.Add('/');
                            break;
                        case '/':
                            dirSeparators.Add('\\');
                            break;
                    }

                    return _directorySeparatorsToReplace = dirSeparators.ToArray();
                }
            }

            public static bool ContainsOnlyAscii(string input)
            {
                return !ContainsNonAscii(input);
            }

            public static bool ContainsNonAscii(string input)
            {
                return input.ToCharArray().Any(c => c > sbyte.MaxValue);
            }

            public static bool ContainsOnlyLatin1(string input)
            {
                try
                {
                    var check = Latin1Encoding.GetString(Latin1Encoding.GetBytes(input));
                    return input == check;
                }
                catch (EncoderFallbackException)
                {
                    return false;
                }
            }

            public static bool ContainsNonLatin1(string input)
            {
                return !ContainsOnlyLatin1(input);
            }

            private static bool TryGetCachedNormalizedPath(string path, out string normalizedPath)
            {
                normalizedPath = path;
                return _alreadyNormalizedPaths.Contains(path) ||
                       _recentlyNormalizedPaths.TryGetValue(path, out normalizedPath);
            }


            private static void CacheNormalizePathResult(string path, string normalizedPath)
            {

                if (_alreadyNormalizedPaths.Count >= NormalizedPathLimit) _alreadyNormalizedPaths.Clear();
                _alreadyNormalizedPaths.Add(normalizedPath);

                if (path == normalizedPath) return;
                if (_recentlyNormalizedPaths.Count >= NormalizedPathLimit) _recentlyNormalizedPaths.Clear();
                _recentlyNormalizedPaths[path] = normalizedPath;
            }

            private static string PathSelectHelper(string input, int i)
            {
                // ensure drive letters remain absolute paths
                return (i == 0 && input != null && input.Length >= 2 && input.EndsWith(":"))
                    ? string.Concat(input, Path.DirectorySeparatorChar.ToString())
                    : input;
            }

            public static string NormalizePathSeparators(string path)
            {
                if (TryGetCachedNormalizedPath(path, out var result)) return result;
                var parts = path?.Split((char[]) DirectorySeparatorsToReplace);
                result = parts == null || parts.Length <= 1
                    ? path
                    : parts.Select(PathSelectHelper).Aggregate(Path.Combine);
                CacheNormalizePathResult(path, result);
                return result;
            }


            public static string CombinePaths(params string[] parts)
            {
                var splitChars = (char[]) DirectorySeparatorsToReplace;
                return parts?.SelectMany(i => i.Split(splitChars)).Select(PathSelectHelper).Aggregate(Path.Combine);
            }

            public static string[] SplitPath(string path) => path?.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);

            /// <summary>Wrapper for <see cref="string.Join(string, string[])" /> to workaround lack of params usage in .NET 3.5.</summary>
            public static string JoinStrings(string separator, params string[] value)
            {
                return string.Join(separator, value);
            }

            public static bool ArrayContains<T>(IEnumerable<T> haystack, IEnumerable<T> needle) where T : IComparable
            {
                if (haystack is null) return false;
                var haystackList = haystack.ToList();
                var haystackLength = haystackList.Count;
                var needleList = needle.ToList();
                var needleLength = needleList.Count;

                var start = 0;
                // while first character exists in remaining haystack
                while ((start = haystackList.IndexOf(needleList[0], start)) != -1)
                {
                    if (start + needleLength > haystackLength)
                    {
                        // can't fit in remaining bytes
                        break;
                    }

                    var found = true;
                    for (var i = 1; i < needleLength; i++)
                    {
                        if (needleList[i].CompareTo(haystackList[start + i]) == 0) continue;

                        // mismatch
                        found = false;
                        break;
                    }

                    if (found) return true;
                }
                return false;
            }

            public static bool StringIsSingleReplacement(string str)
            {
                if (str.IsNullOrEmpty()) return false;
                return str.StartsWith("[") && str.EndsWith("]") && str.Count(c => c == '[') == 1;
            }
        }
    }
}
