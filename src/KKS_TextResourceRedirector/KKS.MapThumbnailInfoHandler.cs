using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class MapThumbnailInfoHandler : UntestedParamAssetLoadedHandler<MapThumbnailInfo, MapThumbnailInfo.Param>
    {
        public MapThumbnailInfoHandler(TextResourceRedirector plugin) : base(plugin, true, false) { }

        public override IEnumerable<MapThumbnailInfo.Param> GetParams(MapThumbnailInfo asset)
        {
            return DefaultGetParams(asset);
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            MapThumbnailInfo.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, "Name");
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, MapThumbnailInfo.Param param)
        {
            return DefaultDumpParam(cache, param.Name);
        }
    }
}
