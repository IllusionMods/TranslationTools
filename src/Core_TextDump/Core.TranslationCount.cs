using System;
using System.Collections.Generic;
using System.Linq;

namespace IllusionMods
{
    public class TranslationCount : IComparable<TranslationCount>, IEquatable<TranslationCount>
    {
        public TranslationCount(TranslationCount translationCount) : this()
        {
            Lines = translationCount.Lines;
            TranslatedLines = translationCount.TranslatedLines;
        }

        public TranslationCount(IDictionary<string, string> translations) : this()
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

        public static bool operator >(TranslationCount a, TranslationCount b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <(TranslationCount a, TranslationCount b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >=(TranslationCount a, TranslationCount b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <=(TranslationCount a, TranslationCount b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator ==(TranslationCount a, TranslationCount b)
        {
            return a is null ? b is null : a.Equals(b);
        }

        public static bool operator !=(TranslationCount a, TranslationCount b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return ((Lines + 1) * 397) ^ (TranslatedLines + 1);
        }


        public override string ToString()
        {
            return $"{Lines} ({TranslatedLines})";
        }

        public int CompareTo(TranslationCount other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var linesComparison = Lines.CompareTo(other.Lines);
            return linesComparison != 0 ? linesComparison : TranslatedLines.CompareTo(other.TranslatedLines);
        }

        public bool Equals(TranslationCount other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Lines == other.Lines && TranslatedLines == other.TranslatedLines;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is TranslationCount other && Equals(other);
        }
    }
}
