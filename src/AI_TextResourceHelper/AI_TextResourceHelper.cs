using System.Collections.Generic;
using System.Linq;
using ADV;

namespace IllusionMods
{
    public class AI_TextResourceHelper : AI_HS2_TextResourceHelper
    {
        protected AI_TextResourceHelper() 
        {
            SupportedCommands.Add(Command.Switch); // Definately don't want this in HS2
            CalcKeys = new HashSet<string>(new[] {"want"});
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
