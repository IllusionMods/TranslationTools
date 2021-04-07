using System.Collections.Generic;
using HarmonyLib;
using HS2;
using JetBrains.Annotations;

namespace IllusionMods
{
    [UsedImplicitly]
    public class HS2_TextResourceHelper : AI_HS2_TextResourceHelper
    {
        private static bool? _isHS2DX;


        private readonly HashSet<string> _blackListedStringArrayParamAssetTranslations =
            new HashSet<string> {"0", "None", "无", "無"};

        protected HS2_TextResourceHelper() { }

        public override bool IsValidStringArrayParamAssetTranslation(string orig, string translated)
        {
            var tmp = translated?.Trim();
            if (!string.IsNullOrEmpty(tmp) && tmp != orig && translated != orig &&
                !_blackListedStringArrayParamAssetTranslations.Contains(tmp))
            {
                return true;
            }

            return base.IsValidStringArrayParamAssetTranslation(orig, translated);
        }

        public bool IsHS2DX()
        {
            if (!_isHS2DX.HasValue)
            {
                _isHS2DX = AccessTools.Field(typeof(LeaveTheRoomUI), "strGotoSPRoom") != null;
            }

            return _isHS2DX.Value;
        }

        protected override TextAssetTableHelper GetTableHelper()
        {
            var tableHelper = base.GetTableHelper();
            tableHelper.HTextColumns.AddRange(new[] {0});
            return tableHelper;
        }

        internal override Dictionary<string, IEnumerable<string>> GetSettingsStrings()
        {
            var settingsStrings = base.GetSettingsStrings();

            settingsStrings[nameof(IsHS2DX)] = new[] {IsHS2DX().ToString()};
            settingsStrings[nameof(_blackListedStringArrayParamAssetTranslations)] =
                _blackListedStringArrayParamAssetTranslations;
            return settingsStrings;
        }
    }
}
