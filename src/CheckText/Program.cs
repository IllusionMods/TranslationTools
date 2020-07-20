using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using IllusionMods.Shared;

namespace CheckText
{
    internal class Program
    {
        internal static DirectoryInfo TranslationRoot;
        internal static DirectoryInfo TextRoot;
        internal static DirectoryInfo ResourceRoot;

        internal static IEnumerable<Regex> ResourcePrefixes = new[]
        {
            new Regex(@"^CHOICE:", RegexOptions.Compiled),
            new Regex(@"^OPTION[\d+]:", RegexOptions.Compiled)
        };

        internal static ConsoleColor OrigConsoleColor;

        private static void Main(string[] args)
        {
            OrigConsoleColor = Console.ForegroundColor;
            try
            {
                try
                {
                    ParseArgs(args);
                }
                catch (Exception err)
                {
                    Usage();
                    WriteLine(err.Message, ConsoleColor.Red);
                    return;
                }

                DoCheck();
            }
            finally
            {
                Console.ForegroundColor = OrigConsoleColor;
            }
        }

        private static void ParseArgs(string[] args)
        {
            if (args.Length != 1) throw new ArgumentException("Incorrect parameters");
            TranslationRoot = new DirectoryInfo(args[0]);
            if (!TranslationRoot.Exists)
            {
                throw new ArgumentException($"{nameof(TranslationRoot)} does not exist: {args[0]}");
            }

            TextRoot = TranslationRoot.GetDirectories().FirstOrDefault(d => d.Name == "Text");
            ResourceRoot = TranslationRoot.GetDirectories().FirstOrDefault(d => d.Name == "RedirectedResources");

            var missing = new List<string>(2);
            if (TextRoot == null) missing.Add("Text");
            if (ResourceRoot == null) missing.Add("RedirectedResources");
            if (missing.Count > 0)
            {
                throw new ArgumentException(
                    $"{TranslationRoot.FullName} missing required subdirectories:\n - {string.Join("\n - ", missing.ToArray())}");
            }
        }

        private static void Usage()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var name = Path.GetFileName(codeBase);
            Console.WriteLine($"{name} [TranslationRoot]");
        }

        private static IDictionary<string, IDictionary<string, IList<string>>> LoadKeys(DirectoryInfo root,
            bool isResource = false)
        {
            var result = new Dictionary<string, IDictionary<string, IList<string>>>(new TrimmedStringComparer());

            var parent = root.Parent?.FullName ?? Path.Combine(root.FullName, "..");
            foreach (var pth in GetTranslationFiles(root))
            {
                foreach (var entry in GetTranslationsFromFile(pth))
                {
                    var key = entry.Key;

                    if (key.StartsWith("r:") || key.StartsWith("sr:")) continue;
                    if (isResource)
                    {
                        foreach (var prefix in ResourcePrefixes)
                        {
                            if (!prefix.IsMatch(key)) continue;
                            key = prefix.Replace(key, string.Empty);
                            break;
                        }
                    }

                    if (!result.TryGetValue(key, out var entries))
                    {
                        result[key] =
                            entries = new Dictionary<string, IList<string>>(new TrimmedStringComparer());
                    }

                    if (!entries.TryGetValue(entry.Value, out var subEntries))
                    {
                        entries[entry.Value] = subEntries = new List<string>();
                    }


                    subEntries.Add(GetRelativePath(parent, pth.FullName));
                }
            }

            return result;
        }

