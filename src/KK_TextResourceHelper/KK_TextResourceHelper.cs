using System;
using System.Collections.Generic;
using System.Linq;
using ADV;
using IllusionMods.Shared;
using JetBrains.Annotations;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    [UsedImplicitly]
    public class KK_TextResourceHelper : TextResourceHelper
    {
        private static readonly Dictionary<string, IEnumerable<string>> ExcelListPathColumnNameMapping =
            new Dictionary<string, IEnumerable<string>>
            {
                {
                    CombinePaths(string.Empty, "list", "characustom", string.Empty),
                    new[] {"Name"}
                },
                {
                    CombinePaths(string.Empty, "studio", "info", string.Empty),
                    new[] {"表示名", "名称"}
                }
            };

        public readonly Dictionary<string, string> SpeakerLocalizations = new Dictionary<string, string>();

        protected KK_TextResourceHelper()
        {
            SupportedCommands.Add(Command.Choice);
            SupportedCommands.Add((Command) 242);
        }

        public override bool IsReplacement(ScenarioData.Param param)
        {
            // only Party has ADV.Command.ReplaceLanguage
            return (int) param.Command == 223;
        }

        public override IEnumerable<string> GetExcelRowTranslationKeys(string assetName, List<string> row, int i)
        {
            var isOptionDisplay = IsOptionDisplayItemAsset(assetName);
            foreach (var key in base.GetExcelRowTranslationKeys(assetName, row, i))
            {
                // specialized match much come first
                if (isOptionDisplay) yield return $"OPTION[{row[0]}]:{key}";
                yield return key;
            }
        }

        public override string GetSpecializedKey(object obj, string defaultValue)
        {
            if (obj is NickName.Param nickParam)
            {
                return nickParam.isSpecial ? $"SPECIAL:{nickParam.Name}" : nickParam.Name;
            }

            return base.GetSpecializedKey(obj, defaultValue);
        }

        public override IEnumerable<int> GetSupportedExcelColumns(string calculatedModificationPath, ExcelData asset, out int firstRow)
        {
            foreach (var mapping in ExcelListPathColumnNameMapping)
            {
                if (!calculatedModificationPath.Contains(mapping.Key)) continue;

                var columns = new List<int>();
                foreach (var headers in GetExcelHeaderRows(asset, out firstRow))
                {
                    columns.AddRange(mapping.Value.Select(h => headers.IndexOf(h)).Where(i => i != -1));
                }
                return columns.Distinct().Ordered();
            }

            var result = base.GetSupportedExcelColumns(calculatedModificationPath, asset, out firstRow).ToList();

            if (result.Count == 0 && string.Equals(asset.name, "cus_pose", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(3);
            }

            return result;
        }

        protected override ResourceMappingHelper GetResourceMappingHelper()
        {
            return new KK_ResourceMappingHelper();
        }

        public override IEnumerable<string> GetRandomNameDirs()
        {
            yield return "list/random_name";
            foreach (var dir in base.GetRandomNameDirs())
            {
                yield return dir;
            }
        }

        public override bool IsRandomNameListAsset(string assetName)
        {
            return assetName.StartsWith("random_name", StringComparison.OrdinalIgnoreCase);
        }

        protected override TextAssetTableHelper GetTableHelper()
        {
            var tableHelper = base.GetTableHelper();
            tableHelper.HTextColumns.AddRange(new[] {4, 27, 50, 73});
            return tableHelper;
        }

        protected override bool IsRawKeyDisabled(object obj)
        {
            if (obj is NickName.Param) return true;
            return base.IsRawKeyDisabled(obj);
        }
    }
}
