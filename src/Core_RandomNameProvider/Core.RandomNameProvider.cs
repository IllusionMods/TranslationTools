using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;


namespace IllusionMods
{
    public partial class RandomNameProvider : BaseUnityPlugin
    {
        public const string GUID = "com.illusionmods.translationtools.random_name_provider";
        public const string PluginName = "Random Name Provider";
        public const string Version = "2.0.1.2";

        internal new static ManualLogSource Logger;

        public static ConfigEntry<bool> DumpNames { get; private set; }
        public static ConfigEntry<bool> ReplaceMode { get; private set; }
        public static ConfigEntry<bool> EnableLoading { get; private set; }

        public static string NameDirectory = Path.Combine("UserData", "Names");

        public static bool DumpCompleted = false;
        public void Awake()
        {
            // have to hook in awake since random list loaded early
            Logger = Logger ?? base.Logger;
            EnableLoading = Config.Bind("Config", "Load Names", false, "Load name lists.");
            DumpNames = Config.Bind("Config", "Dump Default", false, "Write default name lists out to files");
            ReplaceMode = Config.Bind("Config", "Replace Mode", false,
                "Replace names with external name lists (otherwise append to defaults");

            if (!Directory.Exists(NameDirectory)) Directory.CreateDirectory(NameDirectory);

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        public static partial class Hooks { }

        public static LoadOptions GetLoadOptions()
        {
            return (EnableLoading.Value ? LoadOptions.LoadNames : LoadOptions.None) |
                   (DumpNames.Value ? LoadOptions.Dump : LoadOptions.None) |
                   (ReplaceMode.Value ? LoadOptions.Replace : LoadOptions.None);
        }

        private static List<ExcelData.Param> LoadNames()
        {
            List<ExcelData.Param> result = null;
            try
            {
                result = LoadData().ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading names: {ex.Message}");
                return null;
            }

            return result;
        }

        internal static IEnumerable<ExcelData.Param> LoadData()
        {
            var nameFiles = Directory.GetFiles(NameDirectory, "*.txt")
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).Select((path) =>
                {
                    var fName = Path.GetFileNameWithoutExtension(path);
                    var numStr = fName.Substring(fName.LastIndexOf('.') + 1);
                    return new
                    {
                        ShortName = fName,
                        Path = path,
                        Kind = int.TryParse(numStr, out var kind) ? kind : -1
                    };
                }).Where(e => e.Kind != -1 && !e.ShortName.StartsWith("__default"));
            
            var buckets = new List<string>[6];
            for (var j = 0; j < 6; j++) buckets[j] = new List<string>();
            
            foreach (var entry in nameFiles)
            {
                var list = buckets[entry.Kind];
                Logger.LogDebug($"Loading {entry.ShortName} (list {entry.Kind})");
                using (var fileStream = File.OpenRead(entry.Path))
                {
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        while (!streamReader.EndOfStream)
                        {
                            var text = streamReader.ReadLine();
                            var text2 = text?.Trim();
                            if (!string.IsNullOrEmpty(text2) && !text2.StartsWith(";"))
                            {
                                list.Add(text);
                            }
                        }
                    }
                }
            }

            var max = buckets.Max(b => b.Count);
            for (var i = 0; i < max; i++)
            {
                yield return new ExcelData.Param
                {
                    list = buckets.Select(bucket => bucket.ElementAtOrDefault(i) ?? "0").ToList()
                };
            }
        }

        private static void DumpNamesToFile(List<ExcelData.Param> data)
        {
            // only dump once per execution
            if (DumpCompleted) return;
            
            Logger.LogInfo("Dumping Default Names");
            var writers = new StreamWriter[6];
            try
            {
                foreach (var list in data.Select(entry => entry.list))
                {
                    for (var j = 0; j < 6; j++)
                    {
                        if (writers[j] == null)
                        {
                            writers[j] = new StreamWriter(Path.Combine(NameDirectory, $"__default.{j}.txt"), false,
                                Encoding.UTF8);
                        }

                        if (!list[j].IsNullOrWhiteSpace() && list[j] != "0")
                        {
                            writers[j].WriteLine(list[j]);
                        }
                    }
                }
            }
            finally
            {
                for (var k = 0; k < 6; k++) writers[k]?.Dispose();
            }
            DumpCompleted = true;
        }
    }
}
