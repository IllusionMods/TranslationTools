using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AIChara;
using AIProject;
using AIProject.Animal;
using AIProject.Definitions;
using AIProject.Player;
using AIProject.UI;
using CharaCustom;
using ConfigScene;
using GameLoadCharaFileSystem;
using HarmonyLib;
using Housing;
using Illusion.Extensions;
using IllusionUtility.GetUtility;
using Localize.Translate;
using Manager;
using MyLocalize;
using UploaderSystem;
using static IllusionMods.TextResourceHelper.Helpers;
using Path = System.IO.Path;

namespace IllusionMods
{
    public partial class AI_INT_LocalizationDumpHelper : AI_LocalizationDumpHelper
    {
        private readonly Dictionary<string, HashSet<string>> _tutorialCategoryMap =
            new Dictionary<string, HashSet<string>>();

        protected AI_INT_LocalizationDumpHelper(TextDump plugin) : base(plugin)
        {
            OtherDataByTag[100] = new Dictionary<string, string>
            {
                {"DeleteScene", "データを消去しますか？"},
                {
                    "DeleteWarning",
                    string.Concat("本当に削除しますか？\n", "このキャラにはパラメータが含まれています。".Coloring("#DE4529FF").Size(24))
                },
                {"Delete", "本当に削除しますか？"},
                {"EndHousing", "ハウジングを終了しますか？"},
                {"EndHScene", "Hシーンを終了しますか"},
                {
                    "LoadScene",
                    string.Concat("データを読込みますか？\n", "セットされたアイテムは削除されます。".Coloring("#DE4529FF").Size(24))
                },
                {"Migration", "{0}に移動しますか？"},
                {
                    "OverwriteWarn",
                    string.Concat("本当に上書きしますか？\n", "上書きするとパラメータは初期化されます。".Coloring("#DE4529FF").Size(24))
                },
                {"Overwrite", "本当に上書きしますか？"},
                {"ReleaseHousingItem", "作成しますか"},
                {
                    "RestoreScene",
                    string.Concat("初期化しますか？\n", "セットされたアイテムは削除されます。".Coloring("#DE4529FF").Size(24))
                },
                {"Save", "セーブしますか？"},
                {"SleepTogether", "一緒に寝た場合2人で行動状態が解除されます。"},
                {"Sleep", "一日を終了しますか？"},
                {"Teleport", "このポイントに移動しますか"},
                {"Warp", "移動しますか"},
                {
                    "ReleasePet",
                    string.Concat("{0}を逃しますか？\n", "逃がすとアイテムとして戻ってきません。".Coloring("#DE4529FF").Size(24))
                }
            };
        }


        public override string LocalizationFileRemap(string outputFile)
        {
            var fileName = Path.GetFileNameWithoutExtension(outputFile);
            if (fileName.StartsWith("p_ai_tutorial"))
            {
                var mapName =
                    _tutorialCategoryMap.Where(m => m.Value.Contains(fileName)).Select(m => m.Key).FirstOrDefault() ??
                    "xx_Unknown";

                return $"Tutorials/{mapName}/{fileName}.txt";
            }

            if (outputFile.StartsWith(@"GameInfoTables\HousingItems") && int.TryParse(fileName, out var categoryId))
            {
                var catNameLocalization = string.Empty;
                Singleton<Resources>.Instance.Localize.ConvertTranslateHousingCategory(categoryId,
                    ref catNameLocalization);
                if (!string.IsNullOrEmpty(catNameLocalization))
                {
                    return CombinePaths(Path.GetDirectoryName(outputFile), $"{catNameLocalization}.txt");
                }
            }

            return base.LocalizationFileRemap(outputFile);
        }

