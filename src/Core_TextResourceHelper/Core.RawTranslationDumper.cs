#if RAW_DUMP_SUPPORT
using System;
using System.Collections.Generic;

namespace IllusionMods
{
    public class RawTranslationDumper : TranslationDumper<IEnumerable<byte>>
    {
        public RawTranslationDumper(string path, TranslationCollector collector) : base(path, collector) { }

    }
}
#endif
