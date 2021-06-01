using System.Collections;
using System.Collections.Generic;

namespace IllusionMods.Shared.TextDumpBase
{
    public interface IScopedTranslations<T> where T : IEnumerable
    {
        IEnumerable<int> Scopes { get; }

        T GetScope(int scope);
    }
}
