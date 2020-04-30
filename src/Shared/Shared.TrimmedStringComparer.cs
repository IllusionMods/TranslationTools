using System.Collections.Generic;
using System.Linq;

// Keep code .NET 3.5 friendly and free of Unity/BepInEx
namespace IllusionMods.Shared
{
    internal class TrimmedStringComparer : IEqualityComparer<string>
    {
        private readonly char[] _extraTrimChars;

        public TrimmedStringComparer(params char[] extraTrimChars)
        {
            _extraTrimChars = extraTrimChars;
        }

        public TrimmedStringComparer() : this(null) { }

        public TrimmedStringComparer(IEnumerable<char> extraTrimChars) : this(extraTrimChars.ToArray()) { }


        private string TrimString(string input)
        {
            if (input == null) return input;
            var result = input.Trim();
            if (_extraTrimChars != null) result = result.Trim(_extraTrimChars).Trim();
            return result;
        }

        public bool Equals(string x, string y)
        {
            return TrimString(x) == TrimString(y);
        }

        public int GetHashCode(string obj)
        {
            return TrimString(obj).GetHashCode();
        }
    }
}
