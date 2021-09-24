using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ActionGame;
using ADV.Commands.Game;
using ChaCustom;
using HarmonyLib;
using Illusion;
using IllusionMods.Shared;
using IllusionMods.Shared.TextDumpBase;
using Localize.Translate;
using Manager;
using SaveData;
using UGUI_AssistLibrary;
using UnityEngine;
using UnityEngine.UI;
using UploaderSystem;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public partial class KKS_LocalizationDumpHelper : LocalizationDumpHelper
    {

        protected readonly Dictionary<string, HashSet<string>> TutorialCategoryMap =
            new Dictionary<string, HashSet<string>>();


        public override string LocalizationFileRemap(string outputFile)
        {
            var fileName = Path.GetFileNameWithoutExtension(outputFile);
            if (fileName.StartsWith("p_ai_tutorial"))
            {
                var mapName =
                    TutorialCategoryMap.Where(m => m.Value.Contains(fileName)).Select(m => m.Key).FirstOrDefault() ??
                    "xx_Unknown";

                return $"Tutorials/{mapName}/{fileName}.txt";
            }

            return base.LocalizationFileRemap(outputFile);
        }

        public override IEnumerable<ITranslationDumper> GetInstanceLocalizers()
        {
            foreach (var localizer in base.GetInstanceLocalizers())
            {
                yield return localizer;
            }

            yield return MakeStandardInstanceLocalizer<DayTimeChange>(false, "ArgsDefault");
            yield return MakeStandardInstanceLocalizer<CharaHInfoComponent>(false, "nonName", "strWeakPoint");
            yield return MakeStandardInstanceLocalizer<OutputAnmInfo>(false, "msg");
            yield return MakeStandardInstanceLocalizer<TimeUIControl>(false, "timeZoneNames");
            yield return MakeStandardInstanceLocalizer<TwoShotTest>(false, "mapName");
            yield return MakeStandardInstanceLocalizer<UIAL_ListCtrl>(false, "dropdownAllOptName",
                "dropdownNotOptName");

            yield return MakeStandardInstanceLocalizer<ActionChangeUI>(false, "message");
        }

        public override IEnumerable<ITranslationDumper> GetStaticLocalizers()
        {
            foreach (var localizer in base.GetStaticLocalizers()) yield return localizer;
            yield return MakeStandardStaticLocalizer(typeof(ClassCallConfirmViewer), false, "LiveIns", "TitleAnother");
            yield return MakeStandardStaticLocalizer(typeof(CustomFileListCtrl.CategoryInfo), false, "CharaLabels",
                "CoordinateLabels");
            /* Trial
            yield return MakeStandardStaticLocalizer(typeof(Cycle), false, "wakeUpWeekdayWords");
            yield return MakeStandardStaticLocalizer(typeof(ActionPoint), false, "wakeUpWords");
            */
            yield return MakeStandardStaticLocalizer(typeof(Utils.Comparer), false, "LABEL");
            yield return MakeStandardStaticLocalizer(typeof(Localize.Translate.Manager), false, "UnknownText");
            yield return MakeStandardStaticLocalizer(typeof(Game), false, "ClubNameMan");
            yield return MakeStandardStaticLocalizer(typeof(Passport), false, "BloodTypes");
            yield return MakeStandardStaticLocalizer(typeof(CharaData), false, "Names");
            yield return MakeStandardStaticLocalizer(typeof(Heroine), false, "relationLabels", "relationNPCLabels");

            yield return MakeStandardStaticLocalizer(typeof(NetworkDefine), false, "msgDownConfirmDelete",
                "msgDownDecompressingFile", "msgDownDeleteCache", "msgDownDeleteData", "msgDownDownloadData",
                "msgDownDownloaded", "msgDownFailed", "msgDownFailedDecompressingFile", "msgDownFailedDelete",
                "msgDownFailedGetThumbnail", "msgDownLikes", "msgDownNotUploadDataFound", "msgDownUnknown",
                "msgNetConfirmUser", "msgNetFailedConfirmUser", "msgNetFailedGetAllHN", "msgNetFailedGetCharaInfo",
                "msgNetFailedGetVersion", "msgNetFailedUpdateHN", "msgNetGetAllCharaInfo", "msgNetGetAllHN",
                "msgNetGetInfoFromServer", "msgNetGetVersion", "msgNetNotReady", "msgNetReady", "msgNetReadyGetData",
                "msgNetStartEntryHN", "msgNetUpdatedHN", "msgServerAccessField", "msgServerAccessInfoField",
                "msgServerCheck", "msgUpAlreadyUploaded", "msgUpCannotBeIdentified", "msgUpCannotRead",
                "msgUpConfirmation", "msgUpDone", "msgUpFailer", "strBtnUpload");

            yield return MakeStandardStaticLocalizer(typeof(RandomNetChara), false, "FailedMsg");
        }

        private static readonly string[] SupportedEnumerationTypeNames =
        {
            "UnityEngine.UI.Text",
            "UnityEngine.TextMesh",
            "TMPro.TMP_Text",
            "TMPro.TextMeshProUGUI",
            "TMPro.TextMeshPro",
            "UILabel",
            "FairyGUI.TextField"
        };

        private static System.Type[] SupportedEnumerationTypes = null;

        private static Dictionary<string, System.Type> SupportedEnumerationTypeMap = null;

        [SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline",
            Justification = "Dynamic initialization")]
        static KKS_LocalizationDumpHelper()
        {
            FormatStringRegex = new Regex(@"(\{[0-9]\}|\[[PH][^\]]*\])");
           

        }

        private static void InitSupportedEnumerationTypes()
        {
            if (SupportedEnumerationTypes != null) return;
            var typeMap = SupportedEnumerationTypeMap = new Dictionary<string, Type>();
            SupportedEnumerationTypes = new Type[0];

            foreach (var typeName in SupportedEnumerationTypeNames)
            {
                Type type = null;
                try
                {
                    type = AccessTools.TypeByName(typeName);
                }
                catch (Exception)
                {
                    type = TextDump.Helpers.FindType(typeName);
                }

                if (type == null)
                {
                    BaseTextDumpPlugin.Logger.LogDebug(
                        $"SupportedEnumerationTypes: Unable to find type {typeName} {type}, skipping.");
                }

                typeMap[typeName] = type;
            }

            SupportedEnumerationTypes = typeMap.Values.Where(o => o != null).ToArray();
        }

        protected KKS_LocalizationDumpHelper(TextDump plugin) : base(plugin)
        {
#if false
            OtherDataByTag[0] = new Dictionary<string, string>
            {
                {"InitCheck", "容姿を初期化しますか？"},
                {"Check", "選択した場所に移動しますか？"},
                {"ServerCheck", "サーバーをチェックしています"},
                {"ServerAccessField", "サーバーへのアクセスに失敗しました。"},
                {"Other", "特殊"}
            };

            // translateCharaInfo
            OtherDataByTag[1] = new Dictionary<string, string>
            {
                {"Month", "月"},
                {"Day", "日"},
                {"SampleColor", "左クリックで適用"}
                //{"LoadCheck", ""},
                //{"SaveCheck", ""},
                //{"SaveCheckOverride", ""},
                //{"Saved", ""},
            };


            // translateQuestionTitle
            OtherDataByTag[3] = new Dictionary<string, string>
            {
                {"Remove", "転校させますか？"},
                {"FailedData", "データの取得に失敗しました"}
                //{"RemoveAll", ""},
                //{"RemoveClass", ""}
            };

            // translateMessageTitle
            OtherDataByTag[4] = new Dictionary<string, string>
            {
                {"Members", "{0} 人"},
                {"Times", "{0} 回"}
            };

            // translateOther
            // OtherDataByTag[5] = new Dictionary<string, string> {};

            OtherDataByTag[100] = new Dictionary<string, string>
            {
                {"ReturnHome", "家に帰りますか？"},
                {"RestoreDefault", "設定を初期化しますか？"}
            };

            // translateQuestionTitle
            OtherDataByTag[995] = new Dictionary<string, string>
            {
                {"Restore", "キャラを編集前に戻しますか？"},
                {"Change", "キャラ画像を変更しますか？"},
                {"Erase", "本当に削除しますか？"},
                {"CoordinateInput", "コーディネート名を入力して下さい"},
                {"Overwrite", "本当に上書きしますか？"}
            };

            // translateSlotTitle
            //OtherDataByTag[996] = new Dictionary<string, string> {};
#endif
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        public static List<Heroine> LoadHeroines(string assetBundlePath)
        {
            var heroines = new Dictionary<int, TextAsset>();
            foreach (var assetBundleName in GetAssetBundleNameListFromPath(assetBundlePath))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName).Where(a => a.EndsWith(".bytes")))
                {
                    var textAsset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, null);
                    if (textAsset == null) continue;

                    if (int.TryParse(textAsset.name.Replace("c", string.Empty), out var id))
                    {
                        heroines[id] = textAsset;
                    }
                }
            }

            return heroines.Select(h =>
            {
                var heroine = new Heroine(false);
                Game.LoadFromTextAsset(h.Key, heroine, h.Value);
                return heroine;
            }).ToList();
        }

