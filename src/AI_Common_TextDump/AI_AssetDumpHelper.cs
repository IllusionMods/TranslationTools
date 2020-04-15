using AIChara;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace IllusionMods
{
    public class AI_AssetDumpHelper : AssetDumpHelper
    {
        protected readonly AssetDumpColumnInfo titleAssetCols;

        protected readonly AssetDumpColumnInfo stdExcelAssetCols;
        protected readonly AssetDumpColumnInfo itemLookup;
        protected readonly AssetDumpColumnInfo itemLookupAndAssetCols;

        public AI_AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            titleAssetCols = new AssetDumpColumnInfo { CombineWithParentBundle = true };

            stdExcelAssetCols = new AssetDumpColumnInfo(new Dictionary<string, string>
            {
                {"Text", "Text _EnUS" },
                {"タイトル(日本語)", "タイトル(英語)" },
                {"サブタイトル(日本語)", "サブタイトル(英語)" },
                {"タイトル", "英語" },
                {"日本語", "英語" },
                {"本文(日本語)", "本文(英語？)" },
                {"日本語(0)", "英語" }
            });

            itemLookup = new AssetDumpColumnInfo(null, null, true, new string[] {
                "アイテム名",
                "名前(メモ)"
            });

            itemLookupAndAssetCols = new AssetDumpColumnInfo(null, stdExcelAssetCols.NameMappings, true, itemLookup.ItemLookupColumns);

            ListEntryDumpers.Add(TryDumpTitleSkillName);
        }

        protected virtual bool TryDumpTitleSkillName(string assetBundleName, string assetName, AssetDumpColumnInfo _, ref Dictionary<string, string> translations)
        {
            var titleSkillName = ManualLoadAsset<TitleSkillName>(assetBundleName, assetName, "abdata");
            if (titleSkillName is null)
            {
                return false;
            }
            foreach (var entry in titleSkillName.param)
            {
                var key = entry.name0;
                var value = entry.name1;
                ResourceHelper.AddLocalizationToResults(translations, key, value);
            }
            LocalizationDumpHelper?.AddAutoLocalizer($"{assetBundleName.Replace(".unity3d", string.Empty)}/{System.IO.Path.GetFileNameWithoutExtension(assetName)}", new Dictionary<string, string>(translations));
            return true;
        }
        private IEnumerable<KeyValuePair<string, string>> HandleChaListData(byte[] bytes, AssetDumpColumnInfo assetDumpColumnInfo)
        {
            ChaListData chaListData = MessagePackSerializer.Deserialize<ChaListData>(bytes);
            foreach (var entry in chaListData.dictList.Values)
            {
                foreach (var mapping in assetDumpColumnInfo.NumericMappings)
                {
                    var jpCol = mapping.Key;
                    var transCol = mapping.Value;
                    if (entry.Count < jpCol)
                    {
                        continue;
                    }
                    var key = entry[jpCol];
                    var val = string.Empty;
                    if (transCol >= 0 && entry.Count > Math.Max(11, transCol))
                    {
                        val = entry[transCol];
                    }
                    ///Logger.LogWarning($"match: {key}={val}");
                    yield return new KeyValuePair<string, string>(key, val);
                }
            }

            foreach (var id in chaListData.dictList.Keys)
            {
                foreach (var mapping in assetDumpColumnInfo.NameMappings)
                {
                    var key = chaListData.GetInfo(id, mapping.Key);
                    if (!string.IsNullOrEmpty(key))
                    {
                        var val = chaListData.GetInfo(id, mapping.Value);
                        yield return new KeyValuePair<string, string>(key, IsValidLocalization(key, val) ? val : string.Empty);
                    }
                }
            }
        }
        protected override IEnumerable<KeyValuePair<string, string>> DumpListBytes(byte[] bytes, AssetDumpColumnInfo assetDumpColumnInfo)
        {
            bool handled = false;
            foreach (var result in base.DumpListBytes(bytes, assetDumpColumnInfo))
            {
                handled = true;
                yield return result;
            }

            if (!handled)
            {
                if (TextResourceHelper.ArrayContains<byte>(bytes, Encoding.UTF8.GetBytes(ChaListData.ChaListDataMark)))
                {
                    foreach (var entry in HandleChaListData(bytes, assetDumpColumnInfo))
                    {
                        yield return entry;
                    }
                }
            }
        }

        protected override IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            foreach (var list in base.GetLists())
            {
                yield return list;
            }

            yield return new KeyValuePair<string, AssetDumpColumnInfo>("actor/animal", itemLookupAndAssetCols);

            foreach (var animal in new string[] { "cat", "chicken" })
            {
                foreach (var state in new string[] { "wild", "pet" })
                {
                    yield return new KeyValuePair<string, AssetDumpColumnInfo>($"actor/animal/action/{animal}/{state}", stdExcelAssetCols);
                }
            }

            yield return new KeyValuePair<string, AssetDumpColumnInfo>("actor/gameitem/recipe/recycling", itemLookup);

            foreach (var mapdir in new string[] { "eventpoint", "popupinfo" })
            {
                yield return new KeyValuePair<string, AssetDumpColumnInfo>($"map/{mapdir}", stdExcelAssetCols);
            }
        }
    }
}