        private IEnumerable<ITranslationDumper> GetUILocalizers(DefinePack definePack, string topLevelName)
        {
            //Logger.LogWarning($"GetUILocalizers({topLevelName})");
            var finished = new HashSet<string>();
            var props = definePack.ABDirectories.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.Name == "PopupInfoList");
            foreach (var prop in props)
            {
                foreach (var redirectTable in GetRedirectTables((string) prop.GetValue(definePack.ABDirectories)))
                {
                    //Logger.LogWarning($"RedirectTable: name={redirectTable.name}, assetbundle={redirectTable.assetbundle}, asset={redirectTable.asset}, manifest={redirectTable.manifest}");
                    foreach (var assetTable in GetAssetTables(redirectTable))
                    {
                        //Logger.LogWarning($"AssetTable: name={assetTable.name}, assetbundle={assetTable.assetbundle}, asset={assetTable.asset}, manifest={assetTable.manifest}");
                        var objects = GetAssetTableObjects(assetTable);
                        var assetNameForPath = Path.GetFileNameWithoutExtension(assetTable.asset);
                        foreach (var gameObject in objects)
                        {
                            var handled = new HashSet<object>();
                            var outputName = $"UI/{topLevelName}/{assetNameForPath}/{gameObject.name}";
                            if (finished.Contains(outputName)) continue;
                            finished.Add(outputName);
                            var textList = EnumerateTexts(gameObject, handled).Select(t => t.Value).ToArray();
                            var before = textList.Select(t => t.text).ToArray();

                            var binder = gameObject.Get<UIBinder>();
                            if (binder)
                            {
                                var binderLoad = Traverse.Create(binder).Method("Load");
                                if (binderLoad.MethodExists())
                                {
                                    binderLoad.GetValue();
                                }
                            }

                            Dictionary<string, string> Localizer()
                            {
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
            }
        }

        private IEnumerable<ITranslationDumper> LocalizationMappingLocalizers()
        {
            foreach (var path in new[] {"characustom", "downloader", "entryhandlename", "networkcheck", "uploader"})
            {
                var assetBundleNames = GetAssetBundleNameListFromPath($"localize/{path}");
                var assetBundleNameJP = assetBundleNames.FirstOrDefault(x => x.EndsWith("jp.unity3d"));
                if (assetBundleNameJP is null)
                {
                    continue;
                }

                var assetBundleNameUS = assetBundleNames.FirstOrDefault(x => x.EndsWith("us.unity3d"));

                var assetNamesJp = GetAssetNamesFromBundle(assetBundleNameJP);

                foreach (var tmp in assetNamesJp)
                {
                    var assetNameJp = tmp;

                    Dictionary<string, string> Localizer()
                    {
                        var results = new Dictionary<string, string>();
                        var assetJP = ManualLoadAsset<TextInfo>(assetBundleNameJP, assetNameJp, "abdata");
                        if (assetJP != null)
                        {
                            var assetUS = ManualLoadAsset<TextInfo>(assetBundleNameUS, assetNameJp, "abdata");

                            var entriesJP = assetJP.lstInfo;

                            var entriesUS = assetUS != null ? assetUS.lstInfo : null;

                            for (var i = 0; i < entriesJP.Count; i++)
                            {
                                var entryJP = entriesJP[i];
                                var entryUS = entriesUS?.Find(e => e.textId == entryJP.textId);
                                AddLocalizationToResults(results, entryJP.str, entryUS?.str);
                                if (entryUS is null || entryUS.str.IsNullOrEmpty()) continue;
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

                    yield return new StringTranslationDumper($"Mapping/{path}", Localizer);
                }
            }
        }

        private Dictionary<string, string> TutorialTitleLocalizer()
        {
            var results = new Dictionary<string, string>();
            foreach (var title in Singleton<Resources>.Instance.Localize.TutorialTitleTable)
            {
                AddLocalizationToResults(results, title.Value[0], title.Value[1]);
            }

            return results;
        }

        private IEnumerable<ITranslationDumper> GetTutorialPrefabLocalizers()
        {
            var nameLookup = new Dictionary<string, string>();
            foreach (var title in Singleton<Resources>.Instance.Localize.TutorialTitleTable)
            {
                nameLookup[title.Value[0]] = title.Value[1];
            }

            foreach (var level1 in Singleton<Resources>.Instance.PopupInfo.TutorialPrefabTable)
            {
                var entry = level1.Value;
                var categoryId = level1.Key;
                foreach (var gameObject in entry.Item2)
                {
                    var handled = new HashSet<object>();
                    var textList = EnumerateTexts(gameObject, handled).Select(t => t.Value).ToArray();
                    var before = textList.Select(t => t.text).ToArray();

                    var binder = gameObject.Get<UIBinder>();
                    if (binder)
                    {
                        var binderLoad = Traverse.Create(binder).Method("Load");
                        if (binderLoad.MethodExists())
                        {
                            binderLoad.GetValue();
                        }
                    }

                    if (!nameLookup.TryGetValue(entry.Item1, out var name))
                    {
                        name = entry.Item1;
                    }

                    Dictionary<string, string> Localizer()
                    {
                        var results = new Dictionary<string, string>();
                        AddLocalizationToResults(results, entry.Item1, name);
                        var after = textList.Select(t => t.text).ToArray();
                        for (var i = 1; i < before.Length; i++)
                        {
                            AddLocalizationToResults(results, before[i], after[i]);
                        }

                        return results;
                    }

                    var mapName = $"{categoryId:00}_{name}".Replace("&", "and");
                    if (!_tutorialCategoryMap.ContainsKey(mapName))
                    {
                        _tutorialCategoryMap[mapName] = new HashSet<string>();
                    }

                    _tutorialCategoryMap[mapName].Add(gameObject.name);
                    yield return new StringTranslationDumper(
                        $"Tutorials/{mapName}/{gameObject.name}", Localizer);
                }
            }
        }

        private IEnumerable<ITranslationDumper> MakeManagerResourceLocalizers()
        {
            foreach (var resource in _managerResources)
            {
                var localize = Singleton<Resources>.Instance.Localize;
                //Logger.LogWarning(localize);
                var func = AccessTools.Method(localize.GetType(), $"Get{resource.Key}");
                //Logger.LogWarning(func);

                if (func is null)
                {
                    continue;
                }

                var getter = (Func<int, string>) Delegate.CreateDelegate(typeof(Func<int, string>), localize, func);

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

        private IEnumerable<ITranslationDumper> MakeCharacterCategoryLocalizers()
        {
            var instance = Singleton<Character>.Instance.chaListCtrl;

            var categories = Enum.GetValues(typeof(ChaListDefine.CategoryNo)).Cast<ChaListDefine.CategoryNo>();
            foreach (var cat in categories)
            {
                var category = cat;

                Dictionary<string, string> Localizer()
                {
                    var results = new Dictionary<string, string>();
                    var categoryInfo = instance.GetCategoryInfo(category);
                    foreach (var infoBase in categoryInfo.Values)
                    {
                        AddLocalizationToResults(results, infoBase.GetInfo(ChaListDefine.KeyType.Name), infoBase.Name);
                    }

                    return results;
                }

                yield return new StringTranslationDumper($"Character/Category/{category}", Localizer);
            }
        }

        public override IEnumerable<ITranslationDumper> GetInstanceLocalizers()
        {
            foreach (var localizer in base.GetInstanceLocalizers())
            {
                yield return localizer;
            }

            yield return MakeStandardInstanceLocalizer<AllAreaMapUI>("_islandNameTxt");
            yield return MakeStandardInstanceLocalizer<TitleLoadScene>("localizeIsLoad", "localizeIsGameStart",
                "localizeIsDelete");
            yield return MakeStandardInstanceLocalizer<FishingUI>("_fishingLabels", "_stopLabels", "_changeEsaLabels",
                "_moveLureLabels", "_backLabels", "_forceDirLabels");
            yield return MakeStandardInstanceLocalizer<JukeBoxAudioListUI>("_noneStrs");
            yield return MakeStandardInstanceLocalizer<PlantInfoUI>("_completeStrs");
            yield return MakeStandardInstanceLocalizer<PlayerLookEditUI>("_localizeMale", "_localizeFemale",
                "_localizeFutanari");
            yield return MakeStandardInstanceLocalizer<ConfigWindow>("localizeIsInit", "localizeIsTitle");
            yield return MakeStandardInstanceLocalizer<GameLoadCharaListCtrl>("localizeMale", "localizeFemale");
            yield return MakeStandardInstanceLocalizer<HSceneSpriteAccessoryCondition>("_slotText");
        }

        public override IEnumerable<ITranslationDumper> GetStaticLocalizers()
        {
            foreach (var localizer in base.GetStaticLocalizers())
            {
                yield return localizer;
            }

            yield return MakeStandardStaticLocalizer(typeof(ActionPoint), "_sleepErrorText", "_sickLabelTable");
            yield return MakeStandardStaticLocalizer(typeof(AgentActor), "_talkCommandLabel", "_attachCommandLabel",
                "_pickCommandLabel", "_talk2CommandLabel");

            yield return MapLabelPostProcessor(MakeStandardStaticLocalizer(typeof(AllAreaCameraControler),
                "_playerName"));
            yield return MakeStandardStaticLocalizer(typeof(FishTankPoint), "_strs");
            yield return MakeStandardStaticLocalizer(typeof(GroundInsect), "_catchStrs");
            yield return MakeStandardStaticLocalizer(typeof(BasePoint), "_labelName", "_notify");
            yield return MakeStandardStaticLocalizer(typeof(CommandArea), "_fishingText");
            yield return MakeStandardStaticLocalizer(typeof(CraftPoint), "_medicLabel", "_petLabel", "_recyclingLabel");
            yield return MakeStandardStaticLocalizer(typeof(DevicePoint), "_label");
            yield return MakeStandardStaticLocalizer(typeof(FarmPoint), "_farmLabel", "_chickenLabel");

            yield return MakeStandardStaticLocalizer(typeof(MapUIContainer), "_getItemStrs", "_getEmptyStrs");
            yield return MakeStandardStaticLocalizer(typeof(MerchantActor), "_nameStrs", "_label");
            yield return MakeStandardStaticLocalizer(typeof(Lie), "_backStrs");
            yield return MakeStandardStaticLocalizer(typeof(Onbu), "_label", "_warningMessage");

            yield return MakeStandardStaticLocalizer(typeof(SearchActionPoint), "_lockText", "_pouchFullText",
                "_eqLowText");

            yield return MakeStandardStaticLocalizer(typeof(ShipPoint), "_label");

            yield return MakeStandardStaticLocalizer(typeof(HomeMenu), "_saveStrs");
            yield return MakeStandardStaticLocalizer(typeof(PetHomeUI), "_defaultName");
            yield return MakeStandardStaticLocalizer(typeof(PhotoShotUI), "_moveStrs", "_zoomStrs", "_takeStrs",
                "_endStrs");

            yield return MakeStandardStaticLocalizer(typeof(RecipeItemTitleNodeUI), "_recipeText");
            yield return MakeStandardStaticLocalizer(typeof(StatusUI), "_maleStrs", "_femaleStrs", "_futanariStrs");

            yield return MakeStandardStaticLocalizer(typeof(HomeMenu), "_saveStrs");
            yield return MakeStandardStaticLocalizer(typeof(PetHomeUI), "_defaultName");
            yield return MakeStandardStaticLocalizer(typeof(PhotoShotUI), "_moveStrs", "_zoomStrs", "_takeStrs");

            yield return MakeStandardStaticLocalizer(typeof(RecipeItemTitleNodeUI), "_recipeText");
            yield return MakeStandardStaticLocalizer(typeof(StatusUI), "_maleStrs", "_femaleStrs", "_funatariStrs");
            yield return MakeStandardStaticLocalizer(typeof(WarpPoint), "_errorText");

            yield return MakeStandardStaticLocalizer(typeof(CharaCustomDefine), "CustomCorrectTitle",
                "CustomColorTitle",
                "CustomCapSave", "CustomCapUpdate", "CustomNoneStr", "ColorPresetNewMessage", "CustomHandBaseMsg");

            yield return MakeStandardStaticLocalizer(typeof(InfoUICtrl), "_campStr", "_fieldStr");
            yield return MakeStandardStaticLocalizer(typeof(ListUICtrl), "_itemFullMessage");
            yield return MakeStandardStaticLocalizer(typeof(OIFolder), "nameStrs");
            yield return MakeStandardStaticLocalizer(typeof(SettingUICtrl), "_itemSettingLabel", "_folderSettingLabel");
            yield return MakeStandardStaticLocalizer(typeof(SystemUICtrl), "warningMessage", "_saveMessage");

            yield return MakeStandardStaticLocalizer(typeof(HSceneSprite), "HelpText", "_motionLabelText",
                "_cumOutBodyText", "_cumOutsideText");

            yield return MakeStandardStaticLocalizer(typeof(Resources.LocalizeData), "_cancelLabel", "_standupLabel",
                "_sleepAgentMessage", "_collapseMessage", "_callMessage", "_extendSlotMessage");

            yield return MakeStandardStaticLocalizer(typeof(NetworkDefine), "msgPressAnyKey", "msgServerCheck",
                "msgServerAccessInfoField",
                "msgServerAccessField", "msgUpCannotBeIdentified", "msgUpAlreadyUploaded", "msgUpCompressionHousing",
                "msgUpStartUploadHousing", "msgDownDeleteData",
                "msgDownDeleteCache", "msgDownUnknown", "msgDownDownloadData", "msgDownDownloaded", "msgDownFailed",
                "msgDownLikes", "msgDownFailedGetThumbnail",
                "msgDownNotUploadDataFound", "msgDownDecompressingFile", "msgDownFailedDecompressingFile",
                "msgDownConfirmDelete", "msgDownFailedDelete",
                "msgNetGetInfoFromServer", "msgNetGetVersion", "msgNetConfirmUser", "msgNetStartEntryHN",
                "msgNetGetAllHN", "msgNetGetAllCharaInfo",
                "msgNetGetAllHousingInfo", "msgNetReady", "msgNetNotReady", "msgNetFailedGetCharaInfo",
                "msgNetFailedGetHousingInfo", "msgNetReadyGetData",
                "msgNetFailedGetVersion", "msgNetFailedConfirmUser", "msgNetFailedUpdateHN", "msgNetUpdatedHN",
                "msgNetFailedGetAllHN");
        }

        private IEnumerable<ITranslationDumper> GetPopupLocalizers()
        {
            yield return new StringTranslationDumper("Popups/Warning", MakePopupLocalizer(
                Singleton<Resources>.Instance.PopupInfo.WarningTable,
                x => new List<string[]> {x}));

            yield return new StringTranslationDumper("Popups/StorySupport", MakePopupLocalizer(
                Singleton<Resources>.Instance.PopupInfo.StorySupportTable,
                x => new List<string[]> {x}));

            yield return new StringTranslationDumper("Popups/Request", MakePopupLocalizer(
                Singleton<Resources>.Instance.PopupInfo.RequestTable,
                x =>
                {
                    var items = new List<string[]>();
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
            var results = new Dictionary<string, string>();

            foreach (var level3 in from level1 in Singleton<Resources>.Instance.Map.PlayerDateActionPointInfoTable
                from level2 in level1.Value
                from level3 in level2.Value
                select level3)
            {
                foreach (var dateActionPointInfo in level3.Value)
                {
                    var localization =
                        Singleton<Resources>.Instance.Localize.GetDateActionName(dateActionPointInfo.pointID,
                            dateActionPointInfo.eventID) ?? string.Empty;
                    AddLocalizationToResults(results, dateActionPointInfo.actionName, localization);
                }
            }

            foreach (var level3 in Singleton<Resources>.Instance.Map.AgentDateActionPointInfoTable.SelectMany(
                agentLevel1 => agentLevel1.Value))
            {
                foreach (var dateActionPointInfo in level3.Value)
                {
                    var localization =
                        Singleton<Resources>.Instance.Localize.GetDateActionName(dateActionPointInfo.pointID,
                            dateActionPointInfo.eventID) ?? string.Empty;
                    AddLocalizationToResults(results, dateActionPointInfo.actionName, localization);
                }
            }

            return results;
        }

        private Dictionary<string, string> SickNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var sickness in Sickness.NameTable)
            {
                AddLocalizationToResults(results, sickness.Value,
                    Singleton<Resources>.Instance.Localize.GetSickName(sickness.Key));
            }

            return results;
        }

        private Dictionary<string, string> MapNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var map in Singleton<Resources>.Instance.Map.MapList)
            {
                AddLocalizationToResults(results, map.Value.name,
                    Singleton<Resources>.Instance.Localize.GetMapName(map.Key));
            }

            return results;
        }

        private IEnumerable<ITranslationDumper> GetRecipeLocalizers()
        {
            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipie/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.cookTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.cookTable));

            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipie/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.equipmentTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.equipmentTable));

            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipie/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.materialTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.materialTable));

            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipie/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.medicineTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.medicineTable));

            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipie/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.petTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.petTable));
        }

        private IEnumerable<ITranslationDumper> GetHousingItemLocalizers()
        {
            foreach (var level1 in Singleton<Manager.Housing>.Instance.dicCategoryInfo)
            {
                var categoryId = level1.Key;
                var categoryInfo = level1.Value;
                var catNameLocalization = string.Empty;
                Singleton<Resources>.Instance.Localize.ConvertTranslateHousingCategory(categoryId,
                    ref catNameLocalization);

                Dictionary<string, string> Localizer()
                {
                    var results = new Dictionary<string, string>();

                    AddLocalizationToResults(results, categoryInfo.name, catNameLocalization);

                    var fileInfos = Singleton<Manager.Housing>.Instance.dicLoadInfo
                        .Where(v => v.Value.category == categoryId)
                        .Select(v => new AddUICtrl.FileInfo
                        {
                            no = v.Key,
                            loadInfo = v.Value
                        });

                    foreach (var fileInfo in fileInfos)
                    {
                        var text = string.Empty;
                        var name = string.Empty;
                        Singleton<Resources>.Instance.Localize.ConvertTranslateHousingItem(fileInfo.loadInfo.category,
                            fileInfo.no, ref name, ref text);
                        AddLocalizationToResults(results, fileInfo.loadInfo.name, name);
                        AddLocalizationToResults(results, fileInfo.loadInfo.text, text);
                    }

                    return results;
                }

                var fileName = string.IsNullOrEmpty(catNameLocalization) ? $"{categoryId}" : catNameLocalization;
                yield return new StringTranslationDumper($"GameInfoTables/HousingItems/{fileName}", Localizer);
            }
        }

        private IEnumerable<ITranslationDumper> GetItemLocalizers()
        {
            foreach (var id in Singleton<Resources>.Instance.GameInfo.GetItemCategories())
            {
                var categoryId = id;
                var catNameLocalization = string.Empty;
                Singleton<Resources>.Instance.Localize.ConvertTranslateCategory(categoryId, ref catNameLocalization);

                Dictionary<string, string> Localizer()
                {
                    var results = new Dictionary<string, string>();
                    var catIcon = Singleton<Resources>.Instance.itemIconTables.CategoryIcon[categoryId];
                    AddLocalizationToResults(results, catIcon.Item1, catNameLocalization);
                    foreach (var itemTableEntry in Singleton<Resources>.Instance.GameInfo.GetItemTable(categoryId))
                    {
                        var stuffItemInfo = itemTableEntry.Value;
                        var name = string.Empty;
                        var explanation = string.Empty;

                        Singleton<Resources>.Instance.Localize.ConvertTranslateItem(stuffItemInfo.CategoryID,
                            stuffItemInfo.ID, ref name, ref explanation);

                        AddLocalizationToResults(results, stuffItemInfo.Name, name);
                        AddLocalizationToResults(results, stuffItemInfo.Explanation, explanation);
                        AddLocalizationToResults(ResourceHelper.GlobalMappings, stuffItemInfo.Name, name);
                    }

                    return results;
                }

                var fileName = string.IsNullOrEmpty(catNameLocalization) ? $"{categoryId}" : catNameLocalization;
                yield return new StringTranslationDumper($"GameInfoTables/Items/{fileName}", Localizer);
            }
        }

        private Dictionary<string, string> AgentLifeStyleLocalizer()
        {
            var results = new Dictionary<string, string>();
            foreach (var entry in Singleton<Resources>.Instance.GameInfo.AgentLifeStyleInfoTable)
            {
                var info = entry.Value;

                AddLocalizationToResults(results, info.Name, info.NameEnUS);
                AddLocalizationToResults(results, info.Explanation, info.ExplanationEnUS);
            }

            return results;
        }

        protected override string GetPersonalityNameLocalization(VoiceInfo.Param voiceInfo)
        {
            return voiceInfo.Get(Localize.Translate.Manager.Language);
        }

        protected Dictionary<string, string> FallbackPersonalityLinesLocalizer()
        {
            var results = new Dictionary<string, string>();
            var subdirs = new[] {"2", "2_0"};
            foreach (var voiceInfo in Singleton<Voice>.Instance.voiceInfoList)
            {
                for (var i = 0; i < 3; i++)
                {
                    foreach (var subdir1 in subdirs)
                    {
                        var fileKey = CombinePaths(TextDump.AssetsRoot,
                            "adv", "scenario", $"c{voiceInfo.No:00}", "00", $"{i:00}", subdir1, "translation.txt");

                        if (TextDump.GetTranslationPaths().Contains(fileKey))
                        {
                            var dict = TextDump.GetTranslationsForPath(fileKey);
                            foreach (var entry in dict.Where(e =>
                                e.Key.Contains("{0}") && !string.IsNullOrEmpty(e.Value)))
                            {
                                if (entry.Key.Contains("{0}が"))
                                {
                                    var key = new StringBuilder(entry.Key.Length * 3);
                                    var hit = false;
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
                                    key = key.Replace("{0}",
                                        @"^(?<color_open_i><color[^>]+>)(?<item_name>[\S\s]+)x(?<item_count_i>[1-9][0-9]*)(?<color_close_i><\/color>)$");
                                    key.Insert(0, "sr:\"");
                                    key.Append("\"");
                                    var value = entry.Value.Replace("{0}",
                                        "${color_open_i}${item_name} x${item_count_i}${color_close_i}");
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
            var results = new Dictionary<string, string>();

            foreach (var level1 in Singleton<Resources>.Instance.Map.AgentActionPointInfoTable)
            {
                foreach (var level2 in level1.Value)
                {
                    for (var i = 0; i < level2.Value.Count; i++)
                    {
                        var actionPointInfo = level2.Value[i];
                        AddLocalizationToResults(results, actionPointInfo.actionName,
                            Singleton<Resources>.Instance.Localize.GetActionName(actionPointInfo.pointID,
                                actionPointInfo.eventID));
                    }
                }
            }

            foreach (var playerLevel1 in Singleton<Resources>.Instance.Map.PlayerActionPointInfoTable)
            {
                foreach (var level3 in playerLevel1.Value)
                {
                    for (var i = 0; i < level3.Value.Count; i++)
                    {
                        var actionPointInfo = level3.Value[i];
                        var localization =
                            Singleton<Resources>.Instance.Localize.GetActionName(actionPointInfo.pointID,
                                actionPointInfo.eventID) ?? string.Empty;
                        AddLocalizationToResults(results, actionPointInfo.actionName, localization);
                    }
                }
            }

            return results;
        }

        private Dictionary<string, string> BaseNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var basepoint in Singleton<Resources>.Instance.itemIconTables.BaseName)
            {
                AddLocalizationToResults(results, basepoint.Value,
                    Singleton<Resources>.Instance.Localize.GetBaseName(basepoint.Key));
            }

            return results;
        }

        private Dictionary<string, string> MiniMapIconNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var icon in Singleton<Resources>.Instance.itemIconTables.MiniMapIconName)
            {
                AddLocalizationToResults(results, icon.Value,
                    Singleton<Resources>.Instance.Localize.GetMinimapIcon(icon.Key));
            }

            return results;
        }

        private IEnumerable<ITranslationDumper> GetHAnimationLocalizers()
        {
            var hSceneTable = Traverse.Create(Singleton<Resources>.Instance.HSceneTable);
            var assetNames = hSceneTable.Field("assetNames").GetValue<string[]>();

            var animListArray = hSceneTable.Field("lstAnimInfo").GetValue<List<HScene.AnimationListInfo>[]>();

            for (var i = 0; i < animListArray.Length; i++)
            {
                var animList = animListArray[i];
                var animListName = assetNames[i];
                var category = i;

                Dictionary<string, string> Localizer()
                {
                    var results = new Dictionary<string, string>();
                    foreach (var info in animList)
                    {
                        var hname = Singleton<Resources>.Instance.Localize.GetHName(category, info.id);
                        AddLocalizationToResults(results, info.nameAnimation, hname);
                    }

                    return results;
                }

                yield return new StringTranslationDumper($"Manager/Resources/HName/{animListName}", Localizer);
            }
        }

        protected override IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            foreach (var generator in base.GetLocalizationGenerators())
            {
                yield return generator;
            }

            var readyToDump = TextDump.IsReadyForFinalDump();
            foreach (var dir in new[]
                {"scene/common", "housing/base", "h/scene", "prefabs/tutorial_ui", "scene/map", "title/scene"})
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

            yield return GetHousingItemLocalizers;
            yield return GetItemLocalizers;

            yield return MakeManagerResourceLocalizers;
            yield return WrapTranslationCollector("Fallback/PersonalityLines", FallbackPersonalityLinesLocalizer);

            if (!readyToDump) yield break;

            yield return MakeCharacterCategoryLocalizers;
            yield return WrapTranslationCollector("GameInfoTables/AgentLifeStyle", AgentLifeStyleLocalizer);
            yield return GetRecipeLocalizers;

            yield return WrapTranslationCollector("Manager/Resources/DateActionName", DateActionNameLocalizer);

            yield return WrapTranslationCollector("Manager/Resources/MapName", MapNameLocalizer);
            yield return WrapTranslationCollector("Manager/Resources/ActionName", ActionNameLocalizer);
            yield return WrapTranslationCollector("Manager/Resources/SickName", SickNameLocalizer);
            yield return () => new[]
                {MapLabelPostProcessor(new StringTranslationDumper("Manager/Resources/BaseName", BaseNameLocalizer))};
            yield return WrapTranslationCollector("Manager/Resources/MiniMapIconName", MiniMapIconNameLocalizer);
            yield return GetHAnimationLocalizers;

            // add this one an extra time at the end
            yield return () => GetBindLocalizers("scene/map");
        }
    }
}
