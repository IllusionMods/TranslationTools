using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace IllusionMods
{
    public delegate IEnumerable BaseTranslationCollector();

    public delegate IEnumerable<T> TranslationCollector<T>();

    [UsedImplicitly]
    public delegate IEnumerable<ITranslationDumper> TranslationGenerator();

    public interface ITranslationDumper
    {
        string Path { get; }
        BaseTranslationCollector Collector { get; }
    }

    public interface ITranslationDumper<T> : ITranslationDumper
    {
        new TranslationCollector<T> Collector { get; }
    }
}
