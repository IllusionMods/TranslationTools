using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IllusionMods.Shared;

namespace IllusionMods.TranslationTools.Tools.MergeIntoDump
{
    internal class Program
    {
        internal static DirectoryInfo CleanDumpRootInfo;
        internal static DirectoryInfo TranslationRootInfo;
        //private static DirectoryInfo OutputRootInfo;

        private static void Main(string[] args)
        {
            var origConsoleColor = Console.ForegroundColor;
            try
            {
                try
                {
                    ParseArgs(args);
                   
                }
                catch (Exception err)
                {
                    Usage();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(err.Message);
                }
                Merge();
            }
            finally
            {
                Console.ForegroundColor = origConsoleColor;
            }
        }

        private static void Merge()
        {
            foreach (var job in EnumeratePaths())
            {
                if (!job.DestFileInfo.Exists)
                {
                    Console.WriteLine($"{job.DestFilePath}: Adding new file");
                    var destParent = Path.GetDirectoryName(job.DestFilePath);
                    if (!Directory.Exists(destParent)) Directory.CreateDirectory(destParent);
                    job.SourceFileInfo.CopyTo(job.DestFilePath);
                }
                else
                {
                    var destLines = ReadLines(job.DestFileInfo, out var destTranslations);
                    var sourceLines = ReadLines(job.SourceFileInfo, out var srcTranslations);

                    var newLines = sourceLines.Where(((Predicate<string>)destLines.Contains).Not).ToArray();

                    if (newLines.Length == 0) continue;

                    Console.WriteLine($"{job.DestFilePath}: Adding {newLines.Length} missing lines");
                    using (var writer = job.DestFileInfo.AppendText())
                    {
                        // make sure we start on a new line
                        writer.WriteLine(string.Empty);


                        foreach (var line in newLines)
                        {
                            writer.WriteLine(srcTranslations[line]);
                        }
                    }
                }
            }
        }

        private static HashSet<string> ReadLines(FileInfo fileInfo, out Dictionary<string, string> translations)
        {
            translations = new Dictionary<string, string>(new TrimmedStringComparer('/'));
            var results = new HashSet<string>(new TrimmedStringComparer('/'));
            var lines = File.ReadAllLines(fileInfo.FullName)
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.Contains('='));
            foreach (var line in lines)
            {
                var parts = line.Split(new[] {'='}, 2);
                results.Add(parts[0]);
                translations[parts[0].TrimStart('/')] = line;
            }
            return results;
        }

        private static void Usage()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var name = Path.GetFileName(codeBase);
            Console.WriteLine($"{name} [DumpRoot] [TranslationRoot] [Output]");



        }

        private static IEnumerable<MergeJob> EnumeratePaths()
        {
            var dumpFiles = CleanDumpRootInfo.GetFiles("*.txt", SearchOption.AllDirectories);//.OrderBy(x => x.FullName);
            
            foreach (var dumpFile in dumpFiles)
            {
                var job = new MergeJob(dumpFile);
                //Console.WriteLine($"{job.SourceFilePath}\n  -  {job.DestFilePath}");
                yield return job;
            }
        }



        private static void ParseArgs(string[] args)
        {
            if (args.Length != 2) throw new ArgumentException("Incorrect parameters");
            CleanDumpRootInfo = new DirectoryInfo(args[0]);
            TranslationRootInfo = new DirectoryInfo(args[1]);
            //OutputRootInfo = new DirectoryInfo(args[2]);
            if (!CleanDumpRootInfo.Exists) throw new ArgumentException($"DumpRoot does not exist: {args[0]}");
            if (!TranslationRootInfo.Exists) throw new ArgumentException($"TranslationRoot does not exist: {args[1]}");
            /*
            if (!OutputRootInfo.Exists)
            {
                OutputRootInfo.Create();
            }
            */
        }
    }
}
