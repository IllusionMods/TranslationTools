using System;
using System.Collections.Generic;
using ActionGame;
using ChaCustom;
using CharaFiles;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class VoiceInfoHandler : ParamAssetLoadedHandler<VoiceInfo, VoiceInfo.Param>
    {
        private static VoiceInfoHandler _instance;

        private static bool _hooksInitialized;
        private static readonly object HookLock = new object();

        private readonly Dictionary<int, string> _personalityById = new Dictionary<int, string>();

        private readonly Dictionary<string, string> _personalityByName = new Dictionary<string, string>();
        // game searches by personality name against this table
        // can't do an in-place update of personality name table without possibly
        // breaking things

        public VoiceInfoHandler(TextResourceRedirector plugin) : base(plugin, true)
        {
            _instance = this;
            InitHooks();
        }


        public override IEnumerable<VoiceInfo.Param> GetParams(VoiceInfo asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            VoiceInfo.Param param)
        {
            var result = false;
            var key = param.Personality;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                _personalityById[param.No] = translated;
                _personalityByName[key] = translated;
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

        public override bool DumpParam(SimpleTextTranslationCache cache, VoiceInfo.Param param)
        {
            var key = param.Personality;
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            var val = string.Empty;
            cache.AddTranslationToCache(key, val);
            return true;
        }

        public bool TryNameLookup(int personality, string originalName, out string translatedPersonalityName)
        {
            return TryNameLookup(personality, out translatedPersonalityName) ||
                   TryNameLookup(originalName, out translatedPersonalityName);
        }

        public bool TryNameLookup(int personality, out string translatedPersonalityName)
        {
            return _personalityById.TryGetValue(personality, out translatedPersonalityName);
        }

        public bool TryNameLookup(string originalName, out string translatedPersonalityName)
        {
            return _personalityByName.TryGetValue(originalName, out translatedPersonalityName);
        }

        private void InitHooks()
        {
            if (_hooksInitialized) return;
            lock (HookLock)
            {
                if (_hooksInitialized) return;
                try
                {
                    Harmony.CreateAndPatchAll(typeof(Hooks));
                    _hooksInitialized = true;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{GetType().Name}.{nameof(InitHooks)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }
        }


        private static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Localize.Translate.Manager), nameof(Localize.Translate.Manager.GetPersonalityName))]
            private static void GetPersonalityNamePostfix(int personality, bool check, ref string __result)
            {
                if (_instance == null) return;
                try
                {
                    if (_instance.TryNameLookup(personality, __result, out var tmpResult)) __result = tmpResult;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(GetPersonalityNamePostfix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PreviewClassData.InfoData),
                nameof(PreviewClassData.InfoData.SetInfo))]
            private static void InfoDataSetInfoPrefix(ref string personality)
            {
                if (_instance == null) return;
                try
                {
                    if (_instance.TryNameLookup(personality, out var tmpResult)) personality = tmpResult;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(InfoDataSetInfoPrefix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }


            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaFileInfoComponent), nameof(ChaFileInfoComponent.SetPersonality), typeof(string))]
            [HarmonyPatch(typeof(CustomFileInfoComponent), nameof(CustomFileInfoComponent.SetPersonality),
                typeof(string))]
            [HarmonyPatch(typeof(HeroineStatusComponent.Page1), nameof(HeroineStatusComponent.Page1.SetPersonality), typeof(string))]
            private static void SetPersonalityPrefix(ref string text)
            {
                if (_instance == null) return;
                try
                {
                    if (_instance.TryNameLookup(text, out var tmpResult)) text = tmpResult;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(SetPersonalityPrefix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CharaHInfoComponent), nameof(CharaHInfoComponent.SetCharaInfo))]
            private static void CharaHInfoComponentSetCharaInfoPostfix(CharaHInfoComponent __instance,
                ChaFileControl chaFileCtrl)
            {
                if (_instance == null || __instance == null || __instance.textPersonality == null) return;
                try
                {
                    if (_instance.TryNameLookup(chaFileCtrl.parameter.personality, __instance.textPersonality.text, out var tmpResult))
                    {
                        __instance.textPersonality.text = tmpResult;
                    }
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(CharaHInfoComponentSetCharaInfoPostfix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }
        }
    }
}
