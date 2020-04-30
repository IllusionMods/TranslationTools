using System;
using System.Collections.Generic;
using System.Linq;
using Manager;
using MessagePack;
using UnityEngine;

namespace IllusionMods
{
    public class KKP_AssetDumpHelper : KK_AssetDumpHelper
    {
        public KKP_AssetDumpHelper(TextDump plugin) : base(plugin) { }

        protected override string LookupSpeakerLocalization(string speaker, string bundle, string asset)
        {
            string result;
            if (Localize.Translate.Manager.initialized)
            {
                result = Localize.Translate.Manager.GetScenarioCharaName(speaker, bundle, asset);
                if (result != speaker && !result.IsNullOrEmpty()) return result;
            }

            if (SpeakerLocalizations.TryGetValue(speaker, out result))
            {
                if (result != speaker && !result.IsNullOrEmpty()) return result;
            }

            result = base.LookupSpeakerLocalization(speaker, bundle, asset);
            if (result != speaker && !result.IsNullOrEmpty()) return result;

            return string.Empty;
        }

        protected override IEnumerable<KeyValuePair<string, string>> HandleChaListData(TextAsset asset,
            AssetDumpColumnInfo assetDumpColumnInfo)
        {
            if (!TextDump.IsReadyForFinalDump())
            {
                foreach (var result in base.HandleChaListData(asset, assetDumpColumnInfo))
                {
                    yield return result;
                }
            }
            else
            {
                // now try and redump/populate
                if (!TextDump.IsReadyForFinalDump()) yield break;
                var categoryName = asset.name.Substring(0, asset.name.LastIndexOf("_", StringComparison.Ordinal));
                var category = (ChaListDefine.CategoryNo) Enum.Parse(typeof(ChaListDefine.CategoryNo), categoryName);
                var catInfo = Singleton<Character>.Instance.chaListCtrl.GetCategoryInfo(category);

                var lookupDict = new Dictionary<string, string>();

                foreach (var value in catInfo.Select(c => c.Value))
                {
                    if (!value.Name.IsNullOrEmpty())
                    {
                        lookupDict[$"{value.Kind}_{value.Id}"] = value.Name;
                    }
                }

                var chaListData = MessagePackSerializer.Deserialize<ChaListData>(asset.bytes);

                foreach (var entry in chaListData.dictList.Values)
                {
                    foreach (var id in chaListData.dictList.Keys)
                    {
                        var key = chaListData.GetInfo(id, "Name");
                        if (string.IsNullOrEmpty(key)) continue;
                        if (!lookupDict.TryGetValue($"{chaListData.GetInfo(id, "Kind")}_{id}", out var val))
                        {
                            val = string.Empty;
                        }

                        yield return new KeyValuePair<string, string>(key, val);
                    }
                }
            }
        }
    }
}
