using System;
using System.Collections.Generic;
using System.Linq;
using ADV;
using Manager;

namespace IllusionMods
{
    public class AI_HS2_TextResourceHelper : TextResourceHelper
    {
        protected AI_HS2_TextResourceHelper()
        {
            SupportedCommands.Add(Command.Calc);
            SupportedCommands.Add(Command.Choice);
            SupportedCommands.Add((Command) 242);

            // TextDump sometimes picks up this column header, so workaround here.
            TextKeysBlacklist.Add("表示名");
        }

        public override void InitializeHelper()
        {
            base.InitializeHelper();
            // blacklist all CalcKeys and FormatKeys
            foreach (var key in CalcKeys)
            {
                TextKeysBlacklist.Add(key);
            }

            foreach (var key in FormatKeys)
            {
                TextKeysBlacklist.Add(key);
            }
        }

        public override bool IsReplacement(ScenarioData.Param param)
        {
            return param.Command == Command.ReplaceLanguage;
        }

        public override int XUnityLanguageToGameLanguage(string xUnityLanguage)
        {
            string ShortCulture(string culture)
            {
                var cultureParts = culture.Split('-');
                return cultureParts.Length > 0 ? cultureParts[0] : culture;
            }

            var tmp = GameSystem.IsInstance()
                ? Singleton<GameSystem>.Instance.cultureNames.ToList()
                : new List<string> {"ja-JP", "en-US", "zh-CN", "zh-TW"};
            var result = tmp.IndexOf(xUnityLanguage);

            if (result != -1) return result;
            tmp = tmp.Select(ShortCulture).ToList();
            result = tmp.IndexOf(xUnityLanguage);

            return result != -1 ? result : base.XUnityLanguageToGameLanguage(xUnityLanguage);
        }

        public override IEnumerable<string> GetRandomNameDirs()
        {
            yield return "list/characustom";
            foreach (var dir in base.GetRandomNameDirs())
            {
                yield return dir;
            }
        }

        public override bool IsRandomNameListAsset(string assetName)
        {
            return assetName.StartsWith("randnamelist", StringComparison.OrdinalIgnoreCase);
        }
    }
}
