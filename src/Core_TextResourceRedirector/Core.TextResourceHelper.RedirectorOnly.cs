#if !HS
using System.Linq;
using ADV;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public partial class TextResourceHelper
    {
        public virtual void DumpScenarioParam(ScenarioData.Param param, SimpleTextTranslationCache cache)
        {
            if (!IsSupportedCommand(param.Command))
            {
                return;
            }

            if (param.Command == Command.Text)
            {
                foreach (var i in GetScenarioCommandTranslationIndexes(param.Command))
                {
                    var key = param.Args[i];
                    if (!string.IsNullOrEmpty(key) && !TextKeysBlacklist.Contains(key) &&
                        LanguageHelper.IsTranslatable(key))
                    {
                        cache.AddTranslationToCache(key, key);
                    }
                }
            }
            else if (param.Command == Command.Calc)
            {
                if (param.Args.Length >= 3 && CalcKeys.Contains(param.Args[0]))
                {
                    foreach (var i in GetScenarioCommandTranslationIndexes(param.Command))
                    {
                        var key = param.Args[i];
                        cache.AddTranslationToCache(key, key);
                    }
                }
            }
            else if (param.Command == Command.Format)
            {
                if (param.Args.Length >= 2 && FormatKeys.Contains(param.Args[0]))
                {
                    foreach (var i in GetScenarioCommandTranslationIndexes(param.Command))
                    {
                        var key = param.Args[i];
                        cache.AddTranslationToCache(key, key);
                    }
                }
            }
            else if (param.Command == Command.Choice)
            {
                for (var i = 0; i < param.Args.Length; i++)
                {
                    var key = GetSpecializedKey(param, i, out var value);

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

        public virtual bool TryRegisterScenarioTranslation(SimpleTextTranslationCache cache, ScenarioData.Param param, int i,
            string calculatedModificationPath)
        {

            var origKey = param.Args.SafeGet(i);
            if (origKey.IsNullOrEmpty()) return false;
            foreach (var key in GetTranslationKeys(param, i))
            {
                if (string.IsNullOrEmpty(key)) return false;
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    var result = GetSpecializedTranslation(param, i, translated);
                    TranslationHelper.RegisterRedirectedResourceTextToPath(result, calculatedModificationPath);
                    param.Args[i] = result;
                    Logger.DebugLogDebug("{0} handled {1}", GetType(), calculatedModificationPath);
                    return true;
                }

                if (!LanguageHelper.IsTranslatable(origKey)) return false;
                TranslationHelper.RegisterRedirectedResourceTextToPath(key, calculatedModificationPath);
                if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled)
                {
                    cache.AddTranslationToCache(key, key);
                }
            }

            return false;
        }


        public virtual bool ReplaceOrUpdateScenarioParam(string calculatedModificationPath, ScenarioData.Param param,
            SimpleTextTranslationCache cache)
        {
            var result = false;
            if (!IsSupportedCommand(param.Command))
            {
                Logger.DebugLogDebug($"{GetType()} skipping unsupported command: {param.Command}");
                return false;
            }

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (param.Command)
            {
                case Command.Text:

                    // Text: 0 - jp speaker (if present), 1 - text
                    foreach (var i in GetScenarioCommandTranslationIndexes(param.Command))
                    {
                        var key = param.Args[i];
                        if (key.IsNullOrEmpty() || TextKeysBlacklist.Contains(key) ||
                            Helpers.StringIsSingleReplacement(key)) continue;
                        if (TryRegisterScenarioTranslation(cache, param, i, calculatedModificationPath)) result = true;
                    }

                    break;

                case Command.Calc:
                {
                    if (param.Args.Length >= 3 && CalcKeys.Contains(param.Args[0]))
                    {
                        foreach (var i in GetScenarioCommandTranslationIndexes(param.Command))
                        {
                            if (TryRegisterScenarioTranslation(cache, param, i, calculatedModificationPath))
                                result = true;
                        }

                    }

                    break;
                }
                case Command.Format:
                {
                    if (param.Args.Length >= 2 && FormatKeys.Contains(param.Args[0]))
                    {
                        foreach (var i in GetScenarioCommandTranslationIndexes(param.Command))
                        {
                            if (TryRegisterScenarioTranslation(cache, param, i, calculatedModificationPath))
                                result = true;
                        }
                    }

                    break;
                }
                case Command.Choice:
                {
                    for (var i = 0; i < param.Args.Length; i++)
                    {
                        if (TryRegisterScenarioTranslation(cache, param, i, calculatedModificationPath)) result = true;
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

            return result;
        }
    }
}
#endif
