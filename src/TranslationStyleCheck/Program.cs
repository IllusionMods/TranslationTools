using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TranslationStyleCheck
{
    internal class Program
    {
        internal static DirectoryInfo TranslationRoot;

        private const string JapaneseCharRegexString =
            @"([\u3000-\u303F]|[\u3040-\u309F]|[\u30A0-\u30FF]|[\uFF00-\uFFEF]|[\u4E00-\u9FAF]|[\u2605-\u2606]|[\u2190-\u2195]|\u203B)";


        internal static IEnumerable<LineCheck> LineChecks = new[]
        {
            new LineCheck(@".*=.*", "No Equals", Severity.Skip, "Not a translation line", CheckFailMode.Mismatch),

            // do-not-translate
            new LineCheck(@"^//+(セリフ|want)=", "Do Not Translate", Severity.Skip, string.Empty),

            new LineCheck(@"^//+[^=]+=.*" + JapaneseCharRegexString, "Not Translated", Severity.Skip, string.Empty), 
         
            new LineCheck("=.*=", "Single Equals", Severity.Fatal, "line contains multiple '='"),
            new LineCheck("^//..*=..*", "Commented Out", Severity.PotentialIssue,
                "line commented out but contains translation"),
            new LineCheck(@"^[^/]..*=\s+$", "Translation Missing", Severity.Fatal,
                "line uncommented but has to translation")
        };

        internal static IEnumerable<TranslationCheck> TranslationChecks = new[]
        {
            new TranslationCheck(@"^CHOICE:\S+$", "Single Word Choice", Severity.Skip, string.Empty,
                CheckFailMode.Match, BaseCheck.CheckDefaultRegexOptions, true),

            new TranslationCheck(@"^[A-Z][a-z]{1,9}$", "Single Short Word", Severity.Skip, string.Empty),
            
            new TranslationCheck(@"…", "Ellipses Conversion", Severity.Style, "'…' should be converted to '...'"), 
            //elipsis should be multiple of 3
            new TranslationCheck(@"(?<!\.)(?=\.{2})(\.{3})*\.{1,2}(?!\.)", "Ellipses Length", Severity.Style,
                "Ellipses length should be multiple of 3"),

            //Should be a space after elipsis
            new TranslationCheck(@"[\S](?<!\.)(\.{3})+(?!\.)\){1}?[\S]", "No Space After Ellipses", Severity.Style,
                "There should be a space after ellipses (except start/end of line)"),

            //Should NOT have a space before elipsis
            new TranslationCheck(@"[\s](?<!\.)(\.{3})+(?!\.)([\S]|$)", "Space Before Ellipses", Severity.Style,
                "There should not be a space before mid-string ellipses"),

            // Should have space after punctuation
            new TranslationCheck(@"(?<![\.?!:])(?!\.{3})[\.?!:]+(?![\.?!:])\){1}?\S", "No Space After Punctuation",
                Severity.Style,
                "There should be a space after punctuation (except start/end of line)"),

            new TranslationCheck(@"[^\.!?-]\){1}?\s*$", "No Ending Punctuation", Severity.Style,
                "There should be punctuation at the end of the line for dialog lines"),

            new TranslationCheck(@"\S\s\s+\S", "Multiple Spaces", Severity.Suggestion,
                "Compress multiple spaces down to single space")
            
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

                var results = DoChecks();
                ShowResults(results);
            }
            finally
            {
                Console.ForegroundColor = OrigConsoleColor;
            }
        }

        private static void ShowResults(
            IDictionary<string, IDictionary<int, IEnumerable<BaseCheck.CheckResult>>> results)
        {
            foreach (var fileEntry in results.OrderBy(e => e.Key))
            {
                WriteLine(GetRelativePath(TranslationRoot.FullName + @"\", fileEntry.Key), ConsoleColor.White);
                foreach (var lineEntry in fileEntry.Value.OrderBy(e => e.Key))
                {
                    var line = lineEntry.Value.FirstOrDefault()?.Line;
                    Write($" {lineEntry.Key,6}", ConsoleColor.Cyan);
                    WriteLine($": {line}", ConsoleColor.DarkGray);
                    foreach (var error in lineEntry.Value)
                    {
                        Write($"{"",8}{error.Severity}: ");
                        WriteLine($"{error.Message}", SeverityColor(error.Severity));
                    }
                }
            }
        }

        private static ConsoleColor SeverityColor(Severity severity)
        {
            switch (severity)
            {
                case Severity.PotentialIssue:
                    return ConsoleColor.DarkMagenta;
                case Severity.Suggestion:
                    return ConsoleColor.Yellow;
                case Severity.Style:
                    return ConsoleColor.DarkYellow;
                case Severity.Fatal:
                    return ConsoleColor.Red;
                default:
                    return ConsoleColor.DarkRed;
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
        }

        private static void Usage()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var name = Path.GetFileName(codeBase);
            Console.WriteLine($"{name} [TranslationRoot]");
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

        private static IDictionary<string, IDictionary<int, IEnumerable<BaseCheck.CheckResult>>> DoChecks()
        {
            var results = new Dictionary<string, IDictionary<int, IEnumerable<BaseCheck.CheckResult>>>();
            foreach (var fileInfo in GetTranslationFiles(TranslationRoot))
            {
                var fileResults = new Dictionary<int, IEnumerable<BaseCheck.CheckResult>>();
                var lineNum = 0;

                foreach (var line in File.ReadAllLines(fileInfo.FullName))
                {
                    var skipLine = false;
                    var lineResults = new List<BaseCheck.CheckResult>();
                    lineNum++;

                    var lineFailed = false;
                    foreach (var lineCheck in LineChecks)
                    {
                        if (lineCheck.CheckLine(line, out var result)) continue;
                        lineFailed = true;
                        if (result.Severity == Severity.Skip)
                        {
                            skipLine = true;
                            break;
                        }

                        lineResults.Add(result);
                    }

                    if (!lineFailed)
                    {
                        foreach (var transCheck in TranslationChecks)
                        {
                            if (transCheck.CheckLine(line, out var result)) continue;
                            if (result.Severity == Severity.Skip)
                            {
                                skipLine = true;
                                break;
                            }

                            lineResults.Add(result);
                        }
                    }

                    if (skipLine || lineResults.Count == 0) continue;
                    fileResults[lineNum] = lineResults;
                }

                if (fileResults.Count == 0) continue;
                results[fileInfo.FullName] = fileResults;
            }

            return results;
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
    }
}
