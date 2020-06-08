using System.Collections.Generic;
using System.Linq;
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
    }
}
