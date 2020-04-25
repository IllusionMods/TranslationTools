using System.Collections.Generic;

namespace IllusionMods
{
    internal class TrimmedStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x?.Trim() == y?.Trim();
        }

        public int GetHashCode(string obj)
        {
            return obj.Trim().GetHashCode();
        }
    }
}
