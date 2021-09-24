using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class MapThumbnailInfoHandler : ParamAssetLoadedHandler<MapThumbnailInfo, MapThumbnailInfo.Param>
    {
        public MapThumbnailInfoHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<MapThumbnailInfo.Param> GetParams(MapThumbnailInfo asset)
        {
            return DefaultGetParams(asset);
        }

        private static void ApplyTranslation(string calculatedModificationPath, MapThumbnailInfo.Param param, string value)
        {
            param.Name = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            MapThumbnailInfo.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.Name, ApplyTranslation);
        }



        public override bool DumpParam(SimpleTextTranslationCache cache, MapThumbnailInfo.Param param)
        {
            return DefaultDumpParam(cache, param, param.Name);
        }
    }
}
