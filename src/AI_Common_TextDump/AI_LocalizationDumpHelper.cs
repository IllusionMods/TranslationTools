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
using Manager;
using UploaderSystem;
using static IllusionMods.TextResourceHelper.Helpers;
using Path = System.IO.Path;

namespace IllusionMods
{
    public partial class AI_LocalizationDumpHelper : AI_HS2_LocalizationDumpHelper
    {
        protected readonly Dictionary<string, HashSet<string>> TutorialCategoryMap =
            new Dictionary<string, HashSet<string>>();

        protected AI_LocalizationDumpHelper(TextDump plugin) : base(plugin) { }

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

            if (outputFile.StartsWith(@"GameInfoTables\HousingItems") && int.TryParse(fileName, out var categoryId))
            {
                var catNameLocalization = string.Empty;
#if LOCALIZE
                Singleton<Resources>.Instance.Localize.ConvertTranslateHousingCategory(categoryId,
                    ref catNameLocalization);
#endif
                if (!string.IsNullOrEmpty(catNameLocalization))
                {
                    return CombinePaths(Path.GetDirectoryName(outputFile), $"{catNameLocalization}.txt");
                }
            }

            return base.LocalizationFileRemap(outputFile);
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

        protected virtual  Dictionary<string, string> FallbackPersonalityLinesLocalizer()
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

        protected override IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            foreach (var generator in base.GetLocalizationGenerators())
            {
                yield return generator;
            }

            var readyToDump = TextDump.IsReadyForFinalDump();
            if (!readyToDump) yield break;
            foreach (var dir in new[]
                {"scene/common", "housing/base", "h/scene", "prefabs/tutorial_ui", "scene/map", "title/scene"})
            {
                yield return () => GetBindLocalizers(dir);
            }

            yield return GetStaticLocalizers;
            yield return GetPopupLocalizers;

            yield return GetHousingItemLocalizers;
            yield return GetItemLocalizers;

            yield return MakeManagerResourceLocalizers;
            yield return WrapTranslationCollector("Fallback/PersonalityLines", FallbackPersonalityLinesLocalizer);

            

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

        

        

        protected virtual IEnumerable<ITranslationDumper> MakeManagerResourceLocalizers()
        {
            foreach (var resource in _managerResources)
            {
#if LOCALIZE
                var localize = Singleton<Resources>.Instance.Localize;
                //Logger.LogWarning(localize);
                var func = AccessTools.Method(localize.GetType(), $"Get{resource.Key}");
                //Logger.LogWarning(func);

                if (func is null)
                {
                    continue;
                }

                var getter = (Func<int, string>) Delegate.CreateDelegate(typeof(Func<int, string>), localize, func);
#else
                var getter = new Func<int, string>((i) => string.Empty);
#endif
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

        protected virtual IEnumerable<ITranslationDumper> MakeCharacterCategoryLocalizers()
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

        protected virtual IEnumerable<ITranslationDumper> GetPopupLocalizers()
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

        protected virtual Dictionary<string, string> DateActionNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var level3 in from level1 in Singleton<Resources>.Instance.Map.PlayerDateActionPointInfoTable
                from level2 in level1.Value
                from level3 in level2.Value
                select level3)
            {
                foreach (var dateActionPointInfo in level3.Value)
                {
                    var localization = string.Empty;
#if LOCALIZE
                    localization = Singleton<Resources>.Instance.Localize.GetDateActionName(dateActionPointInfo.pointID,
                        dateActionPointInfo.eventID) ?? string.Empty;
#endif
                    AddLocalizationToResults(results, dateActionPointInfo.actionName, localization);
                }
            }

            foreach (var level3 in Singleton<Resources>.Instance.Map.AgentDateActionPointInfoTable.SelectMany(
                agentLevel1 => agentLevel1.Value))
            {
                foreach (var dateActionPointInfo in level3.Value)
                {
                    var localization = string.Empty;
#if LOCALIZE
                    localization = Singleton<Resources>.Instance.Localize.GetDateActionName(dateActionPointInfo.pointID,
                        dateActionPointInfo.eventID) ?? string.Empty;
#endif
                    AddLocalizationToResults(results, dateActionPointInfo.actionName, localization);
                }
            }

            return results;
        }

        protected virtual Dictionary<string, string> SickNameLocalizer()
        {
            var results = new Dictionary<string, string>();
            foreach (var sickness in Sickness.NameTable)
            {
                var localization = string.Empty;
#if LOCALIZE
                localization = Singleton<Resources>.Instance.Localize.GetSickName(sickness.Key);
#endif
                AddLocalizationToResults(results, sickness.Value, localization);
            }

            return results;
        }

        protected virtual Dictionary<string, string> MapNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var map in Singleton<Resources>.Instance.Map.MapList)
            {
                var localization = string.Empty;
#if LOCALIZE
                localization = Singleton<Resources>.Instance.Localize.GetMapName(map.Key);
#endif
                AddLocalizationToResults(results, map.Value.name, localization);
            }

