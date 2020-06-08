using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public abstract class ParamAssetLoadedHandler<T, TParam> : RedirectorAssetLoadedHandlerBase<T> where T : Object
    {


        protected ParamAssetLoadedHandler(TextResourceRedirector plugin) : base(plugin) { }
        public abstract IEnumerable<TParam> GetParams(T asset);

        public abstract bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache, TParam param);

        public abstract bool DumpParam(SimpleTextTranslationCache cache, TParam param);

        protected override bool DumpAsset(string calculatedModificationPath, T asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            var result = false;
            foreach (var entry in GetParams(asset))
            {
                if (DumpParam(cache, entry)) result = true;
            }

            return result;
        }
        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref T asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);

            if (cache.IsEmpty) return false;

            var result = false;


            foreach (var entry in GetParams(asset))
            {
                if (UpdateParam(calculatedModificationPath, cache, entry)) result = true;
            }

            return result;
        }
       
    }
}
