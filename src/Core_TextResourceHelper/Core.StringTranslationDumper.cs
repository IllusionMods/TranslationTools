using System.Collections.Generic;

namespace IllusionMods
{
    public class StringTranslationDumper : TranslationDumper<IDictionary<string, string>>
    {
        public StringTranslationDumper(string path, TranslationCollector collector) : base(path, collector) { }
    }
}
