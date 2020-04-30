using System.Collections.Generic;

namespace IllusionMods
{
    public partial class BaseDumpHelper
    {



        protected Dictionary<string, string> SpeakerLocalizations
        {
            get
            {
                if (Plugin.TextResourceHelper is KK_TextResourceHelper helper) return helper.SpeakerLocalizations;
                return null;
            }
        }
    }
}
