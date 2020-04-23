using System.Collections.Generic;
using System.Linq;

namespace IllusionMods
{
    public class TranslationCount
    {
        public TranslationCount(Dictionary<string, string> translations) : this()
        {
            Lines = translations.Count;
            TranslatedLines = translations.Count(tl => !tl.Value.IsNullOrEmpty() && tl.Value != tl.Key);
        }

        public TranslationCount()
        {
            Lines = 0;
            TranslatedLines = 0;
        }

        public int Lines { get; private set; }
        public int TranslatedLines { get; private set; }

        private TranslationCount Add(TranslationCount other)
        {
            return new TranslationCount
            {
                Lines = Lines + other.Lines,
                TranslatedLines = TranslatedLines + other.TranslatedLines
            };
        }

        private TranslationCount Negate()
        {
            return new TranslationCount
            {
                Lines = Lines * -1,
                TranslatedLines = TranslatedLines * -1
            };
        }

        public static TranslationCount operator +(TranslationCount a)
        {
            return a;
        }

        public static TranslationCount operator -(TranslationCount a)
        {
            return a.Negate();
        }

        public static TranslationCount operator +(TranslationCount a, TranslationCount b)
        {
            return a.Add(b);
        }

        public static TranslationCount operator -(TranslationCount a, TranslationCount b)
        {
            return a.Add(b.Negate());
        }

        public override string ToString()
        {
            return $"{Lines} ({TranslatedLines})";
        }
    }
}
