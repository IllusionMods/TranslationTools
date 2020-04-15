using ADV;
using Illusion.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace IllusionMods
{
    public class AI_TextResourceHelper : TextResourceHelper
    {
        public AI_TextResourceHelper()
        {
            CalcKeys = new HashSet<string>(new string[] { "want" });
            FormatKeys = new HashSet<string>(new string[] { "パターン", "セリフ" });
            TextKeysBlacklist = new HashSet<string>(CalcKeys.Concat(FormatKeys).ToArray());

            SupportedCommands[Command.Calc] = true;
            SupportedCommands[Command.Format] = true;
            SupportedCommands[Command.Choice] = true;
            SupportedCommands[Command.Switch] = true;
            SupportedCommands[(ADV.Command)242] = true;
        }

        public override bool IsReplacement(ScenarioData.Param param) => param.Command == Command.ReplaceLanguage;

        public override IEnumerable<string> GetScenarioDirs()
        {
            foreach (string dir in base.GetScenarioDirs())
            {
                yield return dir;
            }
            yield return "adv/message";
        }
    }
}
