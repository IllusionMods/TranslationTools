using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ADV;

namespace IllusionMods
{
    public class AI_HS2_TextResourceHelper : TextResourceHelper
    {
        protected AI_HS2_TextResourceHelper()
        {
            FormatKeys = new HashSet<string>(new[] {"パターン", "セリフ"});
            SupportedCommands.Add(Command.Calc);
            SupportedCommands.Add(Command.Format);
            SupportedCommands.Add(Command.Choice);
            SupportedCommands.Add((Command) 242);
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
            var result = -1;
            var tmp = Manager.GameSystem.IsInstance() ?
                Singleton<Manager.GameSystem>.Instance.cultureNames.ToList() :
                new List<string> { "ja-JP", "en-US", "zh-CN", "zh-TW" };
            result = tmp.IndexOf(xUnityLanguage);
            
            if (result != -1) return result;
            tmp = tmp.Select(ShortCulture).ToList();
            result = tmp.IndexOf(xUnityLanguage);
            
            return result != -1 ? result : base.XUnityLanguageToGameLanguage(xUnityLanguage);
        }
    }
}
