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


        protected override IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            foreach (var generator in base.GetLocalizationGenerators())
            {
                yield return generator;
            }
            var readyToDump = TextDump.IsReadyForFinalDump();

            yield return LocalizationMappingLocalizers;
            yield return WrapTranslationCollector("Tutorials/TutorialTitles", TutorialTitleLocalizer);
            yield return GetTutorialPrefabLocalizers;

            
            // this crashes, and doesn't find anything not covered already
            //foreach (var entry in GetUILocalizers(Singleton<Manager.Resources>.Instance.DefinePack, nameof(Singleton<Manager.Resources>.Instance.DefinePack)))
            //{
            //    yield return entry;
            //}
        }

        public override IEnumerable<ITranslationDumper> GetStaticLocalizers()
        {
            foreach (var localizer in base.GetStaticLocalizers())
            {
                yield return localizer;
            }

            yield return MakeStandardStaticLocalizer(typeof(Resources.LocalizeData), "_cancelLabel", "_standupLabel",
                "_sleepAgentMessage", "_collapseMessage", "_callMessage", "_extendSlotMessage");
        }

        protected override string GetPersonalityNameLocalization(VoiceInfo.Param voiceInfo)
        {
            return voiceInfo.Get(Localize.Translate.Manager.Language);
        }

        protected IEnumerable<ITranslationDumper> GetUILocalizers(DefinePack definePack, string topLevelName)
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


        protected virtual IEnumerable<ITranslationDumper> LocalizationMappingLocalizers()
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

        protected virtual Dictionary<string, string> TutorialTitleLocalizer()
        {
            var results = new Dictionary<string, string>();
            foreach (var title in Singleton<Resources>.Instance.Localize.TutorialTitleTable)
            {
                AddLocalizationToResults(results, title.Value[0], title.Value[1]);
            }

            return results;
        }

        protected virtual IEnumerable<ITranslationDumper> GetTutorialPrefabLocalizers()
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
                    if (!TutorialCategoryMap.ContainsKey(mapName))
                    {
                        TutorialCategoryMap[mapName] = new HashSet<string>();
                    }

                    TutorialCategoryMap[mapName].Add(gameObject.name);
                    yield return new StringTranslationDumper(
                        $"Tutorials/{mapName}/{gameObject.name}", Localizer);
                }
            }
        }

        // HERE
      
        
    }
}