            return results;
        }

        protected virtual IEnumerable<ITranslationDumper> GetRecipeLocalizers()
        {
            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipe/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.cookTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.cookTable));

            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipe/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.equipmentTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.equipmentTable));

            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipe/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.materialTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.materialTable));

            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipe/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.medicineTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.medicineTable));

            yield return new StringTranslationDumper(
                $"GameInfoTables/Recipe/{nameof(Singleton<Resources>.Instance.GameInfo.recipe.petTable)}",
                MakeRecipeLocalizer(Singleton<Resources>.Instance.GameInfo.recipe.petTable));
        }

        protected virtual IEnumerable<ITranslationDumper> GetHousingItemLocalizers()
        {
            foreach (var level1 in Singleton<Manager.Housing>.Instance.dicCategoryInfo)
            {
                var categoryId = level1.Key;
                var categoryInfo = level1.Value;
                var catNameLocalization = string.Empty;
#if LOCALIZE
                Singleton<Resources>.Instance.Localize.ConvertTranslateHousingCategory(categoryId,
                    ref catNameLocalization);
#endif
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
#if LOCALIZE
                        Singleton<Resources>.Instance.Localize.ConvertTranslateHousingItem(fileInfo.loadInfo.category,
                            fileInfo.no, ref name, ref text);
#endif
                        AddLocalizationToResults(results, fileInfo.loadInfo.name, name);
                        AddLocalizationToResults(results, fileInfo.loadInfo.text, text);
                    }

                    return results;
                }

                var fileName = string.IsNullOrEmpty(catNameLocalization) ? $"{categoryId}" : catNameLocalization;
                yield return new StringTranslationDumper($"GameInfoTables/HousingItems/{fileName}", Localizer);
            }
        }

        protected virtual IEnumerable<ITranslationDumper> GetItemLocalizers()
        {
            foreach (var id in Singleton<Resources>.Instance.GameInfo.GetItemCategories())
            {
                var categoryId = id;
                var catNameLocalization = string.Empty;
#if AI_INT
                Singleton<Resources>.Instance.Localize.ConvertTranslateCategory(categoryId, ref catNameLocalization);
#endif
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

#if LOCALIZE
                        Singleton<Resources>.Instance.Localize.ConvertTranslateItem(stuffItemInfo.CategoryID,
                            stuffItemInfo.ID, ref name, ref explanation);
#endif
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

        protected virtual Dictionary<string, string> AgentLifeStyleLocalizer()
        {
            var results = new Dictionary<string, string>();
            foreach (var entry in Singleton<Resources>.Instance.GameInfo.AgentLifeStyleInfoTable)
            {
                var info = entry.Value;

                var transName = string.Empty;
                var transExplanation = string.Empty;
#if LOCALIZE
                transName = info.NameEnUS;
                transExplanation = info.ExplanationEnUS;
#endif

                AddLocalizationToResults(results, info.Name, transName);
                AddLocalizationToResults(results, info.Explanation, transExplanation);
            }

            return results;
        }

        protected virtual Dictionary<string, string> ActionNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var level1 in Singleton<Resources>.Instance.Map.AgentActionPointInfoTable)
            {
                foreach (var level2 in level1.Value)
                {
                    for (var i = 0; i < level2.Value.Count; i++)
                    {
                        var actionPointInfo = level2.Value[i];
                        var localization = string.Empty;
#if LOCALIZE
                        localization = Singleton<Resources>.Instance.Localize.GetActionName(actionPointInfo.pointID,
                            actionPointInfo.eventID);
#endif
                        AddLocalizationToResults(results, actionPointInfo.actionName, localization);
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
                        var localization = string.Empty;
#if LOCALIZE
                        localization = Singleton<Resources>.Instance.Localize.GetActionName(actionPointInfo.pointID,
                            actionPointInfo.eventID) ?? string.Empty;
#endif
                        AddLocalizationToResults(results, actionPointInfo.actionName, localization);
                    }
                }
            }

            return results;
        }

        protected virtual Dictionary<string, string> BaseNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var basepoint in Singleton<Resources>.Instance.itemIconTables.BaseName)
            {
                var localization = string.Empty;
#if LOCALIZE
                localization = Singleton<Resources>.Instance.Localize.GetBaseName(basepoint.Key);
#endif
                AddLocalizationToResults(results, basepoint.Value, localization);
            }

            return results;
        }

        protected Dictionary<string, string> MiniMapIconNameLocalizer()
        {
            var results = new Dictionary<string, string>();

            foreach (var icon in Singleton<Resources>.Instance.itemIconTables.MiniMapIconName)
            {
                var localization = string.Empty;
#if LOCALIZE
                localization = Singleton<Resources>.Instance.Localize.GetMinimapIcon(icon.Key);
#endif
                AddLocalizationToResults(results, icon.Value, localization);
            }

            return results;
        }

        protected virtual IEnumerable<ITranslationDumper> GetHAnimationLocalizers()
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
                        var hname = string.Empty;
#if LOCALIZE
                        hname = Singleton<Resources>.Instance.Localize.GetHName(category, info.id);
#endif
                        AddLocalizationToResults(results, info.nameAnimation, hname);
                    }

                    return results;
                }

                yield return new StringTranslationDumper($"Manager/Resources/HName/{animListName}", Localizer);
            }
        }
    }
}
