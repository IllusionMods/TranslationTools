using AIChara;
using AIProject;
using HarmonyLib;
using Illusion.Extensions;
using IllusionUtility.GetUtility;
using Localize.Translate;
using System;
using System.Collections.Generic;
using MyLocalize;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEx;

namespace IllusionMods
{
    public partial class AI_Int_LocalizationDumpHelper : LocalizationDumpHelper
    {
        public AI_Int_LocalizationDumpHelper(TextDump plugin) : base(plugin) { }

        public override string LocalizationFileRemap(string outputFile)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(outputFile);
            if (fileName.StartsWith("p_ai_tutorial"))
            {
                string mapName = tutorialCategoryMap.Where((m) => m.Value.Contains(fileName)).Select((m) => m.Key).FirstOrDefault() ?? "xx_Unknown";

                return $"Tutorials/{mapName}/{fileName}.txt";
            }

            if (outputFile.StartsWith(@"GameInfoTables\HousingItems") && int.TryParse(fileName, out int categoryId))
            {
                var catNameLocalization = string.Empty;
                Singleton<Manager.Resources>.Instance.Localize.ConvertTranslateHousingCategory(categoryId, ref catNameLocalization);
                if (!string.IsNullOrEmpty(catNameLocalization))
                {
                    return CombinePaths(System.IO.Path.GetDirectoryName(outputFile), $"{catNameLocalization}.txt");
                }
            }
            return base.LocalizationFileRemap(outputFile);
        }

        private IEnumerable<TranslationDumper> GetUILocalizers(DefinePack definePack, string topLevelName)
        {
            //Logger.LogWarning($"GetUILocalizers({topLevelName})");
            HashSet<string> finished = new HashSet<string>();
            var props = definePack.ABDirectories.GetType().GetProperties().Where((p) => p.PropertyType == typeof(string) && p.Name == "PopupInfoList");
            foreach (var prop in props)
            {
                foreach (var redirectTable in GetRedirectTables((string)prop.GetValue(definePack.ABDirectories)))
                {
                    //Logger.LogWarning($"RedirectTable: name={redirectTable.name}, assetbundle={redirectTable.assetbundle}, asset={redirectTable.asset}, manifest={redirectTable.manifest}");
                    foreach (var assetTable in GetAssetTables(redirectTable))
                    {
                        //Logger.LogWarning($"AssetTable: name={assetTable.name}, assetbundle={assetTable.assetbundle}, asset={assetTable.asset}, manifest={assetTable.manifest}");
                        var objects = GetAssetTableObjects(assetTable);
                        var assetNameforPath = System.IO.Path.GetFileNameWithoutExtension(assetTable.asset);
                        foreach (var gameObject in objects)
                        {
                            HashSet<object> handled = new HashSet<object>();
                            var outputName = $"UI/{topLevelName}/{assetNameforPath}/{gameObject.name}";
                            if (finished.Contains(outputName)) continue;
                            finished.Add(outputName);
                            var textList = EnumerateTexts(gameObject, handled).Select((t) => t.Value).ToArray();
                            var before = textList.Select((t) => t.text).ToArray();

                            var binder = gameObject.Get<UIBinder>();
                            if (binder)
                            {
                                var binderLoad = Traverse.Create(binder).Method("Load");
                                if (binderLoad.MethodExists())
                                {
                                    binderLoad.GetValue();
                                }
                            }

                            Dictionary<string, string> localizer()
                            {
                                Dictionary<string, string> results = new Dictionary<string, string>();
                                var after = textList.Select((t) => t.text).ToArray();
                                for (int i = 1; i < before.Length; i++)
                                {
                                    AddLocalizationToResults(results, before[i], after[i]);
                                }
                                return results;
                            }

                            yield return new TranslationDumper(outputName, localizer);
                        }
                    }
                }
            }
        }

