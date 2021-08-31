using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using static EnvSEData;

namespace IllusionMods
{
    public class EnvSEDataHandler : UntestedParamAssetLoadedHandler<EnvSEData, Param>
    {
        public EnvSEDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<Param> GetParams(EnvSEData asset)
        {
            return new[] {asset.param};
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            Param param)
        {
            var clipDataResult = UpdateClipDatas(calculatedModificationPath, cache, param.clipDatas);
            var playListDataResult = UpdatePlayListDatas(calculatedModificationPath, cache, param.playListDatas);
            return clipDataResult || playListDataResult;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, Param param)
        {
            var clipDataResult = DumpClipDatas(cache, param.clipDatas);
            var playListDataResult = DumpPlayListDatas(cache, param.playListDatas);
            return clipDataResult || playListDataResult;
        }

        private bool UpdatePlayListDatas(string calculatedModificationPath, SimpleTextTranslationCache cache,
            List<PlayListData> paramPlayListDatas)
        {
            var result = false;
            foreach (var playListData in paramPlayListDatas)
            {
                foreach (var detail in playListData.details)
                {
                    if (UpdateDetail(calculatedModificationPath, cache, detail)) result = true;
                }
            }

            return result;
        }

        private bool UpdateDetail(string calculatedModificationPath, SimpleTextTranslationCache cache,
            PlayListDataDetail detail)
        {
            var key = detail.name;
            var result = false;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                if (!EnableSafeMode.Value)
                {
                    WarnIfUnsafe(calculatedModificationPath);
                    detail.name = translated;
                }

                TrackReplacement(calculatedModificationPath, key, translated);
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                result = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                cache.AddTranslationToCache(key, string.Empty);
            }

            return result;
        }

        private bool UpdateClipDatas(string calculatedModificationPath, SimpleTextTranslationCache cache,
            List<ClipData> paramClipDatas)
        {
            var result = false;
            foreach (var clipData in paramClipDatas)
            {
                if (UpdateClipData(calculatedModificationPath, cache, clipData)) result = true;
            }

            return result;
        }

        private bool UpdateClipData(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ClipData clipData)
        {
            var key = clipData.name;
            var result = false;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                if (!EnableSafeMode.Value)
                {
                    WarnIfUnsafe(calculatedModificationPath);
                    clipData.name = translated;
                }

                TrackReplacement(calculatedModificationPath, key, translated);
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                result = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                cache.AddTranslationToCache(key, string.Empty);
            }

            return result;
        }

        private bool DumpPlayListDatas(SimpleTextTranslationCache cache, List<PlayListData> paramPlayListDatas)
        {
            var result = false;
            foreach (var playListData in paramPlayListDatas)
            {
                foreach (var detail in playListData.details)
                {
                    if (DefaultDumpParam(cache, detail.name)) result = true;
                }
            }

            return result;
        }

        private bool DumpClipDatas(SimpleTextTranslationCache cache, List<ClipData> paramClipDatas)
        {
            var result = false;
            foreach (var clipData in paramClipDatas)
            {
                if (DefaultDumpParam(cache, clipData.name)) result = true;
            }

            return result;
        }
    }
}