#if LOCALIZE
        protected override IEnumerable<ITranslationDumper> GetOtherDataLocalizers()
        {
            yield break;
        }
#endif

        protected static IEnumerable<KeyValuePair<GameObject, Component>> EnumerateTextComponents(GameObject gameObject,
            Component component, HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            var _ = binders;
            if (handled is null)
            {
                handled = new HashSet<object>();
            }

            if (!handled.Contains(component))
            {
                handled.Add(component);
                if (IsSupportedForEnumeration(component))
                {
                    //Logger.LogInfo($"EnumerateTextComponents: {gameObject} yield {text}");
                    yield return new KeyValuePair<GameObject, Component>(gameObject, component);
                }
                else
                {
                    var trav = Traverse.Create(component);
                    foreach (var fieldName in trav.Fields())
                    {
                        var field = trav.Field(fieldName);
                        var fieldType = field.GetValueType();
                        if (IsSupportedForEnumeration(fieldType))
                        {
                            var fieldValue = field.GetValue<Component>();
                            if (fieldValue != null && !handled.Contains(fieldValue))
                            {
                                //Logger.LogInfo($"EnumerateTextComponents: {gameObject} field {fieldName} text {fieldValue}");
                                yield return new KeyValuePair<GameObject, Component>(gameObject, fieldValue);
                            }
                        }
                        else if (typeof(Component).IsAssignableFrom(fieldType))
                        {
                            var subComponent = field.GetValue<Component>();
                            if (subComponent != null && !handled.Contains(subComponent))
                            {
                                foreach (var subValue in EnumerateTextComponents(gameObject, subComponent, handled))
                                {
                                    yield return subValue;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected static XuaResizerResult GetTextResizerFromComponent(Component component)
        {
            var result = new XuaResizerResult();
            var componentType = component.GetType();

            if (component is Text textComponent)
            {
                result.AutoResize = textComponent.resizeTextForBestFit;
                result.FontSize = textComponent.fontSize;
                result.LineSpacing = (decimal) textComponent.lineSpacing;
                result.HorizontalOverflow = textComponent.horizontalOverflow == HorizontalWrapMode.Overflow
                    ? XuaResizerResult.HorizontalOverflowValue.Overflow
                    : XuaResizerResult.HorizontalOverflowValue.Wrap;
                result.VerticalOverflow = textComponent.verticalOverflow == VerticalWrapMode.Overflow
                    ? XuaResizerResult.VerticalOverflowValue.Overflow
                    : XuaResizerResult.VerticalOverflowValue.Truncate;
            }

            // UILabel
            /*
            if (SupportedEnumerationTypeMap.TryGetValue("UILabel", out var uiLabelType) &&
                uiLabelType.IsAssignableFrom(componentType))
            {
                // nothing to track here?
                
            }
            */

            else if (SupportedEnumerationTypeMap.TryGetValue("TMPro.TextMeshPro", out var matchType) &&
                     matchType.IsAssignableFrom(componentType) ||
                     SupportedEnumerationTypeMap.TryGetValue("TMPro.TextMeshProUGUI", out matchType) &&
                     matchType.IsAssignableFrom(componentType))
            {
                var nullArgs = new object[0];

                T GetComponentPropertyValue<T>(string name)
                {
                    var propInfo = AccessTools.Property(matchType, name);
                    if (propInfo != null)
                    {
                        return (T) propInfo.GetValue(component, nullArgs);
                    }

                    var fieldInfo = AccessTools.Field(matchType, name);
                    if (fieldInfo != null)
                    {
                        return (T) fieldInfo.GetValue(component);
                    }

                    return default;
                }

                result.AutoResize = GetComponentPropertyValue<bool?>("enableAutoSizing");
                result.FontSize = (decimal?) GetComponentPropertyValue<float?>("fontSize");
                result.LineSpacing = (decimal?) GetComponentPropertyValue<float?>("lineSpacing");

                /*
                yield return new KeyValuePair<string, string>(path,
                    $"UGUI_HorizontalOverflow({textComponent.horizontalOverflow.ToString()})");
                yield return new KeyValuePair<string, string>(path,
                    $"UGUI_VerticalOverflow({textComponent.verticalOverflow.ToString()})");
                */
            }

            return result;
        }

        protected override IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            foreach (var generator in base.GetLocalizationGenerators())
            {
                yield return generator;
            }

            var readyToDump = TextDump.IsReadyForFinalDump();

            if (!readyToDump) yield break;

            InitSupportedEnumerationTypes();

            foreach (var dir in new[] {"tutorial", "abdata/menu/entrylive", 
                "h/scene/freehcharaselect", "network/entryhn"})
            {
                yield return () => GetBindLocalizers(dir);
            }

            yield return WrapTranslationCollector("Names/ScenarioChars", CollectScenarioCharsLocalizations);
            yield return WrapTranslationCollector("Names/Clubs", CollectClubNameLocalizations);
            yield return WrapTranslationCollector("Names/Heroines", CollectHeroineLocalizations);
        }

        protected IEnumerable<AssetBundleAddress> GetAssetBundleAddresses(string assetPath)
        {
            var assetBundleNames = GetAssetBundleNameListFromPath(assetPath);
            assetBundleNames.Sort();
            foreach (var assetBundleName in assetBundleNames)
            {
                string[] assetNames = null;
                try
                {
                    assetNames = GetAssetNamesFromBundle(assetBundleName);
                }
                catch
                {
                    assetNames = null;
                }

                if (assetNames is null) continue;
                foreach (var assetName in assetNames)
                {
                    yield return new AssetBundleAddress(string.Empty, assetBundleName, assetName, null);
                }
            }
        }

        protected GameObject[] LoadGameObjects(params AssetBundleAddress[] assetBundleAddresses)
        {
            var results = new GameObject[0];
            var gameObjects = AIProject.ListPool<GameObject>.Get();
            try
            {
                foreach (var abi in assetBundleAddresses)
                {
                    GameObject gameObject;

                    //Logger.LogError($"LoadGameObjects: assetbundle={abi.assetbundle}, asset={abi.asset}, manifest={abi.manifest}");
                    try
                    {
                        gameObject = ManualLoadAsset<GameObject>(abi);
                    }
                    catch
                    {
                        gameObject = null;
                    }

                    if (gameObject != null)
                    {
                        //Singleton<Manager.Resources>.Instance.AddLoadAssetBundle(abi.assetbundle, abi.manifest);
                        //Logger.LogFatal($"LoadGameObjects: {gameObject.name}");
                        gameObjects.Add(gameObject);
                    }
                }

                if (!gameObjects.IsNullOrEmpty())
                {
                    results = new GameObject[gameObjects.Count];
                    for (var l = 0; l < gameObjects.Count; l++)
                    {
                        results[l] = gameObjects[l];
                    }
                }
            }
            finally
            {
                AIProject.ListPool<GameObject>.Release(gameObjects);
            }

            return results;
        }

         protected IEnumerable<KeyValuePair<GameObject, Text>> EnumerateTexts(GameObject gameObject,
            MonoBehaviour component, HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            var _ = binders;
            if (handled is null)
            {
                handled = new HashSet<object>();
            }

            if (!handled.Contains(component))
            {
                handled.Add(component);
                if (component is Text text)
                {
                    //Logger.LogInfo($"EnumerateTexts: {gameObject} yield {text}");
                    yield return new KeyValuePair<GameObject, Text>(gameObject, text);
                }
                else
                {
                    var trav = Traverse.Create(component);
                    foreach (var fieldName in trav.Fields())
                    {
                        var field = trav.Field(fieldName);
                        var fieldType = field.GetValueType();
                        if (fieldType == typeof(Text))
                        {
                            var fieldValue = field.GetValue<Text>();
                            if (fieldValue != null && !handled.Contains(fieldValue))
                            {
                                //Logger.LogInfo($"EnumerateTexts: {gameObject} field {fieldName} text {fieldValue}");
                                yield return new KeyValuePair<GameObject, Text>(gameObject, fieldValue);
                            }
                        }
                        else if (typeof(MonoBehaviour).IsAssignableFrom(fieldType))
                        {
                            var subBehaviour = field.GetValue<MonoBehaviour>();
                            if (subBehaviour != null && !handled.Contains(subBehaviour))
                            {
                                foreach (var subValue in EnumerateTexts(gameObject, subBehaviour, handled))
                                {
                                    yield return subValue;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected IEnumerable<KeyValuePair<GameObject, Text>> EnumerateTexts(GameObject gameObject,
            HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            handled = handled ?? new HashSet<object>();

            if (handled.Contains(gameObject)) yield break;
            handled.Add(gameObject);

            if (binders != null)
            {
                foreach (var binder in gameObject.GetComponents<UIBinder>())
                {
                    if (!binders.Contains(binder))
                    {
                        binders.Add(binder);
                    }
                }
            }

            foreach (var text in gameObject.GetComponents<Text>())
            {
                //Logger.LogInfo($"EnumerateTexts: {gameObject} GetComponents (text) {text}");
                yield return new KeyValuePair<GameObject, Text>(gameObject, text);
            }

            foreach (var component in gameObject.GetComponents<MonoBehaviour>())
            {
                foreach (var result in EnumerateTexts(gameObject, component, handled, binders))
                {
                    yield return result;
                }
            }

            foreach (var childText in GetChildrenFromGameObject(gameObject)
                .SelectMany(child => EnumerateTexts(child, handled, binders)))
            {
                yield return childText;
            }
        }

        protected IEnumerable<ITranslationDumper> GetBindLocalizers(string assetPath)
        {
            var handled = new HashSet<object>();
            foreach (var entry in GetAssetBundleAddresses(assetPath))
            {
                var path = CombinePaths(
                    Path.GetDirectoryName(entry.AssetBundle),
                    Path.GetFileNameWithoutExtension(entry.Asset));
                foreach (var gameObject in LoadGameObjects(entry))
                {
                    var outputName = $"Bind/{path}/{gameObject.name}";

                    Dictionary<string, string> Localizer()
                    {
                        var binders = new List<UIBinder>();
                        var textList = EnumerateTexts(gameObject, handled, binders).Select(t => t.Value).ToArray();
                        var before = textList.Select(t => t.text).ToArray();

                        foreach (var binder in binders)
                        {
                            var binderLoad = Traverse.Create(binder).Method("Load");
                            if (binderLoad?.MethodExists() == true)
                            {
                                binderLoad.GetValue();
                            }
                        }

                        var results = new Dictionary<string, string>();
                        var after = textList.Select(t => t.text).ToArray();
                        for (var i = 1; i < before.Length; i++)
                        {
                            AddLocalizationToResults(results, before[i], after[i]);
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(outputName, Localizer);
                }
            }
        }

        // TODO: public static string GetClubName(int clubActivities, bool check)
        // TODO: public static string GetPersonalityName(int personality, bool check)

        /*
        private IEnumerable<ITranslationDumper> MakeManagerResourceLocalizers()
        {
            foreach (var resource in _managerResources)
            {
                var localize = Singleton<Manager.Resources>.Instance.Localize;
                //Logger.LogWarning(localize);
                var func = AccessTools.Method(localize.GetType(), $"Get{resource.Key}");
                //Logger.LogWarning(func);

                if (func is null)
                {
                    continue;
                }

                var getter = (Func<int, string>)Delegate.CreateDelegate(typeof(Func<int, string>), localize, func);

                //Logger.LogWarning(getter);
                Dictionary<string, string> Localizer()
                {
                    var results = new Dictionary<string, string>();
                    foreach (var entry in resource.Value)
                    {
                        AddLocalizationToResults(results, entry.Value, getter(entry.Key));
                    }

                    return results;
                }

                yield return new StringTranslationDumper($"Manager/Resources/{resource.Key}", Localizer);
            }
        }
        */

        protected override string GetPersonalityName(VoiceInfo.Param voiceInfo)
        {
            var assetBundleNames = GetAssetBundleNameListFromPath("etcetra/list/config/", true);
            foreach (var voice in from assetBundleName in assetBundleNames
                from assetName in GetAssetNamesFromBundle(assetBundleName)
                select ManualLoadAsset<VoiceInfo>(assetBundleName, assetName, null)
                into asset
                where !(asset is null)
                from voice in asset.param.Where(voice =>
                    voice.No == voiceInfo.No && !voice.Personality.IsNullOrWhiteSpace())
                select voice)
            {
                return voice.Personality;
            }

            return voiceInfo.Personality;
        }

        protected override string GetPersonalityNameLocalization(VoiceInfo.Param voiceInfo)
        {
            return Localize.Translate.Manager.GetPersonalityName(voiceInfo.No, false);
        }

        private static bool IsSupportedForEnumeration(Type type)
        {
            return type != null &&
                   SupportedEnumerationTypes.Any(supported => type == supported || type.IsSubclassOf(supported));
        }

        private static bool IsSupportedForEnumeration(Component component)
        {
            return IsSupportedForEnumeration(component.GetType());
        }

        private static string GetTextFromSupportedComponent(Component component)
        {
            if (component == null) return string.Empty;

            var componentType = component.GetType();
            var fieldInfo = AccessTools.Property(componentType, "text");
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(component, new object[0]) as string;
            }

            var propInfo = AccessTools.Property(componentType, "text");
            if (propInfo != null)
            {
                return propInfo.GetValue(component, new object[0]) as string;
            }

            BaseTextDumpPlugin.Logger.LogWarning($"Unable to access 'text' property for {component}");
            return string.Empty;
        }

        private static IEnumerable<KeyValuePair<GameObject, Component>> EnumerateTextComponents(GameObject gameObject,
            HashSet<object> handled = null, List<UIBinder> binders = null)
        {
            handled = handled ?? new HashSet<object>();

            if (handled.Contains(gameObject)) yield break;
            handled.Add(gameObject);

            if (binders != null)
            {
                foreach (var binder in gameObject.GetComponents<UIBinder>())
                {
                    if (!binders.Contains(binder))
                    {
                        binders.Add(binder);
                    }
                }
            }

            foreach (var component in gameObject.GetComponents<Component>())
            {
                //Logger.LogInfo($"EnumerateTextComponents: {gameObject} GetComponents (text) {text}");
                if (IsSupportedForEnumeration(component))
                {
                    yield return new KeyValuePair<GameObject, Component>(gameObject, component);
                }

                foreach (var result in EnumerateTextComponents(gameObject, component, handled, binders))
                {
                    yield return result;
                }
            }

            foreach (var childText in GetChildrenFromGameObject(gameObject)
                .SelectMany(child => EnumerateTextComponents(child, handled, binders)))
            {
                yield return childText;
            }
        }

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

        private IDictionary<string, string> CollectHeroineLocalizations()
        {
            var results = new OrderedDictionary<string, string>();
            var baseHeroines = LoadHeroines("action/fixchara");
            if (baseHeroines == null) return results;

            var translatedHeroines = LoadHeroines("localize/translate/1/defdata/action/fixchara");

            for (var i = 0; i < baseHeroines.Count; i++)
            {
                var baseHeroine = baseHeroines[i];
                var translatedHeroine = translatedHeroines != null && translatedHeroines.Count > i
                    ? translatedHeroines[i]
                    : null;

                var translatedFirst = translatedHeroine?.param.chara.firstname ?? string.Empty;
                var translatedLast = translatedHeroine?.param.chara.lastname ?? string.Empty;
                AddLocalizationToResults(results, baseHeroine.param.chara.firstname,
                    translatedFirst);
                AddLocalizationToResults(results, baseHeroine.param.chara.lastname,
                    translatedLast);
                AddLocalizationToResults(results, baseHeroine.param.chara.nickname,
                    translatedHeroine?.param.chara.nickname ?? string.Empty);

                // Add names in both orders
                AddLocalizationToResults(results,
                    JoinStrings(" ", baseHeroine.param.chara.firstname, baseHeroine.param.chara.lastname).Trim(),
                    JoinStrings(" ", translatedFirst, translatedLast).Trim());
                AddLocalizationToResults(results,
                    JoinStrings(" ", baseHeroine.param.chara.lastname, baseHeroine.param.chara.firstname).Trim(),
                    JoinStrings(" ", translatedLast, translatedFirst).Trim());
            }

            return results;
        }

        private Dictionary<string, string> CollectClubNameLocalizations()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;
            if (!Singleton<Game>.IsInstance()) return results;

            var clubInfos = Game.ClubInfos ?? new Dictionary<int, ClubInfo.Param>();


            var assetBundleNames = GetAssetBundleNameListFromPath("action/list/clubinfo/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var asset = ManualLoadAsset<ClubInfo>(assetBundleName, assetName, null);
                    if (asset is null) continue;
                    foreach (var club in asset.param.Where(club => !club.Name.IsNullOrEmpty()))
                    {
                        var localization = string.Empty;

                        if (clubInfos.TryGetValue(club.ID, out var clubParam))
                        {
                            localization = clubParam.Name;
                        }

                        AddLocalizationToResults(results, club.Name, localization);
                        AddLocalizationToResults(Plugin.TextResourceHelper.GlobalMappings, club.Name, localization);
                    }
                }
            }

            return results;
        }

        private Dictionary<string, string> CollectScenarioCharsLocalizations()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;

            var propInfo = AccessTools.Property(typeof(Localize.Translate.Manager), "ScenarioReplaceNameData");


            if (!(propInfo?.GetValue(null, new object[0]) is Dictionary<string, List<ScenarioCharaName.Param>>
                scenarioReplaceNameData))
            {
                return results;
            }

            foreach (var name in scenarioReplaceNameData.Values.SelectMany(nameList => nameList))
            {
                AddLocalizationToResults(results, name.Target, name.Replace);
                if (SpeakerLocalizations != null)
                {
                    AddLocalizationToResults(SpeakerLocalizations, name.Target, name.Replace);
                }

                AddLocalizationToResults(Plugin.TextResourceHelper.GlobalMappings, name.Target, name.Replace);
            }

            return results;
        }
    }
}
