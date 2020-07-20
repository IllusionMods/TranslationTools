using System.Collections.Generic;
using System.IO;

namespace IllusionMods
{
    public class AI_HS2_AssetDumpHelper : AssetDumpHelper 
    {
        protected AssetDumpColumnInfo ItemLookup;
        protected AssetDumpColumnInfo ItemLookupAndAssetCols;

        protected AI_HS2_AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            ItemLookup = new AssetDumpColumnInfo(null, null, true, new[]
            {
                "アイテム名",
                "名前(メモ)",
            });

            ItemLookupAndAssetCols =
                new AssetDumpColumnInfo(null, StdExcelAssetCols.NameMappings, true, ItemLookup.ItemLookupColumns);

        }
       
    }
}
