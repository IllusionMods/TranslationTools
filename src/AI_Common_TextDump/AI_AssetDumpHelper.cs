using System.Collections.Generic;
using System.IO;

namespace IllusionMods
{
    public class AI_AssetDumpHelper : AI_HS2_AssetDumpHelper
    {
        protected AssetDumpColumnInfo TitleAssetCols;
        protected AI_AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            TitleAssetCols = new AssetDumpColumnInfo {CombineWithParentBundle = true};

            ListEntryDumpers.Add(TryDumpTitleSkillName);
        }

        protected override string GetMapInfoPath() => "list/map/mapinfo";

        protected override IEnumerable<string> GetHTypeNames()
        {
            string[] result;
            try
            {
                var table = Singleton<Manager.Resources>.Instance.HSceneTable;
                var field = table.GetType().GetField("assetNames");

                result = field?.GetValue(table) as string[];
            }
            catch
            {
                result = null;
            }

            return result ?? base.GetHTypeNames();
        }

        protected virtual bool TryDumpTitleSkillName(string assetBundleName, string assetName, AssetDumpColumnInfo _,
            IDictionary<string, string> translations)
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

            LocalizationDumpHelper?.AddAutoLocalizer(
                $"{assetBundleName.Replace(".unity3d", string.Empty)}/{Path.GetFileNameWithoutExtension(assetName)}",
                new Dictionary<string, string>(translations));
            return true;
        }


        protected override IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            foreach (var list in base.GetLists())
            {
                yield return list;
            }

            yield return new KeyValuePair<string, AssetDumpColumnInfo>("actor/animal", ItemLookupAndAssetCols);

            foreach (var animal in new[] {"cat", "chicken"})
            {
                foreach (var state in new[] {"wild", "pet"})
                {
                    yield return new KeyValuePair<string, AssetDumpColumnInfo>($"actor/animal/action/{animal}/{state}",
                        StdExcelAssetCols);
                }
            }

            yield return new KeyValuePair<string, AssetDumpColumnInfo>("actor/gameitem/recipe/recycling", ItemLookup);

            foreach (var mapdir in new[] {"eventpoint", "popupinfo", "mapinfo"})
            {
                yield return new KeyValuePair<string, AssetDumpColumnInfo>($"map/{mapdir}", StdExcelAssetCols);
            }
        }
    }
}
