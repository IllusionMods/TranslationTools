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
            // Format commands with a key of `セリフ` can be handled by TextDump/TextResourceRedirector
            // others are used to make programmatic labels and should be untouched
            SupportedCommands.Add(Command.Format);
            FormatKeys.Add("セリフ");

            CalcKeys.Add("want");

            // adding known format key that we don't handle to ensure we don't try and translated it
            TextKeysBlacklist.Add("パターン");
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
