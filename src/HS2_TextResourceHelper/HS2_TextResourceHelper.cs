using System.Collections.Generic;
using System.Linq;
using ADV;

namespace IllusionMods
{
    public class HS2_TextResourceHelper : AI_HS2_TextResourceHelper
    {
        protected HS2_TextResourceHelper() { }

        private readonly HashSet<string> _blackListedStringArrayParamAssetTranslations =
            new HashSet<string> {"0", "None", "无", "無"};
        protected override TextAssetTableHelper GetTableHelper()
        {
            var tableHelper = base.GetTableHelper();
            tableHelper.HTextColumns.AddRange(new[] { 0 });
            return tableHelper;
        }

        public override bool IsValidStringArrayParamAssetTranslation(string orig, string translated)
        {
            var tmp = translated?.Trim();
            if (!string.IsNullOrEmpty(tmp) && tmp != orig && translated != orig && !_blackListedStringArrayParamAssetTranslations.Contains(tmp))
            {
                return true;
            }

            return base.IsValidStringArrayParamAssetTranslation(orig, translated);
        }
    }
}
