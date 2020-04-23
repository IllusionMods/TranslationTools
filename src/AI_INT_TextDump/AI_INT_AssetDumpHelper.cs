using System.Collections.Generic;

namespace IllusionMods
{
    public class AI_INT_AssetDumpHelper : AI_AssetDumpHelper
    {
        public AI_INT_AssetDumpHelper(TextDump plugin) : base(plugin) { }

        protected override IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            foreach (var list in base.GetLists())
            {
                yield return list;
            }

            if (TextDump.IsReadyForFinalDump())
            {
                yield return new KeyValuePair<string, AssetDumpColumnInfo>("title", TitleAssetCols);
            }
        }
    }
}
