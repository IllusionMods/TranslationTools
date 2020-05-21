using System.Collections;
using System.Collections.Generic;

namespace IllusionMods
{
    public class TranslationDumper<T> : ITranslationDumper<T>
    {
        public delegate T TranslationCollector();

        public TranslationDumper(string path, TranslationCollector collector)
        {
            Path = path;
            Collector = collector;
        }

        public TranslationCollector Collector { get; }
        public string Path { get; }

        TranslationCollector<T> ITranslationDumper<T>.Collector => TypedCollector;

        BaseTranslationCollector ITranslationDumper.Collector => BaseCollector;

        private IEnumerable BaseCollector()
        {
            return (IEnumerable) Collector();
        }

        private IEnumerable<T> TypedCollector()
        {
            return (IEnumerable<T>) Collector();
        }

        public override string ToString()
        {
            return $"{GetType()}<{Path}, {Collector}>";
        }
    }
}
