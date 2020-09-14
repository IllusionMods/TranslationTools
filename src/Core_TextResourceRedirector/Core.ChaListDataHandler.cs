#if !HS
using System.Linq;
using XUnity.AutoTranslator.Plugin.Core;

#if AI || HS2
using AIChara;
#endif

namespace IllusionMods
{
    public class ChaListDataHandler : TextAssetMessagePackHandlerBase<ChaListData>
    {
        public ChaListDataHandler(TextResourceRedirector plugin) : base(plugin, ChaListData.ChaListDataMark, true)
        {
            SearchLength = ObjectMark.Count() + 10;
        }

        public override bool TranslateObject(ref ChaListData obj, SimpleTextTranslationCache cache,
            string calculatedModificationPath)
        {
            var idx = obj.lstKey.IndexOf("Name");
            if (idx == -1) return false;
            var result = false;
            var shouldTrack = !IsTranslationRegistrationAllowed(calculatedModificationPath);
            foreach (var entry in obj.dictList.Values)
            {
                if (entry.Count <= idx || !cache.TryGetTranslation(entry[idx], true, out var translation)) continue;

                if (shouldTrack) TrackReplacement(calculatedModificationPath, entry[idx], translation);
                TranslationHelper.RegisterRedirectedResourceTextToPath(translation, calculatedModificationPath);
                result = true;
                entry[idx] = translation;
            }

            return result;
        }
    }
}
#endif
