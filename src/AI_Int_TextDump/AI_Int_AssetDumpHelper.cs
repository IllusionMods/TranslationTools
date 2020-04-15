using System.Collections.Generic;

namespace IllusionMods
{
    public  class AI_Int_AssetDumpHelper : AI_AssetDumpHelper
    {
        public AI_Int_AssetDumpHelper(TextDump plugin) : base(plugin) { }

        protected override IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            foreach (var list in base.GetLists())
            {
                yield return list;
            }

            if (TextDump.IsReadyToDump())
            {
                yield return new KeyValuePair<string, AssetDumpColumnInfo>("title", titleAssetCols);
            }
        }
    }
}
