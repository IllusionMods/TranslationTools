using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using IllusionMods.Shared;
using Manager;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public class LocalizationDumpHelper : BaseDumpHelper
    {
        protected static LocalizationDumpHelper _instance;

        protected readonly Dictionary<string, Dictionary<string, string>> AutoLocalizers =
            new Dictionary<string, Dictionary<string, string>>();


        private static readonly object[] noObjArray = new object[0];

        protected LocalizationDumpHelper(TextDump plugin) : base(plugin)
        {
            _instance = this;
        }

        public static Regex FormatStringRegex { get; protected set; } = new Regex(@"\{[0-9]\}");

        protected static IList<ITranslationDumper> HookedTextLocalizationGenerators { get; } =
            new List<ITranslationDumper>();

        public virtual string LocalizationFileRemap(string outputFile)
        {
            return outputFile;
        }

        protected virtual string GetPersonalityName(VoiceInfo.Param voiceInfo)
        {
            return voiceInfo.Personality;
        }

        protected virtual string GetPersonalityNameLocalization(VoiceInfo.Param voiceInfo)
        {
            return voiceInfo.Personality;
        }

        protected Dictionary<string, string> PersonalityLocalizer()
        {
            var results = new Dictionary<string, string>();
            foreach (var voiceInfo in GetVoiceInfos())
            {
                var key = GetPersonalityName(voiceInfo);
                var value = GetPersonalityNameLocalization(voiceInfo);
                AddLocalizationToResults(results, key, value);
                AddLocalizationToResults(ResourceHelper.GlobalMappings, key, value);
            }

            return results;
        }

        protected virtual IEnumerable<VoiceInfo.Param> GetVoiceInfos()
        {
#if AI || KK
            return Singleton<Voice>.Instance.voiceInfoList;
#elif HS2
            if (Manager.Voice.infoTable != null) return Manager.Voice.infoTable.Values;
            return new List<VoiceInfo.Param>();
#else
            return new List<VoiceInfo.Param>();
#endif
        }

        private IEnumerable<ITranslationDumper> GetHookedTextLocalizationGenerators()
        {

            return HookedTextLocalizationGenerators;
        }

        protected virtual IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            yield return GetHookedTextLocalizationGenerators;
            yield return GetAutoLocalizerDumpers;
            yield return GetStaticLocalizers;
            yield return WrapTranslationCollector("Personalities", PersonalityLocalizer);
#if LOCALIZE
            yield return GetOtherDataLocalizers;
#endif
            if (TextDump.IsReadyForFinalDump())
            {
                yield return GetInstanceLocalizers;
            }
        }

        public virtual IEnumerable<ITranslationDumper> GetLocalizations()
        {
            var localizers = GetLocalizationGenerators().ToList();

            var tryCount = 0;
            var retryLocalizers = new List<TranslationGenerator>();
            while (localizers.Count > 0)
            {
                tryCount++;
                if (tryCount > 3) break;
                while (localizers.Count > 0)
                {
                    var localizerGenerator = localizers.PopFront();
                    var entries = new List<ITranslationDumper>();
                    try
                    {
                        foreach (var entry in localizerGenerator())
                        {
                            Logger.LogDebug($"Generated localizer: {entry.Path}");
                            entries.Add(entry);
                        }
                    }
                    catch (Exception err)
                    {
                        Logger.LogWarning($"Re-adding localizer to end: {nameof(localizerGenerator)} : {err}");
                        retryLocalizers.Add(localizerGenerator);
                    }

                    foreach (var l in entries)
                    {
                        yield return l;
                    }
                }

                localizers.AddRange(retryLocalizers);
                retryLocalizers.Clear();
            }
        }

        public void AddAutoLocalizer(string path, Dictionary<string, string> newTranslations)
        {
            var translations = AutoLocalizers.GetOrInit(path);

            foreach (var entry in newTranslations)
            {
                AddLocalizationToResults(translations, entry);
            }
        }

        public virtual IEnumerable<ITranslationDumper> GetAutoLocalizerDumpers()
        {
            foreach (var entry in AutoLocalizers)
            {
                yield return new StringTranslationDumper(
                    CombinePaths("AutoLocalizers", entry.Key),
                    () => entry.Value);
            }
        }

        protected bool FieldLocalizerAddResults(object sources, ref Dictionary<string, string> results, bool stringsAreLocalizations=true)
        {
            bool result = false;
            void AddResult(string[] src, ref Dictionary<string, string> localizations)
            {
                //Logger.LogWarning(src);
                if (stringsAreLocalizations)
                {
                    AddLocalizationToResults(localizations, src[0], src.Length > 1 ? src[1] : string.Empty);
                }
                else
                {
                    foreach (var str in src)
                    {
                        AddLocalizationToResults(localizations, str, string.Empty);
                    }
                }

                result = true;
            }

            while (true)
            {
                switch (sources)
                {
                    case string[][] nestedArr:
                    {
                        foreach (var entry in nestedArr)
                        {
                            AddResult(entry, ref results);
                        }

                        break;
                    }
                    case string[,] multiArr:
                    {
                        for (var i = 0; i < multiArr.GetLength(1); i++)
                        {
                            var entry = new[] {multiArr[0, i], multiArr[1, i]};
                            AddResult(entry, ref results);
                        }
                        break;
                    }

                    case string[] singleArr:
                        AddResult(singleArr, ref results);
                        break;

                    case string singleStr:
                        sources = new[] {singleStr};
                        continue;

                    case IDictionary sourcesDict:
                        sources = sourcesDict.Values;
                        continue;
                    case IEnumerable<string[]> entries:
                        sources = entries.ToArray();
                        continue;

                    default:
                        Logger.LogError($"FieldLocalizerAddResults: Unexpected object: {sources}");
                        break;
                }

                break;
            }

            return result;
        }

        protected StringTranslationDumper MakeStandardInstanceLocalizer<T>(params string[] fieldNames) where T : new()
        {
            return MakeStandardInstanceLocalizer<T>(true, fieldNames);
        }

        protected StringTranslationDumper MakeStandardInstanceLocalizer<T>(bool stringsAreLocalizations, params string[] fieldNames) where T : new()
        {
            Dictionary<string, string> Localizer()
            {
                var results = new Dictionary<string, string>();

                var instance = new T();

                var type = typeof(T);
                foreach (var fieldName in fieldNames)
                {
                    try
                    {
                        var field = type.GetField(fieldName, AccessTools.all);
                        if (!(field is null))
                        {
                            if (FieldLocalizerAddResults(field.GetValue(instance), ref results,
                                stringsAreLocalizations))
                                continue;
                            Logger.LogWarning(
                                $"{nameof(MakeStandardInstanceLocalizer)}: Unable process field: {typeof(T).Name}.{fieldName}: ({field.FieldType.Name}){field.GetValue(instance)}");
                        }

                        var prop = type.GetProperty(fieldName, AccessTools.all);
                        if (!(prop is null))
                        {
                            if (FieldLocalizerAddResults(prop.GetValue(instance, noObjArray), ref results, stringsAreLocalizations))
                                continue;
                            Logger.LogWarning(
                                $"{nameof(MakeStandardInstanceLocalizer)}: Unable process property: {type.Name}.{fieldName}: ({prop.PropertyType.Name}){prop.GetValue(instance, noObjArray)}");
                        }



                        if (field is null && prop is null)
                        {
                            Logger.LogWarning(
                                $"{nameof(MakeStandardInstanceLocalizer)}: Unable to find field/property: {type.Name}.{fieldName}");
                        }
                    }
                    catch (Exception err)
                    {
                        Logger.LogWarning(
                            $"{nameof(MakeStandardInstanceLocalizer)}: unexpected error processing: {type.Name}.{fieldName}: {err}");
                    }
                }

                return results;
            }

            return new StringTranslationDumper($"Instance/{GetPathForType(typeof(T))}", Localizer);
        }

        protected StringTranslationDumper MakeStandardStaticLocalizer(Type type, params string[] fieldNames)
        {
            return MakeStandardStaticLocalizer(type, true, fieldNames);
        }

        protected StringTranslationDumper MakeStandardStaticLocalizer(Type type, bool stringsAreLocalizations, params string[] fieldNames)
        {
            Dictionary<string, string> Localizer()
            {
                var results = new Dictionary<string, string>();

                foreach (var fieldName in fieldNames)
                {
                    try
                    {
                        var field = type.GetField(fieldName, AccessTools.all);
                        if (!(field is null))
                        {
                            if (FieldLocalizerAddResults(field.GetValue(null), ref results, stringsAreLocalizations))
                                continue;
                            Logger.LogWarning(
                                $"{nameof(MakeStandardStaticLocalizer)}: Unable process field: {type.Name}.{fieldName}: ({field.FieldType.Name}){field.GetValue(null)}");
                        }

                        var prop = type.GetProperty(fieldName, AccessTools.all);
                        if (!(prop is null))
                        {
                            if (FieldLocalizerAddResults(prop.GetValue(null,noObjArray), ref results, stringsAreLocalizations))
                                continue;
                            Logger.LogWarning(
                                $"{nameof(MakeStandardStaticLocalizer)}: Unable process property: {type.Name}.{fieldName}: ({prop.PropertyType.Name}){prop.GetValue(null, noObjArray)}");
                        }

                        if (field is null && prop is null)
                        {
                            Logger.LogWarning(
                                $"{nameof(MakeStandardStaticLocalizer)}: Unable to find field/property: {type.Name}.{fieldName}");
                        }
                    }
                    catch (Exception err)
                    {
                        Logger.LogWarning(
                            $"{nameof(MakeStandardStaticLocalizer)}: unexpected error processing: {type.Name}.{fieldName}: {err}");
                    }


                }

                return results;
            }

            return new StringTranslationDumper($"Static/{GetPathForType(type)}", Localizer);
        }


        private static string GetPathForType(Type type)
        {
            return type.FullName != null
                ? type.FullName.Replace('.', '/').Replace('+', '/')
                : type.Name ?? "Unknown";
        }

        public virtual IEnumerable<ITranslationDumper> GetStaticLocalizers()
        {
            return new ITranslationDumper[0];
        }

        public virtual IEnumerable<ITranslationDumper> GetInstanceLocalizers()
        {
            return new ITranslationDumper[0];
        }