        private IEnumerable<TranslationDumper> LocalizationMappingLocalizers()
        {
            foreach (var path in new string[] { "characustom", "downloader", "entryhandlename", "networkcheck", "uploader" })
            {
                var assetBundleNames = CommonLib.GetAssetBundleNameListFromPath($"localize/{path}");
                var assetBundleNameJP = assetBundleNames.FirstOrDefault((x) => x.EndsWith("jp.unity3d"));
                if (assetBundleNameJP is null)
                {
                    continue;
                }

                var assetBundleNameUS = assetBundleNames.FirstOrDefault((x) => x.EndsWith("us.unity3d"));

                string[] assetNamesJP = AssetBundleCheck.GetAllAssetName(assetBundleNameJP);

                foreach (var tmp in assetNamesJP)
                {
                    var assetNameJP = tmp;
                    Dictionary<string, string> localizer()
                    {
                        Dictionary<string, string> results = new Dictionary<string, string>();
                        var assetJP = ManualLoadAsset<TextInfo>(assetBundleNameJP, assetNameJP, "abdata");
                        if (assetJP != null)
                        {
                            var assetUS = ManualLoadAsset<TextInfo>(assetBundleNameUS, assetNameJP, "abdata");

                            var entriesJP = assetJP.lstInfo;

                            var entriesUS = assetUS != null ? assetUS.lstInfo : null;

                            for (int i = 0; i < entriesJP.Count; i++)
                            {
                                var entryJP = entriesJP[i];
                                var entryUS = entriesUS?.Find((e) => e.textId == entryJP.textId);
                                AddLocalizationToResults(results, entryJP.str, entryUS.str);
                                AddLocalizationToResults(ResourceHelper.GlobalMappings, entryJP.str, entryUS.str);
                                if (entryJP.str.EndsWith("EXP"))
                                {
                                    var altKey = entryJP.str.Substring(0, entryJP.str.Length - 2) + "xp";
                                    AddLocalizationToResults(results, altKey, entryUS.str);
                                    AddLocalizationToResults(ResourceHelper.GlobalMappings, altKey, entryUS.str);
                                }
                                else if (entryJP.str.EndsWith("Exp"))
                                {
                                    var altKey = entryJP.str.Substring(0, entryJP.str.Length - 2) + "XP";
                                    AddLocalizationToResults(results, altKey, entryUS.str);
                                    AddLocalizationToResults(ResourceHelper.GlobalMappings, altKey, entryUS.str);
                                }
                            }
                        }
                        return results;
                    }
                    yield return new TranslationDumper($"Mapping/{path}", localizer);
                }
            }
        }

