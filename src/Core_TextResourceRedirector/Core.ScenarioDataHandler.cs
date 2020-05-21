#if !HS
using System.IO;
using System.Linq;
using ADV;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    

    public class ScenarioDataHandler : AssetLoadedHandlerBaseV2<ScenarioData>, IPathListBoundHandler
    {
        private readonly TextResourceHelper _textResourceHelper;

        public ScenarioDataHandler(TextResourceHelper helper)
        {
            CheckDirectory = true;
            _textResourceHelper = helper;
            Logger.LogInfo($"{GetType()} enabled");
        }

        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        protected override string CalculateModificationFilePath(ScenarioData asset,
            IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool DumpAsset(string calculatedModificationPath, ScenarioData asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            foreach (var param in asset.list)
            {
                if (!_textResourceHelper.IsSupportedCommand(param.Command))
                {
                    continue;
                }

                if (param.Command == Command.Text)
                {
                    foreach (var key in param.Args)
                    {
                        if (!string.IsNullOrEmpty(key) && !_textResourceHelper.TextKeysBlacklist.Contains(key) &&
                            LanguageHelper.IsTranslatable(key))
                        {
                            cache.AddTranslationToCache(key, key);
                        }
                    }
                }
                else if (param.Command == Command.Calc)
                {
                    if (param.Args.Length >= 3 && _textResourceHelper.CalcKeys.Contains(param.Args[0]))
                    {
                        cache.AddTranslationToCache(param.Args[2], param.Args[2]);
                    }
                }
                else if (param.Command == Command.Format)
                {
                    if (param.Args.Length >= 2 && _textResourceHelper.FormatKeys.Contains(param.Args[0]))
                    {
                        cache.AddTranslationToCache(param.Args[1], param.Args[1]);
                    }
                }
                else if (param.Command == Command.Choice)
                {
                    for (var i = 0; i < param.Args.Length; i++)
                    {
                        var key = _textResourceHelper.GetSpecializedKey(param, i, out var value);

                        if (!key.IsNullOrEmpty())
                        {
                            cache.AddTranslationToCache(key, value);
                        }
                    }
                }
#if false
                else if (param.Command == ADV.Command.Switch)
                {
                    for (int i
 = 1; i < param.Args.Length; i += 1)
                    {
                        cache.AddTokenTranslationToCache(param.Args[i], param.Args[i]);
                    }
                }
#endif
#if false
                else if (param.Command == ADV.Command.InfoText)
                {
                    for (int i
 = 1; i < param.Args.Length; i += 1)
                    {
                        cache.AddTokenTranslationToCache(param.Args[i], param.Args[i]);
                    }
                }
#endif
#if false
                else if (param.Command == ADV.Command.Jump)
                {
                    // TODO: detect if should be dumped
                    if (param.Args.Length >= 1)
                    {
                       cache.AddTokenTranslationToCache(param.Args[0], param.Args[0]);
                    }
                }
#endif
            }

            return true;
        }

        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref ScenarioData asset,
            IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()} attempt to handle {calculatedModificationPath}");
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);

            if (cache.IsEmpty)
            {
                Logger.DebugLogDebug($"{GetType()} unable to handle {calculatedModificationPath} (no cache)");
                return false;
            }

            var result = false;
            foreach (var param in asset.list)
            {
                if (!_textResourceHelper.IsSupportedCommand(param.Command))
                {
                    Logger.DebugLogDebug($"{GetType()} skipping unsupported command: {param.Command}");
                    continue;
                }

                switch (param.Command)
                {
                    case Command.Text:

                        // Text: 0 - jp speaker (if present), 1 - text
                        for (var i = 0; i < param.Args.Length && i < 2; i++)
                        {
                            var key = param.Args[i];
                            if (key.IsNullOrEmpty() || _textResourceHelper.TextKeysBlacklist.Contains(key) || StringIsSingleReplacement(key)) continue;
                            if (TryRegisterTranslation(cache, param, i, calculatedModificationPath)) result = true;
                        }

                        break;

                    case Command.Calc:
                    {
                        if (param.Args.Length >= 3 && _textResourceHelper.CalcKeys.Contains(param.Args[0]))
                        {
                            if (TryRegisterTranslation(cache, param, 2, calculatedModificationPath)) result = true;
                        }

                        break;
                    }
                    case Command.Format:
                    {
                        if (param.Args.Length >= 2 && _textResourceHelper.FormatKeys.Contains(param.Args[0]))
                        {
                            if (TryRegisterTranslation(cache, param, 1, calculatedModificationPath)) result = true;
                        }

                        break;
                    }
                    case Command.Choice:
                    {
                        for (var i = 0; i < param.Args.Length; i++)
                        {
                            if (TryRegisterTranslation(cache, param, i, calculatedModificationPath)) result = true;
                        }

                        break;
                    }
#if false
                    case ADV.Command.Switch:
                        // TODO
                        break;
#if AI
                    case ADV.Command.InfoText:
                        // TODO
                        break;
#endif
                    case ADV.Command.Jump:
                        // TODO
                        break;
#endif
                    default:
                    {
                        Logger.LogWarning(
                            $"{GetType()} expected to handle {param.Command}, but support not implemented");

                        break;
                    }
                }

            }

            Logger.DebugLogDebug(result
                ? $"{GetType()} handled {calculatedModificationPath}"
                : $"{GetType()} unable to handle {calculatedModificationPath}");

            return result;
        }

        private bool TryRegisterTranslation(SimpleTextTranslationCache cache, ScenarioData.Param param, int i,
            string calculatedModificationPath)
        {
            var key = _textResourceHelper.GetSpecializedKey(param, i, out var value);
            if (!string.IsNullOrEmpty(key))
            {
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    var result = _textResourceHelper.GetSpecializedTranslation(param, i, translated);
                    TranslationHelper.RegisterRedirectedResourceTextToPath(result, calculatedModificationPath);
                    param.Args[i] = result;
                    Logger.DebugLogDebug($"{GetType()} handled {calculatedModificationPath}");
                    return true;
                }

                if (LanguageHelper.IsTranslatable(key))
                {
                    TranslationHelper.RegisterRedirectedResourceTextToPath(key, calculatedModificationPath);
                    if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled)
                    {
                        cache.AddTranslationToCache(key, value);
                    }
                }
            }

            return false;
        }

        protected override bool ShouldHandleAsset(ScenarioData asset, IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}])?");
            var result = !context.HasReferenceBeenRedirectedBefore(asset) && this.IsPathAllowed(asset, context);
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}]) => {result}");
            return result;
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
        public ScenarioDataHandler(TextResourceHelper _ = null) { }
    }
}
#endif
