using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using ActionGame;
using Manager;
using MessagePack;
using UnityEngine;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public class KKP_AssetDumpHelper : KK_AssetDumpHelper
    {
        protected KKP_AssetDumpHelper(TextDump plugin) : base(plugin)
        {
#if RAW_DUMP_SUPPORT
            AssetDumpGenerators.Add(GetFixCharaDumpers);
#endif
        }

        internal byte[] ChaFileControlToBytes(ChaFileControl chaFileControl)
        {
            if (chaFileControl == null) return null;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    chaFileControl.SaveCharaFile(writer, true);
                }

                return stream.ToArray();
            }
        }

#if RAW_DUMP_SUPPORT
        protected IEnumerable<ITranslationDumper> GetFixCharaDumpers()
        {


            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/fixchara"))
            {
                var altAssetBundleName = CombinePaths("localize/translate/1/defdata", assetBundleName);

                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName).Where(a => a.EndsWith(".bytes")))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);
                    IEnumerable<byte> Dumper()
                    {
                        var altAsset = ManualLoadAsset<TextAsset>(altAssetBundleName, assetName, null);
                        Logger.LogFatal($"Loaded: {altAssetBundleName} {assetName} => {altAsset}");
                        return altAsset?.bytes;
                    }

                    yield return new RawTranslationDumper(filePath, Dumper);
                }
            }
        }
#endif

        protected override bool TryEventInfoTranslationLookup(string assetName, EventInfo.Param param, out string result)
        {
            result = string.Empty;
            var loader = Localize.Translate.Manager.LoadScene(Localize.Translate.Manager.SCENE_ID.EXTRA_EVENT, null);
            var match = string.Empty;
            if (int.TryParse(Path.GetFileNameWithoutExtension(assetName), out var id))
            {
                loader?.SafeGet(id)?.SafeGetText(param.ID)?.SafeProc(t => match = t);
                result = match;
            }
            return !string.IsNullOrEmpty(match) || base.TryEventInfoTranslationLookup(assetName, param, out result);
        }

        protected override bool TryMapInfoTranslationLookup(MapInfo.Param param, out string result)
        {
            result = string.Empty;
            var loader = Localize.Translate.Manager.LoadScene(Localize.Translate.Manager.SCENE_ID.MAP, null);
            var match = string.Empty;
            loader?.SafeGet(0)?.SafeGetText(param.No)?.SafeProc(t => match = t);
            result = match;

            return !string.IsNullOrEmpty(match) || base.TryMapInfoTranslationLookup(param, out result);
        }

        protected override bool TryNickNameTranslationLookup(NickName.Param param, out string result)
        {
            result = string.Empty;
            if (!param.isSpecial) return base.TryNickNameTranslationLookup(param, out result);
            var loader = Localize.Translate.Manager.LoadScene(Localize.Translate.Manager.SCENE_ID.NICK_NAME, null);

            var match = string.Empty;
            loader?.SafeGet(1)?.SafeGetText(param.ID)?.SafeProc(t => match = t);
            result = match;

            return !string.IsNullOrEmpty(match) || base.TryNickNameTranslationLookup(param, out result);
        }

        protected override Func<List<string>, IEnumerable<KeyValuePair<string, string>>> GetTranslateManagerRowProcessor(
            int sceneId, int mapIdx, int colToDump, Func<List<string>, string> idGetter = null)
        {
            if (!Localize.Translate.Manager.initialized)
            {
                return base.GetTranslateManagerRowProcessor(sceneId, mapIdx, colToDump, idGetter);
            }


            idGetter = idGetter ?? (r => r.Count > 0 ? r[0] : string.Empty);
            var scene = Localize.Translate.Manager.LoadScene(sceneId, null);
            var lookup = scene?.SafeGet(mapIdx);

            IEnumerable<KeyValuePair<string, string>> TranslateManagerRowProcessor(List<string> row)
            {
                if (row.Count <= colToDump) yield break;
                var key = row[colToDump];
                var value = string.Empty;
                if (int.TryParse(idGetter(row), out var id))
                {
                    lookup?.SafeGetText(id).SafeProc((t) => value = t);
                }

                yield return new KeyValuePair<string, string>(key, value);
            }

            return TranslateManagerRowProcessor;
        }
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
