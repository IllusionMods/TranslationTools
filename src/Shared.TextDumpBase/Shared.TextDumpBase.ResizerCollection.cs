using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IllusionMods.Shared.TextDumpBase
{
    public class ResizerCollection : TranslationDictionary<string, List<string>>
    {
        internal void Merge(IEnumerable<KeyValuePair<string, List<string>>> source)
        {
            var resizersToMerge = source.ToTranslationDictionary();
            foreach (var scope in resizersToMerge.Scopes)
            {
                var scopeSrc = resizersToMerge.GetScope(scope);
                var scopeDest = GetScope(scope);
                foreach (var resizers in scopeSrc)
                {
                    scopeDest[resizers.Key] = resizers.Value.ToList();
                }
            }
        }
    }
}
