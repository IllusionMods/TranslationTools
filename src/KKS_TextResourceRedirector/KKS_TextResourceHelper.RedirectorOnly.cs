using System.Runtime.InteropServices.WindowsRuntime;
using ADV;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public partial class KKS_TextResourceHelper
    {
        public override void DumpScenarioParam(ScenarioData.Param param, SimpleTextTranslationCache cache)
        {
            if (!IsSupportedCommand(param.Command))
            {
                Logger.DebugLogDebug("{0} skipping unsupported command: {1}", GetType().Name, param.Command);
                return;
            }

            if (SelectionCommands.Contains(param.Command))
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

                return;
            }

            base.DumpScenarioParam(param, cache);
        }

        public override bool ReplaceOrUpdateScenarioParam(string calculatedModificationPath, ScenarioData.Param param, SimpleTextTranslationCache cache)
        {
            var result = false;
            if (!IsSupportedCommand(param.Command))
            {
                Logger.DebugLogDebug("{0} skipping unsupported command: {1}", GetType().Name, param.Command);
                return false;
            }

            if (SelectionCommands.Contains(param.Command))
            {
                foreach (var i in GetScenarioCommandTranslationIndexes(param.Command))
                {
                    
                    if (TryRegisterScenarioTranslation(cache, param, i, calculatedModificationPath)) result = true;
                }

                // don't fall through to base call for selection commands
                return result;

            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse -- future proofing
            return result || base.ReplaceOrUpdateScenarioParam(calculatedModificationPath, param, cache);
        }
    }
}
