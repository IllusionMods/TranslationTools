using ADV;
using System.Collections.Generic;

namespace IllusionMods
{
    public class KK_TextResourceHelper : TextResourceHelper
    {
        public KK_TextResourceHelper()
        {
            CalcKeys = new HashSet<string>();
            FormatKeys = new HashSet<string>();
            TextKeysBlacklist = new HashSet<string>();

            SupportedCommands.Add(Command.Choice);
            SupportedCommands.Add((Command)242);
        }

        public override bool IsReplacement(ScenarioData.Param param) => (int)param.Command == 223; // only Party has ADV.Command.ReplaceLanguage
    }
}
