using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using IllusionMods.Shared.TextDumpBase;
using JetBrains.Annotations;
using Localize.Translate;
using UnityEngine;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    [UsedImplicitly]
    public partial class KKS_LocalizationDumpHelper
    {
        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(UIBinder), "Load")]
            private static void UIBinderLoadPrefix(UIBinder __instance, out TranslationHookState __state)
            {
                var gameObject = __instance.gameObject;
                var path = CombinePaths(gameObject.scene.path.Replace(".unity", ""), gameObject.name);
                BaseTextDumpPlugin.Logger.LogInfo($"[TextDump] Collecting UI info for {path}");
                var items = EnumerateTextComponents(gameObject).ToList();
                var components = items.Select(t => t.Value).ToList();
                var scopes = items.Select(t =>
                {
                    try
                    {
                        return t.Key.scene.buildIndex;
                    }
                    catch
                    {
                        return -1;
                    }
                }).ToList();


                __state = new TranslationHookState(path);

                __state.Context.Add(components);
                __state.Context.Add(scopes);
                var origValues = components.Select(GetTextFromSupportedComponent).ToList();
                __state.Context.Add(origValues);
                var origResizers = components.Select(GetTextResizerFromComponent).ToList();
                __state.Context.Add(origResizers);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UIBinder), "Load")]
            private static void UIBinderLoadPostfix(UIBinder __instance, TranslationHookState __state)
            {
                var gameObject = __instance.gameObject;
                var path = __state.Path;

                var components = (List<Component>) __state.Context[0];
                var scopes = (List<int>) __state.Context[1];
                var origValues = (List<string>) __state.Context[2];
                var origResizers = (List<XuaResizerResult>) __state.Context[3];

                var items = EnumerateTextComponents(gameObject).ToList();
                if (items.Count != components.Count)
                {
                    BaseTextDumpPlugin.Logger.LogWarning(
                        $"UIBinder {gameObject}: Component count has changed, may not be able to get all translations");
                }
                else
                {
                    components = items.Select(t => t.Value).ToList();
                }

                var results = new TranslationDictionary();
                var resizers = new ResizerCollection();

                for (var i = 0; i < components.Count; i++)
                {
                    var key = origValues[i];
                    var val = GetTextFromSupportedComponent(components[i]);

                    var scope = scopes[i];
                    _instance.AddLocalizationToResults(results.GetScope(scope), key, val);


                    var currentResizer = GetTextResizerFromComponent(components[i]);

                    var resizePath = components[i].GetXuaResizerPath();
                    if (!string.IsNullOrEmpty(resizePath))
                    {
                        var delta = currentResizer.Delta(origResizers[i]);
                        var scopedResizers = resizers.GetScope(scope);
                        scopedResizers[resizePath] = delta.GetDirectives().ToList();
                    }
                }

                var outputName = CombinePaths("Bind/UI", path);
                HookedTextLocalizationGenerators.Add(new StringTranslationDumper(outputName, () => results));
                HookedTextLocalizationGenerators.Add(new ResizerDumper(outputName, () => resizers));
            }
        }
    }
}
