using ADV;
using System.Collections.Generic;
using System.Linq;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public class KK_TextResourceHelper : TextResourceHelper
    {
        public readonly Dictionary<string, string> SpeakerLocalizations = new Dictionary<string, string>();
        public KK_TextResourceHelper()
        {
            CalcKeys = new HashSet<string>();
            FormatKeys = new HashSet<string>();
            TextKeysBlacklist = new HashSet<string>();

            SupportedCommands.Add(Command.Choice);
            SupportedCommands.Add((Command)242);
        }

        public override bool IsReplacement(ScenarioData.Param param)
        {
            // only Party has ADV.Command.ReplaceLanguage
            return (int) param.Command == 223;
        }

        private static readonly Dictionary<string, IEnumerable<string>> ExcelListPathColumnMapping =
            new Dictionary<string, IEnumerable<string>>
            {
                {
                    CombinePaths(string.Empty, "list", "characustom", string.Empty),
                    new [] {"Name"}
                },
                {
                    CombinePaths(string.Empty, "studio", "info", string.Empty),
                    new [] { "表示名", "名称" }
                }
            };

        public override IEnumerable<int> GetSupportedExcelColumns(string calculatedModificationPath, ExcelData asset)
        {
            foreach (var mapping in ExcelListPathColumnMapping)
            {
                if (!calculatedModificationPath.Contains(mapping.Key)) continue;

                var headers = GetExcelHeaderRow(asset);
                return mapping.Value.Select(h => headers.IndexOf(h)).Where(i => i != -1).Distinct().OrderBy(x=>x);
            }

            return base.GetSupportedExcelColumns(calculatedModificationPath, asset);
        }
    }
}
