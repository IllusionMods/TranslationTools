using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public partial class LocalizationDumpHelper : BaseDumpHelper
    {
        protected readonly Dictionary<string, Dictionary<string, string>> AutoLocalizers =
            new Dictionary<string, Dictionary<string, string>>();

        public LocalizationDumpHelper(TextDump plugin) : base(plugin) { }

        public static Regex FormatStringRegex { get; protected set; } = new Regex(@"\{[0-9]\}");

        public virtual string LocalizationFileRemap(string outputFile)
        {
            return outputFile;
        }

        protected virtual IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            yield return GetAutoLocalizerDumpers;
            yield return GetStaticLocalizers;
            if (TextDump.IsReadyForFinalDump())
            {
                yield return GetInstanceLocalizers;
            }
        }

        public virtual IEnumerable<TranslationDumper> GetLocalizations()
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
                    var entries = new List<TranslationDumper>();
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

        protected void FieldLocalizerAddResults(object sources, ref Dictionary<string, string> results)
        {
            void AddResult(string[] src, ref Dictionary<string, string> localizations)
            {
                //Logger.LogWarning(src);
                AddLocalizationToResults(localizations, src[0], src.Length > 1 ? src[1] : string.Empty);
            }

            while (true)
            {
                switch (sources)
                {
                    case string[][] nested:
                    {
                        foreach (var entry in nested)
                        {
                            AddResult(entry, ref results);
                        }

                        break;
                    }
                    case string[] single:
                        AddResult(single, ref results);
                        break;
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
        }

        protected TranslationDumper MakeStandardInstanceLocalizer<T>(params string[] fieldNames) where T : new()
        {
            Dictionary<string, string> Localizer()
            {
                var results = new Dictionary<string, string>();

                var instance = new T();

                foreach (var fieldName in fieldNames)
                {
                    var field = AccessTools.Field(typeof(T), fieldName);
                    if (field is null)
                    {
                        Logger.LogWarning(
                            $"MakeStandardInstanceLocalizer: Unable to find field: {typeof(T).Name}.{fieldName}");
                        continue;
                    }

                    FieldLocalizerAddResults(field.GetValue(instance), ref results);
                }

                return results;
            }

            return new TranslationDumper($"Instance/{GetPathForType(typeof(T))}", Localizer);
        }

        protected TranslationDumper MakeStandardStaticLocalizer(Type type, params string[] fieldNames)
        {
            Dictionary<string, string> Localizer()
            {
                var results = new Dictionary<string, string>();

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

            return new TranslationDumper($"Static/{GetPathForType(type)}", Localizer);
        }

        private static string GetPathForType(Type type)
        {
            return type.FullName != null
                ? type.FullName.Replace('.', '/').Replace('+', '/')
                : type.Name ?? "Unknown";
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
