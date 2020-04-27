using System;
using System.Collections.Generic;

namespace IllusionMods
{
    public delegate IDictionary<string, string> TranslationCollector();

    public delegate IEnumerable<TranslationDumper> TranslationGenerator();

    public class TranslationDumper
    {
        public TranslationDumper(string path, TranslationCollector collector)
        {
            Path = path;
            Collector = collector;
        }

        [Obsolete("Use Path")] public string Key => Path;

        [Obsolete("Use Collector")] public TranslationCollector Value => Collector;

        public string Path { get; }
        public TranslationCollector Collector { get; }

        public override string ToString()
        {
            return $"TranslationDumper<{Path}, {Collector}>";
        }
    }
}
