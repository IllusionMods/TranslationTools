using ChaCustom;
using CharaFiles;
using HarmonyLib;
using System;
using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class ClubInfoHandler : ParamAssetLoadedHandler<ClubInfo, ClubInfo.Param>
    {
        private static ClubInfoHandler _instance;

        private static bool _hooksInitialized;
        private static readonly object HookLock = new object();

        private readonly Dictionary<int, string> _nameById = new Dictionary<int, string>();
        private readonly Dictionary<string, string> _nameByString = new Dictionary<string, string>();

        private readonly Dictionary<int, string> _placeById = new Dictionary<int, string>();
        private readonly Dictionary<string, string> _placeByString = new Dictionary<string, string>();

        public ClubInfoHandler(TextResourceRedirector plugin) : base(plugin, true)
        {
            _instance = this;
            InitHooks();
        }

        public override IEnumerable<ClubInfo.Param> GetParams(ClubInfo asset)
        {
            return asset.param;
        }


        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ClubInfo.Param param)
        {
            var nameResult = UpdateName(calculatedModificationPath, cache, param);
            var placeResult = UpdatePlace(calculatedModificationPath, cache, param);
            return nameResult || placeResult;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, ClubInfo.Param param)
        {
            var nameResult = DefaultDumpParam(cache, param, param.Name);
            var placeResult = DefaultDumpParam(cache, param, param.Place);
            return nameResult || placeResult;
        }

        private bool UpdatePlace(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ClubInfo.Param param)
        {
            var key = param.Place;
            var result = false;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                _placeByString[key] = translated;
                _placeById[param.ID] = translated;
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

        private bool UpdateName(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ClubInfo.Param param)
        {
            var result = false;
            var key = param.Name;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                _nameByString[key] = translated;
                _nameById[param.ID] = translated;
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

        public bool TryNameLookup(int id, string originalName, out string translatedName)
        {
            return TryNameLookup(id, out translatedName) ||
                   TryNameLookup(originalName, out translatedName);
        }

        public bool TryNameLookup(int id, out string translatedName)
        {
            return _nameById.TryGetValue(id, out translatedName);
        }

        public bool TryNameLookup(string originalName, out string translatedName)
        {
            return _nameByString.TryGetValue(originalName, out translatedName);
        }

        public bool TryPlaceLookup(int id, string originalPlace, out string translatedPlace)
        {
            return TryPlaceLookup(id, out translatedPlace) ||
                   TryPlaceLookup(originalPlace, out translatedPlace);
        }

        public bool TryPlaceLookup(int id, out string translatedPlace)
        {
            return _placeById.TryGetValue(id, out translatedPlace);
        }

        public bool TryPlaceLookup(string originalPlace, out string translatedPlace)
        {
            return _placeByString.TryGetValue(originalPlace, out translatedPlace);
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
            [HarmonyPatch(typeof(Localize.Translate.Manager), nameof(Localize.Translate.Manager.GetClubName))]
            private static void GetClubNamePostfix(int clubActivities, bool check, ref string __result)
            {
                if (_instance == null || !_instance.Enabled) return;
                try
                {
                    if (_instance.TryNameLookup(clubActivities, __result, out var tmpResult)) __result = tmpResult;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(GetClubNamePostfix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaFileInfoComponent), nameof(ChaFileInfoComponent.SetClub), typeof(string))]
            [HarmonyPatch(typeof(CustomFileInfoComponent), nameof(CustomFileInfoComponent.SetClub),
                typeof(string))]
            private static void SetClubPrefix(ref string text)
            {
                if (_instance == null) return;
                try
                {
                    if (_instance.TryNameLookup(text, out var tmpResult)) text = tmpResult;
                }
                catch (Exception err)
                {
                    Logger?.LogWarning(
                        $"{nameof(SetClubPrefix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(Passport), nameof(Passport.Set))]
            private static void PassportSetPostfix(Passport __instance, SaveData.CharaData charaData)
            {
                if (_instance == null || !_instance.Enabled) return;
                try
                {
                    if (_instance.TryNameLookup(__instance._activity.text, out var tmpResult))
                    {
                        __instance._activity.text = tmpResult;
                    }
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(PassportSetPostfix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(StudentCardControlComponent), nameof(StudentCardControlComponent.SetCharaInfo))]
            private static void StudentCardControlComponentSetCharaInfoPostfix(StudentCardControlComponent __instance, ChaFileControl chaFileCtrl)
            {
                if (_instance == null || !_instance.Enabled) return;
                try
                {
                    if (_instance.TryNameLookup((int)chaFileCtrl.parameter.clubActivities, __instance.textClub.text, out var tmpResult))
                    {
                        __instance.textClub.text = tmpResult;
                    }
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(StudentCardControlComponentSetCharaInfoPostfix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }
        }
    }
}
