using System;
using System.Collections.Generic;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class AnimationInfoDataHandler : ParamAssetLoadedHandler<AnimationInfoData, AnimationInfoData.Param>
    {
        private static AnimationInfoDataHandler _instance;


        private static bool _hooksInitialized;
        private static readonly object HookLock = new object();

        private readonly Dictionary<string, string> _byName = new Dictionary<string, string>();

        public AnimationInfoDataHandler(TextResourceRedirector plugin) :
            base(plugin, true)
        {
            _instance = this;
            InitHooks();
        }

        public override IEnumerable<AnimationInfoData.Param> GetParams(AnimationInfoData asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            AnimationInfoData.Param param)
        {
            var result = false;
            var key = param.nameAnimation;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                _byName[key] = translated;
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

        public override bool DumpParam(SimpleTextTranslationCache cache, AnimationInfoData.Param param)
        {
            var key = param.nameAnimation;
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            var val = string.Empty;
            cache.AddTranslationToCache(key, val);
            return true;
        }

        private void InitHooks()
        {
            if (_hooksInitialized) return;
            lock (HookLock)
            {
                if (_hooksInitialized) return;
                Harmony.CreateAndPatchAll(typeof(Hooks));
                _hooksInitialized = true;
            }
        }

        private void PositionNameTranslatingCallback(ComponentTranslationContext context)
        {
            try
            {
                if (!Enabled || !_byName.TryGetValue(context.OriginalText, out var translated)) return;
                context.OverrideTranslatedText(translated);
            }
            catch (Exception err)
            {
                Logger.LogWarning($"{nameof(PositionNameTranslatingCallback)}: {err.Message}");
                UnityEngine.Debug.LogException(err);
            }
        }

        internal static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.LoadMotionList))]
            private static void HSpriteLoadMotionListPrefix()
            {
                try
                {
                    if (_instance == null || !_instance.Enabled) return;
                    AutoTranslator.Default.RegisterOnTranslatingCallback(_instance.PositionNameTranslatingCallback);
                }
                catch (Exception err)
                {
                    Logger.LogWarning($"{nameof(HSpriteLoadMotionListPrefix)}: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), nameof(HSprite.LoadMotionList))]
            private static void HSpriteLoadMotionListPostfix()
            {
                try
                {
                    if (_instance == null || !_instance.Enabled) return;
                    AutoTranslator.Default.UnregisterOnTranslatingCallback(_instance.PositionNameTranslatingCallback);
                }
                catch (Exception err)
                {
                    Logger.LogWarning($"{nameof(HSpriteLoadMotionListPostfix)}: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }
        }
    }
}
