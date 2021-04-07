using System.Collections.Generic;
using ADV;
using JetBrains.Annotations;

namespace IllusionMods
{
    [UsedImplicitly]
    public class AI_TextResourceHelper : AI_HS2_TextResourceHelper
    {
        protected AI_TextResourceHelper()
        {
            SupportedCommands.Add(Command.Format); // Definitely don't want this in HS2
            SupportedCommands.Add(Command.Switch); // Definitely don't want this in HS2
            CalcKeys = new HashSet<string>(new[] {"want"});
            FormatKeys = new HashSet<string>(new[] {"パターン", "セリフ"});
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