//                    voiceInfo.Get(Localize.Translate.Manager.Language)


#if LOCALIZE
        protected readonly Dictionary<int, Dictionary<string, string>> OtherDataByTag =
            new Dictionary<int, Dictionary<string, string>>();

        protected virtual IEnumerable<ITranslationDumper> GetOtherDataLocalizers()
        {
            var categories = Enum.GetValues(typeof(Localize.Translate.Manager.SCENE_ID))
                .Cast<Localize.Translate.Manager.SCENE_ID>();

            foreach (var cat in categories)
            {
                var category = cat;

                Dictionary<string, string> Localizer()
                {
                    var tagsSeen = new Dictionary<int, HashSet<string>>();
                    var results = new Dictionary<string, string>();
                    var otherData = Localize.Translate.Manager.LoadScene(category, null);
                    foreach (var dataset in OtherDataByTag)
                    {
                        tagsSeen[dataset.Key] = new HashSet<string>();
                        foreach (var entry in dataset.Value)
                        {
                            if (!otherData.ContainsKey(dataset.Key)) continue;

                            var localization = otherData.Get(dataset.Key).Values.FindTagText(entry.Key);
                            if (string.IsNullOrEmpty(localization)) continue;

                            AddLocalizationToResults(results, entry.Value, localization);
                            if (!string.IsNullOrEmpty(localization))
                            {
                                tagsSeen[dataset.Key].Add(BuildSeenKey(dataset.Key, entry.Key));
                            }
                        }
                    }

                    foreach (var dataSet in otherData)
                    {
                        var seen = tagsSeen.ContainsKey(dataSet.Key) ? tagsSeen[dataSet.Key] : new HashSet<string>();
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

                yield return new StringTranslationDumper($"OtherData/{category}", Localizer);
            }
        }
#endif
    }
}
