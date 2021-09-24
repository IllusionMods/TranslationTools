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
    public partial class KKS_TextResourceHelper : TextResourceHelper
    {
        private static readonly HashSet<Command> SelectionCommands = new HashSet<Command>
        {
            Command.SelectionAdd, Command.SelectionInsert, Command.SelectionReplace
        };

        private static readonly Dictionary<Command, int> SelectionParamIndex = new Dictionary<Command, int>();

        public string SelectionPrefix { get; } = Command.Selection.ToString().ToUpperInvariant();
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
                },
            };

        private static readonly Dictionary<string, IEnumerable<string>> ExcelListAssetNameColumnNameMapping =
            new Dictionary<string, IEnumerable<string>>
            {
                {"cus_selectlist", new[] {"タイトル"}},
                {"cus_e_ptn", new[] {"タイトル"}},
                {"cus_m_ptn", new[] {"タイトル"}},
                {"cus_eb_ptn", new[] {"タイトル"}},
                {"cus_filelist", new[] {"タイトル"}},


            };

        private static readonly Dictionary<string, IEnumerable<int>> ExcelListAssetNameColumnIndexMapping =
            new Dictionary<string, IEnumerable<int>>
            {
                {"cus_pose", new[] {3}},
                {"cus_pose_trial", new[] {3}},
                {"cus_eyeslook", new []{1}},
                {"cus_necklook", new []{1}},
            };

        public readonly Dictionary<string, string> SpeakerLocalizations = new Dictionary<string, string>();

        protected KKS_TextResourceHelper()
        {
            SupportedCommands.Add(Command.Choice);

            foreach (var selectionCommand in SelectionCommands)
            {
                SupportedCommands.Add(selectionCommand);
                SpecializedKeyCommands.Add(selectionCommand);
            }

            // TextDump sometimes picks up this column header, so workaround here.
            TextKeysBlacklist.Add("表示名");
            TextKeysBlacklist.Add("名称");
            TextKeysBlacklist.Add("タイトル");
            TextKeysBlacklist.Add("Name");

        }

        public override bool IsReplacement(ScenarioData.Param param)
        {
            return param.Command == Command.ReplaceLanguage;
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

        protected override string GetSpecializedKeyPrefix(ScenarioData.Param param)
        {
            var result = base.GetSpecializedKeyPrefix(param);
            return result.StartsWith(SelectionPrefix) ? SelectionPrefix : result;
        }

        public override IEnumerable<int> GetSupportedExcelColumns(string calculatedModificationPath, ExcelData asset, out int firstRow)
        {
            foreach (var assetToColumnIndexMapping in ExcelListAssetNameColumnIndexMapping)
            {
                if (!asset.name.Equals(assetToColumnIndexMapping.Key, StringComparison.OrdinalIgnoreCase)) continue;
                GetExcelHeaderRows(asset, out firstRow);
                return assetToColumnIndexMapping.Value;
            }

            foreach (var assetToColumnNameMapping in ExcelListAssetNameColumnNameMapping)
            {
                if (!asset.name.Equals(assetToColumnNameMapping.Key, StringComparison.OrdinalIgnoreCase)) continue;
                var columns = new List<int>();
                foreach (var headers in GetExcelHeaderRows(asset, out firstRow))
                {
                    columns.AddRange(assetToColumnNameMapping.Value.Select(h => headers.IndexOf(h)).Where(i => i != -1));
                }
                return columns.Distinct().Ordered();
            }


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
            
            return result;
        }

        protected override ResourceMappingHelper GetResourceMappingHelper()
        {
            return new KKS_ResourceMappingHelper();
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

        public override IEnumerable<int> GetScenarioCommandTranslationIndexes(Command command)
        {
            if (SelectionCommands.Contains(command))
            {
                if (!SelectionParamIndex.TryGetValue(command, out var result))
                {
                    var cmd = CommandGenerator.Create(command);
                    SelectionParamIndex[command] = result = cmd.ArgsLabel.ToList().IndexOf("Text");
                }

                yield return result;
                yield break;
            }

            foreach (var i in base.GetScenarioCommandTranslationIndexes(command)) yield return i;
        }

        protected override bool IsRawKeyDisabled(object obj)
        {
            if (obj is NickName.Param) return true;
            return base.IsRawKeyDisabled(obj);
        }
    }
}
