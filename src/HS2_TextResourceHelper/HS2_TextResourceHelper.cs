using System.Collections.Generic;
using System.Linq;
using ADV;

namespace IllusionMods
{
    public class HS2_TextResourceHelper : AI_HS2_TextResourceHelper
    {
        protected HS2_TextResourceHelper() { }

        protected override TextAssetTableHelper GetTableHelper()
        {
            var tableHelper = base.GetTableHelper();
            tableHelper.HTextColumns.AddRange(new[] { 0 });
            return tableHelper;
        }
    }
}
