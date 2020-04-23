#if !HS
using System.Linq;
using XUnity.AutoTranslator.Plugin.Core;

#if AI
using AIChara;
#endif

namespace IllusionMods
{
    public class ChaListDataHandler : TextAssetMessagePackHandlerBase<ChaListData>
    {

        public ChaListDataHandler() : base(ChaListData.ChaListDataMark)
        {
            SearchLength = ObjectMark.Count() * 3;
        }
        public override bool TranslateObject(ref ChaListData obj, SimpleTextTranslationCache cache, string calculatedModificationPath)
        {
            var idx = obj.lstKey.IndexOf("Name");
            if (idx == -1) return false;
            var result = false;
            foreach (var entry in obj.dictList.Values)
            {
                if (entry.Count > idx && cache.TryGetTranslation(entry[idx], true, out var translation))
                {
                    TranslationHelper.RegisterRedirectedResourceTextToPath(translation, calculatedModificationPath);
                    result = true;
                    entry[idx] = translation;
                }
            }
            return result;
        }
    }
}
#endif
