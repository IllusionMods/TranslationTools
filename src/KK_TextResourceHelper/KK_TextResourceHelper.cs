using ADV;
using System.Collections.Generic;

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
    }
}
