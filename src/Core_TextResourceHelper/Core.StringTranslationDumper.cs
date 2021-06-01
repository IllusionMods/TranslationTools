using System.Collections.Generic;
using JetBrains.Annotations;

namespace IllusionMods
{
    [PublicAPI]
    public class StringTranslationDumper : TranslationDumper<IDictionary<string, string>>
    {
        public StringTranslationDumper(string path, TranslationCollector collector) : base(path, collector) { }
    }
}
