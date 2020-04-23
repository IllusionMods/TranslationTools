using System.Collections.Generic;
using System.Linq;
using ADV;

namespace IllusionMods
{
    public class AI_TextResourceHelper : TextResourceHelper
    {
        public AI_TextResourceHelper()
        {
            CalcKeys = new HashSet<string>(new[] {"want"});
            FormatKeys = new HashSet<string>(new[] {"パターン", "セリフ"});
            TextKeysBlacklist = new HashSet<string>(CalcKeys.Concat(FormatKeys).ToArray());

            SupportedCommands.Add(Command.Calc);
            SupportedCommands.Add(Command.Format);
            SupportedCommands.Add(Command.Choice);
            SupportedCommands.Add(Command.Switch);
            SupportedCommands.Add((Command) 242);
        }

        public override bool IsReplacement(ScenarioData.Param param)
        {
            return param.Command == Command.ReplaceLanguage;
        }

        public override IEnumerable<string> GetScenarioDirs()
        {
            foreach (var dir in base.GetScenarioDirs())
            {
                yield return dir;
            }

            yield return "adv/message";
        }
    }
}
