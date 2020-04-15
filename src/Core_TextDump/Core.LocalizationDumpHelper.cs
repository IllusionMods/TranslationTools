using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IllusionMods
{
    public class LocalizationDumpHelper : BaseDumpHelper
    {
        protected readonly Dictionary<string, Dictionary<string, string>> AutoLocalizers = new Dictionary<string, Dictionary<string, string>>();

        public LocalizationDumpHelper(TextDump plugin) : base(plugin) { }

        public virtual string LocalizationFileRemap(string outputFile) => outputFile;

        protected virtual IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            yield return GetAutoLocalizerDumpers;
            yield return GetStaticLocalizers;
            if (TextDump.IsReadyToDump())
            {
                yield return GetInstanceLocalizers;
            }
        }

        public virtual IEnumerable<TranslationDumper> GetLocalizations()
        {
            var localizers = GetLocalizationGenerators().ToList();

            int tryCount = 0;
            var retryLocalizers = new List<TranslationGenerator>();
            while (localizers.Count > 0)
            {
                tryCount++;
                if (tryCount > 3) break;
                while (localizers.Count > 0)
                {
                    var localizerGenerator = localizers.PopFront();
                    List<TranslationDumper> entries = new List<TranslationDumper>();
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
                        Logger.LogError($"Re-adding localizer to end: {nameof(localizerGenerator)} : {err}");
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
            if (!AutoLocalizers.TryGetValue(path, out var translations))
            {
                AutoLocalizers[path] = translations = new Dictionary<string, string>();
            }
            foreach (var entry in newTranslations)
            {
                AddLocalizationToResults(translations, entry);
            }
        }

        public virtual IEnumerable<TranslationDumper> GetAutoLocalizerDumpers()
        {
            foreach (var entry in AutoLocalizers)
            {
                yield return new TranslationDumper(
                    CombinePaths("AutoLocalizers", entry.Key),
                    () => entry.Value);
            }
        }

        protected void FieldLocalizerAddResults(object srcs, ref Dictionary<string, string> results)
        {
            void addResult(string[] src, ref Dictionary<string, string> _results)
            {
                //Logger.LogWarning(src);
                AddLocalizationToResults(_results, src[0], src.Length > 1 ? src[1] : string.Empty);
            }

            if (srcs is string[][] nested)
            {
                for (int i = 0; i < nested.Length; i++)
                {
                    addResult(nested[i], ref results);
                }
            }
            else if (srcs is string[] single)
            {
                addResult(single, ref results);
            }
            else if (srcs is IDictionary srcsDict)
            {
                FieldLocalizerAddResults(srcsDict.Values, ref results);
            }
            else if (srcs is IEnumerable<string[]> entries)
            {
                FieldLocalizerAddResults(entries.ToArray(), ref results);
            }
            else
            {
                Logger.LogError($"FieldLocalizerAddResults: Unexpected object: {srcs}");
            }
        }
        protected TranslationDumper MakeStandardInstanceLocalizer<T>(params string[] fieldNames) where T : new()
        {
            Dictionary<string, string> localizer()
            {
                Dictionary<string, string> results = new Dictionary<string, string>();

                var instance = new T();

                foreach (var fieldName in fieldNames)
                {
                    var field = AccessTools.Field(typeof(T), fieldName);
                    if (field is null)
                    {
                        Logger.LogWarning($"MakeStandardInstanceLocalizer: Unable to find field: {typeof(T).Name}.{fieldName}");
                        continue;
                    }

                    FieldLocalizerAddResults(field.GetValue(instance), ref results);
                }

                return results;
            }
            return new TranslationDumper($"Instance/{typeof(T).FullName.Replace('.', '/').Replace('+', '/')}", localizer);
        }

        protected TranslationDumper MakeStandardStaticLocalizer(Type type, params string[] fieldNames)
        {
            Dictionary<string, string> localizer()
            {
                Dictionary<string, string> results = new Dictionary<string, string>();

                foreach (var fieldName in fieldNames)
                {
                    var field = AccessTools.Field(type, fieldName);
                    if (field is null)
                    {
                        Logger.LogWarning($"MakeStandardStaicLocalizer: Unable to find field: {type.Name}.{fieldName}");
                        continue;
                    }
                    FieldLocalizerAddResults(field.GetValue(null), ref results);
                }

                return results;
            }
            return new TranslationDumper($"Static/{type.FullName.Replace('.', '/').Replace('+', '/')}", localizer);
        }

        public virtual IEnumerable<TranslationDumper> GetStaticLocalizers()
        {
            return new TranslationDumper[0];
        }

        public virtual IEnumerable<TranslationDumper> GetInstanceLocalizers()
        {
            return new TranslationDumper[0];
        }
    }
}
