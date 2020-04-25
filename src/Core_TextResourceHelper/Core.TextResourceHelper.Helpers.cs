using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IllusionMods
{
    public partial class TextResourceHelper
    {
        public static class Helpers
        {
            private static char[] _directorySeparatorsToReplace;

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

            public static bool ContainsNonAscii(string input)
            {
                return input.ToCharArray().Any(c => c > sbyte.MaxValue);
            }

            public static string NormalizePathSeparators(string path)
            {
                return DirectorySeparatorsToReplace.Aggregate(path,
                    (current, sep) => current.Replace(sep, Path.DirectorySeparatorChar));
            }


            public static string CombinePaths(params string[] parts)
            {
                var merged = parts.Aggregate(string.Empty,
                    (current, part) => current.IsNullOrEmpty() ? part : Path.Combine(current, part));
                return NormalizePathSeparators(merged);
            }

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
                        if (needleList[i].CompareTo(haystackList[start + i]) != 0)
                        {
                            // mismatch
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        // loop completed without mismatch
                        return true;
                    }
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
