using System;
using System.Collections;
using System.Linq;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    internal static class TutorialScopeHelper
    {
        private const int TutorialScope = -1024;
        private static bool _enabled = false;

        internal static bool Initialized;

        internal static bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                if (_enabled)
                {
                    AutoTranslator.Default.RegisterOnTranslatingCallback(TutorialScopeHelperCallback);
                    TextResourceRedirector.Instance.StartCoroutine(DisableTutorialScopeCallback());
                }
                else
                {
                    AutoTranslator.Default.UnregisterOnTranslatingCallback(TutorialScopeHelperCallback);
                }
            }
        }

        internal static void Init(TextResourceRedirector plugin)
        {
            if (Initialized) return;
            Initialized = true;
            TextResourceRedirector.Logger.LogDebug($"Hooking {typeof(Hooks).FullName}");
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }


        private static void TutorialScopeHelperCallback(ComponentTranslationContext obj)
        {
            if (!Enabled) return;
            if (AutoTranslator.Default.TryTranslate(obj.OriginalText, TutorialScope, out var translatedText))
            {
                obj.OverrideTranslatedText(translatedText);
            }
        }

        private static IEnumerator DisableTutorialScopeCallback()
        {
            // need to wait a frame before checking starts
            yield return null;
            
            while (Manager.Scene.Overlaps.Contains(SingletonInitializer<Tutorial>.instance))
            {
                yield return null;
            }
            if (!Enabled) yield break;
            Enabled = false;
        }

        private static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Tutorial), nameof(Tutorial.Open))]
            private static void TutorialOpenPrefix()
            {
                try
                {
                    Enabled = true;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger.LogWarning($"{nameof(TutorialOpenPrefix)}: {err}");
                    UnityEngine.Debug.LogException(err);
                }
            }
        }
    }
}