        private static IEnumerable<KeyValuePair<string, string>> GetTranslationsFromFile(FileInfo fileInfo)
        {
            var lines = File.ReadAllLines(fileInfo.FullName)
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.Contains('='));
            foreach (var line in lines)
            {
                var parts = line.Split(new[] {'='}, 2);
                var key = parts[0].TrimStart('/');
                if (string.IsNullOrWhiteSpace(key)) continue;
                yield return new KeyValuePair<string, string>(parts[0].TrimStart('/'), parts[1]);
            }
        }

        private static IEnumerable<FileInfo> GetTranslationFiles(DirectoryInfo root)
        {
            return root.GetFiles("*.txt", SearchOption.AllDirectories)
                .Where(p => !p.Name.EndsWith("_resizer.txt")).OrderBy(x => x.FullName);
        }

        private static void DoCheck()
        {
            var resourceKeys = LoadKeys(ResourceRoot, true);
            var textKeys = LoadKeys(TextRoot);

            CheckDupes(textKeys);
            CheckTextAgainstResources(textKeys, resourceKeys);
        }

        private static void Write<T>(T obj)
        {
            Write(obj, OrigConsoleColor);
        }

        private static void Write<T>(T obj, ConsoleColor color)
        {
            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(obj);
            Console.ForegroundColor = origColor;
        }

        private static void WriteLine<T>(T obj)
        {
            WriteLine(obj, OrigConsoleColor);
        }

        private static void WriteLine<T>(T obj, ConsoleColor color)
        {
            Write(obj, color);
            Console.WriteLine();
        }

        private static void WritePrefix(int i)
        {
            WritePrefix(i, OrigConsoleColor);
        }

        private static void WritePrefix(int i, ConsoleColor color)
        {
            Write(string.Empty.PadLeft(i * 4), color);
            Write(" - ", color);
        }

        private static void CheckDupes(IDictionary<string, IDictionary<string, IList<string>>> textKeys)
        {
            var first = true;
            foreach (var entry in textKeys.OrderBy(e => e.Key))
            {
                var matches = entry.Value.Count;
                if (matches < 2) continue;
                if (first)
                {
                    first = false;
                    WriteLine($"Duplicates entries found under {TextRoot.FullName}");
                }

                WritePrefix(0);
                Write($"'{entry.Key}'", ConsoleColor.DarkYellow);
                Write(" found ");
                Write(entry.Value.Count, ConsoleColor.Magenta);
                WriteLine(" times:");
                foreach (var subEntry in entry.Value.OrderBy(e => e.Key))
                {
                    WritePrefix(1);
                    Write(subEntry.Key, ConsoleColor.Green);
                    WriteLine($" ({subEntry.Value.Count} files)", ConsoleColor.DarkGray);

                    foreach (var match in subEntry.Value.OrderBy(e => e))
                    {
                        WritePrefix(2);
                        WriteLine(match, ConsoleColor.Yellow);
                    }
                }
            }

            Console.ForegroundColor = OrigConsoleColor;
        }

        private static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException(nameof(fromPath));


            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException(nameof(toPath));

            var fromUri = new Uri(Path.GetFullPath(fromPath));
            var toUri = new Uri(Path.GetFullPath(toPath));

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        private static void CheckTextAgainstResources(IDictionary<string, IDictionary<string, IList<string>>> textKeys,
            IDictionary<string, IDictionary<string, IList<string>>> resourceKeys)
        {
            var first = true;
            foreach (var key in textKeys.Keys.OrderBy(k => k))
            {
                if (!resourceKeys.TryGetValue(key, out var resourceEntry)) continue;
                if (first)
                {
                    first = false;
                    WriteLine($"Entries under {TextRoot.FullName} duplicating resources:");
                }

                WritePrefix(0);
                Write($"'{key}'", ConsoleColor.DarkYellow);
                WriteLine($" (found in {resourceEntry.Values.SelectMany(p => p).Distinct().Count()} resource files)",
                    ConsoleColor.DarkGray);
                var textEntry = textKeys[key];

                foreach (var subResourceEntry in resourceEntry.OrderBy(e => e.Key))
                {
                    WritePrefix(1);
                    Write(subResourceEntry.Key, ConsoleColor.Green);
                    WriteLine($" ({subResourceEntry.Value.Count} files)", ConsoleColor.DarkGray);


                    foreach (var match in subResourceEntry.Value.OrderBy(e => e))
                    {
                        WritePrefix(2);
                        WriteLine(match, ConsoleColor.Yellow);
                    }
                }
                
                foreach (var subTextEntry in textEntry.OrderBy(e => e.Key))
                {
                    WritePrefix(1);
                    Write(subTextEntry.Key, ConsoleColor.Green);
                    Write(" in ");
                    Write(subTextEntry.Value.Count, ConsoleColor.Magenta);
                    Write(" text file(s), ");
                    if (resourceEntry.TryGetValue(subTextEntry.Key, out var resourceMatches))
                    {
                        Write("matches ", ConsoleColor.DarkGray);
                        Write(resourceMatches.Count, ConsoleColor.DarkMagenta);
                        Write(" resource files", ConsoleColor.DarkGray);
                        WriteLine(" (possibly redundant)", ConsoleColor.DarkBlue);
                    }
                    else
                    {
                        WriteLine("no matching translation in resource file(s)", ConsoleColor.Red);
                    }

                    foreach (var match in subTextEntry.Value.OrderBy(e => e))
                    {
                        WritePrefix(2);
                        WriteLine(match, ConsoleColor.Yellow);
                    }
                }
            }
        }
    }
}
