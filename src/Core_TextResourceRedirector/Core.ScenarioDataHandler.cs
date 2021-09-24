#if !HS
using System.IO;
using ADV;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class ScenarioDataHandler : RedirectorAssetLoadedHandlerBase<ScenarioData>, IPathListBoundHandler
    {
        public ScenarioDataHandler(TextResourceRedirector plugin) : base(plugin) { }

        protected override bool DumpAsset(string calculatedModificationPath, ScenarioData asset,
            IAssetOrResourceLoadedContext context)
        {
            var cache = GetDumpCache(calculatedModificationPath, asset, context);

            foreach (var param in asset.list)
            {
                TextResourceHelper.DumpScenarioParam(param, cache);
            }

            return true;
        }


        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref ScenarioData asset,
            IAssetOrResourceLoadedContext context)
        {
            var result = false;
            var start = Time.realtimeSinceStartup;
            try
            {
                Logger.DebugLogDebug("{0}.{1} attempt to handle {2}", GetType(), nameof(ReplaceOrUpdateAsset),
                    calculatedModificationPath);
                var cache = GetTranslationCache(calculatedModificationPath, asset, context);

                if (cache.IsEmpty)
                {
                    Logger.DebugLogDebug("{0}.{1} unable to handle {2} (no cache)", GetType(),
                        nameof(ReplaceOrUpdateAsset), calculatedModificationPath);
                    return false;
                }

                foreach (var param in asset.list)
                {
                    if (this.TextResourceHelper.ReplaceOrUpdateScenarioParam(calculatedModificationPath, param, cache))
                        result = true;
                }

                Logger.DebugLogDebug(result ? "{0}.{1} handled {2}" : "{0}.{1} unable to handle {2}", GetType(),
                    nameof(ReplaceOrUpdateAsset), calculatedModificationPath);

                return result;
            }
            finally
            {
                Logger.DebugLogDebug(
                    "{0}.{1}: {2} => {3} ({4} seconds)", GetType(), nameof(ReplaceOrUpdateAsset), calculatedModificationPath, result, Time.realtimeSinceStartup - start);
            }
        }

        #region IPathListBoundHandler

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        #endregion IPathListBoundHandler
    }
}
#else //Stub for HS which has no ScenarioData
    namespace IllusionMods
{
    public class ScenarioDataHandler
    {
        public ScenarioDataHandler(TextResourceRedirector _ = null) { }
    }
}
#endif