        private Dictionary<string, string> TutorialTitleLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var title in Singleton<Manager.Resources>.Instance.Localize.TutorialTitleTable)
            {
                AddLocalizationToResults(results, title.Value[0], title.Value[1]);
            }
            return results;
        }

        private readonly Dictionary<string, HashSet<string>> tutorialCategoryMap = new Dictionary<string, HashSet<string>>();
        private IEnumerable<TranslationDumper> GetTutorialPrefabLocalizers()
        {
            Dictionary<string, string> nameLookup = new Dictionary<string, string>();
            foreach (var title in Singleton<Manager.Resources>.Instance.Localize.TutorialTitleTable)
            {
                nameLookup[title.Value[0]] = title.Value[1];
            }

            foreach (var level1 in Singleton<Manager.Resources>.Instance.PopupInfo.TutorialPrefabTable)
            {
                var entry = level1.Value;
                var categoryId = level1.Key;
                foreach (var gameObject in entry.Item2)
                {
                    HashSet<object> handled = new HashSet<object>();
                    var textList = EnumerateTexts(gameObject, handled).Select((t) => t.Value).ToArray();
                    var before = textList.Select((t) => t.text).ToArray();

                    var binder = gameObject.Get<UIBinder>();
                    if (binder)
                    {
                        var binderLoad = Traverse.Create(binder).Method("Load");
                        if (binderLoad.MethodExists())
                        {
                            binderLoad.GetValue();
                        }
                    }
                    if (!nameLookup.TryGetValue(entry.Item1, out string name))
                    {
                        name = entry.Item1;
                    }
                    Dictionary<string, string> localizer()
                    {
                        Dictionary<string, string> results = new Dictionary<string, string>();
                        AddLocalizationToResults(results, entry.Item1, name);
                        var after = textList.Select((t) => t.text).ToArray();
                        for (int i = 1; i < before.Length; i++)
                        {
                            AddLocalizationToResults(results, before[i], after[i]);
                        }
                        return results;
                    }

                    var mapName = $"{categoryId:00}_{name}".Replace("&", "and");
                    if (!tutorialCategoryMap.ContainsKey(mapName))
                    {
                        tutorialCategoryMap[mapName] = new HashSet<string>();
                    }
                    tutorialCategoryMap[mapName].Add(gameObject.name);
                    yield return new TranslationDumper(
                        $"Tutorials/{mapName}/{gameObject.name}", localizer);
                }
            }
        }
        private IEnumerable<TranslationDumper> MakeManagerResourceLocalizers()
        {
            foreach (var resource in ManagerResources)
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
                Dictionary<string, string> localizer()
                {
                    Dictionary<string, string> results = new Dictionary<string, string>();
                    foreach (var entry in resource.Value)
                    {
                        AddLocalizationToResults(results, entry.Value, getter(entry.Key));
                    }
                    return results;
                }

                yield return new TranslationDumper($"Manager/Resources/{resource.Key}", localizer);
            }
        }

        private IEnumerable<TranslationDumper> MakeCharacterCategoryLocalizers()
        {
            ChaListControl instance = Singleton<Manager.Character>.Instance.chaListCtrl;

            var categories = Enum.GetValues(typeof(ChaListDefine.CategoryNo)).Cast<ChaListDefine.CategoryNo>();
            foreach (var cat in categories)
            {
                var category = cat;
                Dictionary<string, string> localizer()
                {
                    Dictionary<string, string> results = new Dictionary<string, string>();
                    Dictionary<int, ListInfoBase> categoryInfo = instance.GetCategoryInfo(category);
                    foreach (var infoBase in categoryInfo.Values)
                    {
                        AddLocalizationToResults(results, infoBase.GetInfo(ChaListDefine.KeyType.Name), infoBase.Name);
                    }
                    return results;
                }

                yield return new TranslationDumper($"Character/Category/{category}", localizer);
            }
        }

        public override IEnumerable<TranslationDumper> GetInstanceLocalizers()
        {
            foreach (var localizer in base.GetInstanceLocalizers())
            {
                yield return localizer;
            }
            yield return MakeStandardInstanceLocalizer<AllAreaMapUI>("_islandNameTxt");
            yield return MakeStandardInstanceLocalizer<TitleLoadScene>("localizeIsLoad", "localizeIsGameStart", "localizeIsDelete");
            yield return MakeStandardInstanceLocalizer<AIProject.UI.FishingUI>("_fishingLabels", "_stopLabels", "_changeEsaLabels",
                "_moveLureLabels", "_backLabels", "_forceDirLabels");
            yield return MakeStandardInstanceLocalizer<AIProject.UI.JukeBoxAudioListUI>("_noneStrs");
            yield return MakeStandardInstanceLocalizer<AIProject.UI.PlantInfoUI>("_completeStrs");
            yield return MakeStandardInstanceLocalizer<AIProject.UI.PlayerLookEditUI>("_localizeMale", "_localizeFemale", "_localizeFutanari");
            yield return MakeStandardInstanceLocalizer<ConfigScene.ConfigWindow>("localizeIsInit", "localizeIsTitle");
            yield return MakeStandardInstanceLocalizer<GameLoadCharaFileSystem.GameLoadCharaListCtrl>("localizeMale", "localizeFemale");
            yield return MakeStandardInstanceLocalizer<HSceneSpriteAccessoryCondition>("_slotText");
        }
        public override IEnumerable<TranslationDumper> GetStaticLocalizers()
        {
            foreach (var localizer in base.GetStaticLocalizers())
            {
                yield return localizer;
            }
            yield return MakeStandardStaticLocalizer(typeof(ActionPoint), "_sleepErrorText", "_sickLabelTable");
            yield return MakeStandardStaticLocalizer(typeof(AgentActor), "_talkCommandLabel", "_attachCommandLabel", "_pickCommandLabel", "_talk2CommandLabel");

            yield return MapLabelPostProcessor(MakeStandardStaticLocalizer(typeof(AllAreaCameraControler), "_playerName"));
            yield return MakeStandardStaticLocalizer(typeof(AIProject.Animal.FishTankPoint), "_strs");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.Animal.GroundInsect), "_catchStrs");
            yield return MakeStandardStaticLocalizer(typeof(BasePoint), "_labelName", "_notify");
            yield return MakeStandardStaticLocalizer(typeof(CommandArea), "_fishingText");
            yield return MakeStandardStaticLocalizer(typeof(CraftPoint), "_medicLabel", "_petLabel", "_recyclingLabel");
            yield return MakeStandardStaticLocalizer(typeof(DevicePoint), "_label");
            yield return MakeStandardStaticLocalizer(typeof(FarmPoint), "_farmLabel", "_chickenLabel");

            yield return MakeStandardStaticLocalizer(typeof(MapUIContainer), "_getItemStrs", "_getEmptyStrs");
            yield return MakeStandardStaticLocalizer(typeof(MerchantActor), "_nameStrs", "_label");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.Player.Lie), "_backStrs");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.Player.Onbu), "_label", "_warningMessage");

            yield return MakeStandardStaticLocalizer(typeof(SearchActionPoint), "_lockText", "_pouchFullText", "_eqLowText");

            yield return MakeStandardStaticLocalizer(typeof(ShipPoint), "_label");

            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.HomeMenu), "_saveStrs");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.PetHomeUI), "_defaultName");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.PhotoShotUI), "_moveStrs", "_zoomStrs", "_takeStrs", "_endStrs");

            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.RecipeItemTitleNodeUI), "_recipeText");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.StatusUI), "_maleStrs", "_femaleStrs", "_futanariStrs");

            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.HomeMenu), "_saveStrs");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.PetHomeUI), "_defaultName");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.PhotoShotUI), "_moveStrs", "_zoomStrs", "_takeStrs");

            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.RecipeItemTitleNodeUI), "_recipeText");
            yield return MakeStandardStaticLocalizer(typeof(AIProject.UI.StatusUI), "_maleStrs", "_femaleStrs", "_funatariStrs");
            yield return MakeStandardStaticLocalizer(typeof(WarpPoint), "_errorText");

            yield return MakeStandardStaticLocalizer(typeof(CharaCustom.CharaCustomDefine), "CustomCorrectTitle", "CustomColorTitle",
                "CustomCapSave", "CustomCapUpdate", "CustomNoneStr", "ColorPresetNewMessage", "CustomHandBaseMsg");

            yield return MakeStandardStaticLocalizer(typeof(Housing.InfoUICtrl), "_campStr", "_fieldStr");
            yield return MakeStandardStaticLocalizer(typeof(Housing.ListUICtrl), "_itemFullMessage");
            yield return MakeStandardStaticLocalizer(typeof(Housing.OIFolder), "nameStrs");
            yield return MakeStandardStaticLocalizer(typeof(Housing.SettingUICtrl), "_itemSettingLabel", "_folderSettingLabel");
            yield return MakeStandardStaticLocalizer(typeof(Housing.SystemUICtrl), "warningMessage", "_saveMessage");

            yield return MakeStandardStaticLocalizer(typeof(HSceneSprite), "HelpText", "_motionLabelText", "_cumOutBodyText", "_cumOutsideText");

            yield return MakeStandardStaticLocalizer(typeof(Manager.Resources.LocalizeData), "_cancelLabel", "_standupLabel", "_sleepAgentMessage", "_collapseMessage", "_callMessage", "_extendSlotMessage");

            yield return MakeStandardStaticLocalizer(typeof(UploaderSystem.NetworkDefine), "msgPressAnyKey", "msgServerCheck", "msgServerAccessInfoField",
                "msgServerAccessField", "msgUpCannotBeIdentified", "msgUpAlreadyUploaded", "msgUpCompressionHousing", "msgUpStartUploadHousing", "msgDownDeleteData",
                "msgDownDeleteCache", "msgDownUnknown", "msgDownDownloadData", "msgDownDownloaded", "msgDownFailed", "msgDownLikes", "msgDownFailedGetThumbnail",
                "msgDownNotUploadDataFound", "msgDownDecompressingFile", "msgDownFailedDecompressingFile", "msgDownConfirmDelete", "msgDownFailedDelete",
                "msgNetGetInfoFromServer", "msgNetGetVersion", "msgNetConfirmUser", "msgNetStartEntryHN", "msgNetGetAllHN", "msgNetGetAllCharaInfo",
                "msgNetGetAllHousingInfo", "msgNetReady", "msgNetNotReady", "msgNetFailedGetCharaInfo", "msgNetFailedGetHousingInfo", "msgNetReadyGetData",
                "msgNetFailedGetVersion", "msgNetFailedConfirmUser", "msgNetFailedUpdateHN", "msgNetUpdatedHN", "msgNetFailedGetAllHN");
        }

        private IEnumerable<TranslationDumper> GetPopupLocalizers()
        {
            yield return new TranslationDumper("Popups/Warning", MakePopupLocalizer(
                Singleton<Manager.Resources>.Instance.PopupInfo.WarningTable,
                (x) => new List<string[]> { x }));

            yield return new TranslationDumper("Popups/StorySupport", MakePopupLocalizer(
                Singleton<Manager.Resources>.Instance.PopupInfo.StorySupportTable,
                (x) => new List<string[]> { x }));

            yield return new TranslationDumper("Popups/Request", MakePopupLocalizer(
                Singleton<Manager.Resources>.Instance.PopupInfo.RequestTable,
                (x) =>
                {
                    List<string[]> items = new List<string[]>();
                    if (x != null)
                    {
                        items.Add(x.Title);
                        items.Add(x.Message);
                    }
                    return items;
                }));
        }

        private Dictionary<string, string> DateActionNameLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach (var level1 in Singleton<Manager.Resources>.Instance.Map.PlayerDateActionPointInfoTable)
            {
                foreach (var level2 in level1.Value)
                {
                    foreach (var level3 in level2.Value)
                    {
                        for (int i = 0; i < level3.Value.Count; i++)
                        {
                            var dateActionPointInfo = level3.Value[i];
                            var localization = Singleton<Manager.Resources>.Instance.Localize.GetDateActionName(dateActionPointInfo.pointID, dateActionPointInfo.eventID) ?? string.Empty;
                            AddLocalizationToResults(results, dateActionPointInfo.actionName, localization);
                        }
                    }
                }
            }

            foreach (var agentLevel1 in Singleton<Manager.Resources>.Instance.Map.AgentDateActionPointInfoTable)
            {
                foreach (var level3 in agentLevel1.Value)
                {
                    for (int i = 0; i < level3.Value.Count; i++)
                    {
                        var dateActionPointInfo = level3.Value[i];
                        var localization = Singleton<Manager.Resources>.Instance.Localize.GetDateActionName(dateActionPointInfo.pointID, dateActionPointInfo.eventID) ?? string.Empty;
                        AddLocalizationToResults(results, dateActionPointInfo.actionName, localization);
                    }
                }
            }
            return results;
        }

        private Dictionary<string, string> SickNameLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach (var sickness in AIProject.Definitions.Sickness.NameTable)
            {
                AddLocalizationToResults(results, sickness.Value, Singleton<Manager.Resources>.Instance.Localize.GetSickName(sickness.Key));
            }
            return results;
        }

        private Dictionary<string, string> MapNameLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach (var map in Singleton<Manager.Resources>.Instance.Map.MapList)
            {
                AddLocalizationToResults(results, map.Value.name, Singleton<Manager.Resources>.Instance.Localize.GetMapName(map.Key));
            }
            return results;
        }

        private IEnumerable<TranslationDumper> GetRecipeLocalizers()
        {
            yield return new TranslationDumper(
                $"GameInfoTables/Recipie/{nameof(Singleton<Manager.Resources>.Instance.GameInfo.recipe.cookTable)}",
                MakeRecipeLocalizer(Singleton<Manager.Resources>.Instance.GameInfo.recipe.cookTable));

            yield return new TranslationDumper(
                $"GameInfoTables/Recipie/{nameof(Singleton<Manager.Resources>.Instance.GameInfo.recipe.equipmentTable)}",
                MakeRecipeLocalizer(Singleton<Manager.Resources>.Instance.GameInfo.recipe.equipmentTable));

            yield return new TranslationDumper(
                 $"GameInfoTables/Recipie/{nameof(Singleton<Manager.Resources>.Instance.GameInfo.recipe.materialTable)}",
                 MakeRecipeLocalizer(Singleton<Manager.Resources>.Instance.GameInfo.recipe.materialTable));

            yield return new TranslationDumper(
                 $"GameInfoTables/Recipie/{nameof(Singleton<Manager.Resources>.Instance.GameInfo.recipe.medicineTable)}",
                 MakeRecipeLocalizer(Singleton<Manager.Resources>.Instance.GameInfo.recipe.medicineTable));

            yield return new TranslationDumper(
                 $"GameInfoTables/Recipie/{nameof(Singleton<Manager.Resources>.Instance.GameInfo.recipe.petTable)}",
                 MakeRecipeLocalizer(Singleton<Manager.Resources>.Instance.GameInfo.recipe.petTable));
        }
        private IEnumerable<TranslationDumper> GetOtherDataLocalizers()
        {
            var categories = Enum.GetValues(typeof(Localize.Translate.Manager.SCENE_ID)).Cast<Localize.Translate.Manager.SCENE_ID>();

            foreach (var cat in categories)
            {
                var category = cat;
                Dictionary<string, string> localizer()
                {
                    Dictionary<int, HashSet<string>> tagsSeen = new Dictionary<int, HashSet<string>>();
                    Dictionary<string, string> results = new Dictionary<string, string>();
                    var otherData = Localize.Translate.Manager.LoadScene(category, null);
                    foreach (var dataset in OtherDataByTag)
                    {
                        tagsSeen[dataset.Key] = new HashSet<string>();
                        foreach (var entry in dataset.Value)
                        {
                            if (otherData.ContainsKey(dataset.Key))
                            {
                                var localization = otherData.Get(dataset.Key).Values.FindTagText(entry.Key);
                                AddLocalizationToResults(results, entry.Value, localization);
                                if (!string.IsNullOrEmpty(localization))
                                {
                                    tagsSeen[dataset.Key].Add(BuildSeenKey(dataset.Key, entry.Key));
                                }
                            }
                        }
                    }

                    foreach (var dataSet in otherData)
                    {
                        HashSet<string> seen = tagsSeen.ContainsKey(dataSet.Key) ? tagsSeen[dataSet.Key] : new HashSet<string>();
                        foreach (var entry in dataSet.Value)
                        {
                            var param = entry.Value;
                            var key = BuildSeenKey(dataSet.Key, param);
                            if (!seen.Contains(key) && !string.IsNullOrEmpty(param.text))
                            {
                                AddLocalizationToResults(results, $"//__NOTFOUND__{key}", param.text);
                            }
                        }
                    }
                    return results;
                }
                yield return new TranslationDumper($"OtherData/{category}", localizer);
            }
        }

        private IEnumerable<TranslationDumper> GetHousingItemLocalizers()
        {
            foreach (var level1 in Singleton<Manager.Housing>.Instance.dicCategoryInfo)
            {
                var categoryId = level1.Key;
                var categoryInfo = level1.Value;
                string catNameLocalization = string.Empty;
                Singleton<Manager.Resources>.Instance.Localize.ConvertTranslateHousingCategory(categoryId, ref catNameLocalization);

                Dictionary<string, string> localizer()
                {
                    Dictionary<string, string> results = new Dictionary<string, string>();

                    AddLocalizationToResults(results, categoryInfo.name, catNameLocalization);

                    var fileInfos = Singleton<Manager.Housing>.Instance.dicLoadInfo.Where((v) => v.Value.category == categoryId)
                        .Select((v) => new Housing.AddUICtrl.FileInfo()
                        {
                            no = v.Key,
                            loadInfo = v.Value
                        });

                    foreach (var fileInfo in fileInfos)
                    {
                        string text = string.Empty;
                        string name = string.Empty;
                        Singleton<Manager.Resources>.Instance.Localize.ConvertTranslateHousingItem(fileInfo.loadInfo.category, fileInfo.no, ref name, ref text);
                        AddLocalizationToResults(results, fileInfo.loadInfo.name, name);
                        AddLocalizationToResults(results, fileInfo.loadInfo.text, text);
                    }
                    return results;
                }
                var fileName = string.IsNullOrEmpty(catNameLocalization) ? $"{categoryId}" : catNameLocalization;
                yield return new TranslationDumper($"GameInfoTables/HousingItems/{fileName}", localizer);
            }
        }
        private IEnumerable<TranslationDumper> GetItemLocalizers()
        {
            foreach (int id in Singleton<Manager.Resources>.Instance.GameInfo.GetItemCategories())
            {
                var categoryId = id;
                string catNameLocalization = string.Empty;
                Singleton<Manager.Resources>.Instance.Localize.ConvertTranslateCategory(categoryId, ref catNameLocalization);

                Dictionary<string, string> localizer()
                {
                    Dictionary<string, string> results = new Dictionary<string, string>();
                    var catIcon = Singleton<Manager.Resources>.Instance.itemIconTables.CategoryIcon[categoryId];
                    AddLocalizationToResults(results, catIcon.Item1, catNameLocalization);
                    foreach (var itemTableEntry in Singleton<Manager.Resources>.Instance.GameInfo.GetItemTable(categoryId))
                    {
                        var stuffItemInfo = itemTableEntry.Value;
                        string name = string.Empty;
                        string explanation = string.Empty;

                        Singleton<Manager.Resources>.Instance.Localize.ConvertTranslateItem(stuffItemInfo.CategoryID, stuffItemInfo.ID, ref name, ref explanation);

                        AddLocalizationToResults(results, stuffItemInfo.Name, name);
                        AddLocalizationToResults(results, stuffItemInfo.Explanation, explanation);
                    }
                    return results;
                }
                var fileName = string.IsNullOrEmpty(catNameLocalization) ? $"{categoryId}" : catNameLocalization;
                yield return new TranslationDumper($"GameInfoTables/Items/{fileName}", localizer);
            }
        }

        private Dictionary<string, string> AgentLifeStyleLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var entry in Singleton<Manager.Resources>.Instance.GameInfo.AgentLifeStyleInfoTable)
            {
                var info = entry.Value;

                AddLocalizationToResults(results, info.Name, info.NameEnUS);
                AddLocalizationToResults(results, info.Explanation, info.ExplanationEnUS);
            }
            return results;
        }

        private Dictionary<string, string> PersonalityLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var voiceInfo in Singleton<Manager.Voice>.Instance.voiceInfoList)
            {
                AddLocalizationToResults(results, voiceInfo.Personality, voiceInfo.Get(Localize.Translate.Manager.Language));
            }
            return results;
        }

        private Dictionary<string, string> FallbackPersonalityLinesLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            var subdirs = new string[] { "2", "2_0" };
            foreach (var voiceInfo in Singleton<Manager.Voice>.Instance.voiceInfoList)
            {
                for (var i = 0; i < 3; i++)
                {
                    foreach (var subdir1 in subdirs)
                    {
                        var fileKey = CombinePaths(TextDump.AssetsRoot,
                            "adv", "scenario", $"c{voiceInfo.No:00}", "00", $"{i:00}", subdir1, "translation.txt");

                        if (TextDump.TranslationsDict.TryGetValue(fileKey, out var dict))
                        {
                            foreach (var entry in dict.Where((e) => e.Key.Contains("{0}") && !string.IsNullOrEmpty(e.Value)))
                            {
                                if (entry.Key.Contains("{0}が"))
                                {
                                    var key = new StringBuilder(entry.Key.Length * 3);
                                    bool hit = false;
                                    foreach (var c in entry.Key)
                                    {
                                        if (c == 'が')
                                        {
                                            hit = true;
                                        }
                                        key.Append(c);
                                        if (hit)
                                        {
                                            key.Append(@"\n?");
                                        }
                                    }
                                    //sr: "^\[(?<stat>[\w]+)(?<num_i>[\+\-]{1}[0-9]+)?\](?<after>[\s\S]+)?$" = "[${stat}${num_i}]${after}"
                                    key = key.Replace("{0}", @"^(?<color_open_i><color[^>]+>)(?<item_name>[\S\s]+)x(?<item_count_i>[1-9][0-9]*)(?<color_close_i><\/color>)$");
                                    key.Insert(0, "sr:\"");
                                    key.Append("\"");
                                    var value = entry.Value.Replace("{0}", "${color_open_i}${item_name} x${item_count_i}${color_close_i}");
                                    AddLocalizationToResults(results, key.ToString(), value);
                                }
                                else
                                {
                                    AddLocalizationToResults(results, entry.Key, entry.Value);
                                }
                            }
                        }
                    }
                }
            }
            return results;
        }

        private Dictionary<string, string> ActionNameLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach (var level1 in Singleton<Manager.Resources>.Instance.Map.AgentActionPointInfoTable)
            {
                foreach (var level2 in level1.Value)
                {
                    for (int i = 0; i < level2.Value.Count; i++)
                    {
                        var actionPointInfo = level2.Value[i];
                        AddLocalizationToResults(results, actionPointInfo.actionName, Singleton<Manager.Resources>.Instance.Localize.GetActionName(actionPointInfo.pointID, actionPointInfo.eventID));
                    }
                }
            }

            foreach (var playerLevel1 in Singleton<Manager.Resources>.Instance.Map.PlayerActionPointInfoTable)
            {
                foreach (var level3 in playerLevel1.Value)
                {
                    for (int i = 0; i < level3.Value.Count; i++)
                    {
                        var actionPointInfo = level3.Value[i];
                        var localization = Singleton<Manager.Resources>.Instance.Localize.GetActionName(actionPointInfo.pointID, actionPointInfo.eventID) ?? string.Empty;
                        AddLocalizationToResults(results, actionPointInfo.actionName, localization);
                    }
                }
            }

            return results;
        }

        private Dictionary<string, string> BaseNameLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach (var basepoint in Singleton<Manager.Resources>.Instance.itemIconTables.BaseName)
            {
                AddLocalizationToResults(results, basepoint.Value, Singleton<Manager.Resources>.Instance.Localize.GetBaseName(basepoint.Key));
            }

            return results;
        }

        private Dictionary<string, string> MiniMapIconNameLocalizer()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach (var icon in Singleton<Manager.Resources>.Instance.itemIconTables.MiniMapIconName)
            {
                AddLocalizationToResults(results, icon.Value, Singleton<Manager.Resources>.Instance.Localize.GetMinimapIcon(icon.Key));
            }

            return results;
        }

        private IEnumerable<TranslationDumper> GetHAnimationLocalizers()
        {
            var hSceneTable = Traverse.Create(Singleton<Manager.Resources>.Instance.HSceneTable);
            var assetNames = hSceneTable.Field("assetNames").GetValue<string[]>();

            var animListArray = hSceneTable.Field("lstAnimInfo").GetValue<List<HScene.AnimationListInfo>[]>();

            for (int i = 0; i < animListArray.Length; i++)
            {
                var animList = animListArray[i];
                var animListName = assetNames[i];
                var category = i;
                Dictionary<string, string> localizer()
                {
                    Dictionary<string, string> results = new Dictionary<string, string>();
                    foreach (var info in animList)
                    {
                        var hname = Singleton<Manager.Resources>.Instance.Localize.GetHName(category, info.id);
                        AddLocalizationToResults(results, info.nameAnimation, hname);
                    }
                    return results;
                }
                yield return new TranslationDumper($"Manager/Resources/HName/{animListName}", localizer);
            }
        }
        protected override IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            foreach (var generator in base.GetLocalizationGenerators())
            {
                yield return generator;
            }

            bool readyToDump = TextDump.IsReadyToDump();
            foreach (var dir in new string[] { "scene/common", "housing/base", "h/scene", "prefabs/tutorial_ui", "scene/map", "title/scene" })
            {
                yield return () => GetBindLocalizers(dir);
            }

            // this crashes, and doesn't find anything not covered already
            //foreach (var entry in GetUILocalizers(Singleton<Manager.Resources>.Instance.DefinePack, nameof(Singleton<Manager.Resources>.Instance.DefinePack)))
            //{
            //    yield return entry;
            //}

            yield return LocalizationMappingLocalizers;

            yield return WrapTranslationCollector("Tutorials/TutorialTitles", TutorialTitleLocalizer);
            yield return GetTutorialPrefabLocalizers;
            yield return GetStaticLocalizers;
            yield return GetPopupLocalizers;
            yield return GetOtherDataLocalizers;

            yield return GetHousingItemLocalizers;
            yield return GetItemLocalizers;

            yield return MakeManagerResourceLocalizers;
            yield return WrapTranslationCollector("Fallback/PersonalityLines", FallbackPersonalityLinesLocalizer);

            if (readyToDump)
            {
                yield return MakeCharacterCategoryLocalizers;
                yield return WrapTranslationCollector("Personalities", PersonalityLocalizer);
                yield return WrapTranslationCollector("GameInfoTables /AgentLifeStyle", AgentLifeStyleLocalizer);
                yield return GetRecipeLocalizers;

                yield return WrapTranslationCollector("Manager/Resources/DateActionName", DateActionNameLocalizer);

                yield return WrapTranslationCollector("Manager/Resources/MapName", MapNameLocalizer);
                yield return WrapTranslationCollector("Manager/Resources/ActionName", ActionNameLocalizer);
                yield return WrapTranslationCollector("Manager/Resources/SickName", SickNameLocalizer);
                yield return () => new TranslationDumper[] { MapLabelPostProcessor(new TranslationDumper("Manager/Resources/BaseName", BaseNameLocalizer)) };
                yield return WrapTranslationCollector("Manager/Resources/MiniMapIconName", MiniMapIconNameLocalizer);
                yield return GetHAnimationLocalizers;

                // add this one an extra time at the end
                yield return () => GetBindLocalizers("scene/map");
            }
        }
    }
}
