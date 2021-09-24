using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using static EnvSEData;

namespace IllusionMods
{
    public class EnvSEDataHandler : ParamAssetLoadedHandler<EnvSEData, Param>
    {
        public EnvSEDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<Param> GetParams(EnvSEData asset)
        {
            return new[] {asset.param};
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            Param param)
        {
            var clipDataResult = UpdateClipDatas(calculatedModificationPath, cache, param);
            var playListDataResult = UpdatePlayListDatas(calculatedModificationPath, cache, param);
            return clipDataResult || playListDataResult;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, Param param)
        {
            var clipDataResult = DumpClipDatas(cache, param);
            var playListDataResult = DumpPlayListDatas(cache, param);
            return clipDataResult || playListDataResult;
        }

        private bool UpdatePlayListDatas(string calculatedModificationPath, SimpleTextTranslationCache cache,
            Param param)
        {
            var result = false;
            foreach (var playListData in param.playListDatas)
            {
                foreach (var detail in playListData.details)
                {
                    if (UpdateDetail(calculatedModificationPath, cache, param, detail)) result = true;
                }
            }

            return result;
        }

        private bool UpdateDetail(string calculatedModificationPath, SimpleTextTranslationCache cache,
            Param param, PlayListDataDetail detail)
        {
            var key = detail.name;
            var result = false;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                detail.name = translated;
                TrackReplacement(calculatedModificationPath, key, translated);
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                result = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                DefaultDumpParam(cache, param, detail, detail.name);
            }

            return result;
        }

        private bool UpdateClipDatas(string calculatedModificationPath, SimpleTextTranslationCache cache,
            Param param)
        {
            var result = false;
            foreach (var clipData in param.clipDatas)
            {
                if (UpdateClipData(calculatedModificationPath, cache, param, clipData)) result = true;
            }

            return result;
        }

        private bool UpdateClipData(string calculatedModificationPath, SimpleTextTranslationCache cache,
            Param param, ClipData clipData)
        {
            var key = clipData.name;
            var result = false;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                clipData.name = translated;
                TrackReplacement(calculatedModificationPath, key, translated);
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                result = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                DefaultDumpParam(cache, param, clipData, clipData.name);
            }

            return result;
        }

        private bool DumpPlayListDatas(SimpleTextTranslationCache cache, Param param)
        {
            var result = false;
            foreach (var playListData in param.playListDatas)
            {
                foreach (var detail in playListData.details)
                {
                    if (DefaultDumpParam(cache, param, detail, detail.name)) result = true;
                }
            }

            return result;
        }

        private bool DumpClipDatas(SimpleTextTranslationCache cache, Param param)
        {
            var result = false;
            foreach (var clipData in param.clipDatas)
            {
                if (DefaultDumpParam(cache, param, clipData, clipData.name)) result = true;
            }

            return result;
        }
    }
}
