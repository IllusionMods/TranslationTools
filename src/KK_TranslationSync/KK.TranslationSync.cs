using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using IllusionMods.Shared;

namespace IllusionMods
{
    /// <summary>
    /// Copies translations from one .txt file to another for the same personality
    /// </summary>
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier)]
    [BepInPlugin(GUID, PluginName, Version)]
    public class TranslationSync : BaseUnityPlugin
    {
        public const string GUID = "com.deathweasel.bepinex.translationsync";
        public const string PluginName = "Translation Sync";
        public const string PluginNameInternal = "KK_TranslationSync";
        public const string Version = "1.3.2";

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> Personality { get; private set; }
        public static ConfigEntry<KeyboardShortcut> TranslationSyncHotkey { get; private set; }

        public static string TranslationsRoot { get; private set; }
        internal void Main()
        {
            TranslationsRoot = Path.Combine(AutoTranslatorSettings.DefaultRedirectedResourcePath, @"assets\abdata");
            Enabled = Config.Bind("Settings", "Enabled", true, "Whether the plugin is enabled");
            Personality = Config.Bind("Config", "Personality", "c00", "Personality to sync");
            TranslationSyncHotkey = Config.Bind("Keyboard Shortcuts", "Sync Translation Hotkey", new KeyboardShortcut(KeyCode.Alpha0), "Press to sync translations for the specified personality. Hold alt to force overwrite all translations if different (dangerous, make backups first). Hold ctrl to sync all translations for all personalities (may take a while).");
        }

        internal void Update()
        {
            if (!Enabled.Value) return;

            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKey(TranslationSyncHotkey.Value.MainKey))
            {
                SyncTLs(TLType.Scenario, true);
                SyncTLs(TLType.Communication, true);
                SyncTLs(TLType.H, true);
                SyncTLs(TLType.Nickname, true);
                Logger.Log(LogLevel.Info, "Sync complete.");
            }
            else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(TranslationSyncHotkey.Value.MainKey))
            {
                DumpAll();
            }
            else if (TranslationSyncHotkey.Value.IsDown())
            {
                //CountText();
                SyncTLs(TLType.Scenario);
                SyncTLs(TLType.Communication);
                SyncTLs(TLType.H);
                SyncTLs(TLType.Nickname);
                Logger.Log(LogLevel.Info, "Sync complete.");
            }
        }

        public void DumpAll()
        {
            for (int i = 0; i <= 37; i++)
            {
                Personality.Value = "c" + i.ToString("00");
                SyncTLs(TLType.Scenario);
                SyncTLs(TLType.Communication);
                SyncTLs(TLType.H);
                SyncTLs(TLType.Nickname);
            }

            for (int i = 0; i <= 10; i++)
            {
                Personality.Value = "c-" + i.ToString("00");
                SyncTLs(TLType.Scenario);
                SyncTLs(TLType.Communication);
                SyncTLs(TLType.H);
                SyncTLs(TLType.Nickname);
            }

            for (int i = 0; i <= 10; i++)
            {
                Personality.Value = "c-" + i.ToString("0");
                SyncTLs(TLType.Scenario);
                SyncTLs(TLType.Communication);
                SyncTLs(TLType.H);
                SyncTLs(TLType.Nickname);
            }
            Logger.Log(LogLevel.Info, "Sync complete.");
        }

        internal void CountText()
        {
            HashSet<string> AllJPText = new HashSet<string>();

            void CountJPText(string folder)
            {
                var FilePaths = Directory.GetFiles(folder, "*.txt", SearchOption.AllDirectories);
                foreach (string FileName in FilePaths)
                {
                    string[] Lines = File.ReadAllLines(FileName);

                    foreach (string Line in Lines)
                    {
                        string NewLine = Line;
                        if (!NewLine.Contains("="))
                            continue;

                        if (NewLine.StartsWith(@"//"))
                            NewLine = NewLine.Substring(2, NewLine.Length - 2);

                        if (Line.Split('=')[0].IsNullOrEmpty())
                            continue;

                        AllJPText.Add(Line.Split('=')[0]);
                    }
                }
            }

            var translationsTop = Path.Combine(Paths.PluginPath, TranslationsRoot);

            CountJPText(Path.Combine(translationsTop, "adv"));
            CountJPText(Path.Combine(translationsTop, "communication"));
            CountJPText(Path.Combine(translationsTop, "h"));
            CountJPText(Path.Combine(translationsTop, "h"));
            CountJPText(Path.Combine(translationsTop, Path.Combine("etcetra", Path.Combine("list", "nickname"))));

            Logger.Log(LogLevel.Info, $"Total Japanese lines: {AllJPText.Count}");
        }

        private void SyncTLs(TLType translationType, bool ForceOverwrite = false)
        {
            string PersonalityNumber = Personality.Value.Replace("c", "");
            string FolderPath = Path.Combine(Paths.PluginPath, TranslationsRoot);
            switch (translationType)
            {
                case TLType.Scenario:
                    Logger.Log(LogLevel.Info, $"Syncing Scenario translations for personality {Personality.Value}...");
                    FolderPath = Path.Combine(FolderPath, @"adv\scenario");
                    FolderPath = Path.Combine(FolderPath, Personality.Value);
                    break;
                case TLType.Communication:
                    Logger.Log(LogLevel.Info, $"Syncing Communication translations for personality {Personality.Value}...");
                    if (Personality.Value.Contains("-"))
                    {
                        Logger.Log(LogLevel.Info, $"Scenario characters have no Communication files, skipping.");
                        return;
                    }
                    FolderPath = Path.Combine(FolderPath, "communication");
                    break;
                case TLType.H:
                    Logger.Log(LogLevel.Info, $"Syncing H translations for personality {Personality.Value}...");
                    FolderPath = Path.Combine(FolderPath, @"h\list");
                    break;
                case TLType.Nickname:
                    Logger.Log(LogLevel.Info, $"Syncing Nickname translations for personality {Personality.Value}...");
                    FolderPath = Path.Combine(FolderPath, @"translation\etcetra\list\nickname");
                    break;
                default:
                    return;
            }

            if (!Directory.Exists(FolderPath))
                return;

            Logger.LogDebug($"FolderPath={FolderPath}");
            var FilePaths = Directory.GetFiles(FolderPath, "*.txt", SearchOption.AllDirectories).Reverse().ToArray();
            if (FilePaths.Length == 0)
                return;

            foreach (string File1 in FilePaths)
            {
                string Ending = File1.Replace(FolderPath, "").Remove(0, 1);
                bool DidEdit1 = false;

                switch (translationType)
                {
                    case TLType.Scenario:
                        if (!Ending.Contains("penetration"))
                            Ending = Ending.Remove(0, 2);
                        break;
                    case TLType.Communication:
                        if (Ending.Contains($"communication_{PersonalityNumber}"))
                            Ending = Ending.Remove(0, Ending.IndexOf("communication_", StringComparison.Ordinal));
                        else if (Ending.Contains($"communication_off_{PersonalityNumber}"))
                            Ending = Ending.Remove(0, Ending.IndexOf("communication_off_", StringComparison.Ordinal));
                        else if (Ending.Contains($"optiondisplayitems_{PersonalityNumber}"))
                            Ending = Ending.Remove(0, Ending.IndexOf("optiondisplayitems_", StringComparison.Ordinal));
                        else
                            continue;
                        break;
                    case TLType.H:
                        if (Ending.Contains($"personality_voice_{Personality.Value}"))
                            Ending = $"personality_voice_{Personality.Value}";
                        else
                            continue;
                        break;
                    case TLType.Nickname:
                        if (Ending.Contains($"c{Personality.Value}"))
                            Ending = Ending.Remove(0, Ending.IndexOf($"c{Personality.Value}", StringComparison.Ordinal));
                        else
                            continue;
                        break;
                }
                Logger.LogDebug($"+{Ending}");

                string[] Lines1 = File.ReadAllLines(File1);
                Dictionary<string, string> TLLines = new Dictionary<string, string>(new TrimmedStringComparer());

                for (int i = 0; i < Lines1.Count(); i++)
                {
                    string Line1 = Lines1[i];
                    if (Line1.IsNullOrEmpty())
                        continue;

                    var Line1Split = Line1.Split('=');

                    if (CheckLineForErrors(Line1, File1, i + 1))
                        continue;

                    if (Line1.StartsWith(@"//") || Line1.EndsWith("="))
                        continue;

                    if (FormatTLText(ref Line1Split[1]))
                        DidEdit1 = true;

                    if (FormatUnTLText(ref Line1Split[0]))
                        DidEdit1 = true;

                    Lines1[i] = $"{Line1Split[0]}={Line1Split[1]}";
                    try
                    {
                        TLLines.Add(Line1Split[0], Line1Split[1]);
                    }
                    catch (ArgumentException)
                    {
                        Logger.Log(LogLevel.Warning, $"Duplicate translation line detected, only the first will be used: {Line1Split[0]}");
                    }
                }
                //foreach (var x in TLLines)
                //    Logger.Log(LogLevel.Info, x);

                if (DidEdit1)
                    SaveFile(File1, Lines1);

                foreach (string File2 in FilePaths.Where(x => x != File1))
                {
                    switch (translationType)
                    {
                        case TLType.Scenario:
                            if (!File2.Replace(FolderPath, "").EndsWith(Ending))
                                continue;
                            break;
                        case TLType.Communication:
                            if ((File2.Contains($"communication_{PersonalityNumber}") || File2.Contains($"communication_off_{PersonalityNumber}"))
                                && (Ending.Contains($"communication_{PersonalityNumber}") || Ending.Contains($"communication_off_{PersonalityNumber}")))
                            { }
                            else if (File2.Contains($"optiondisplayitems_{PersonalityNumber}") && Ending.Contains($"optiondisplayitems_{PersonalityNumber}"))
                            { }
                            else
                                continue;
                            break;
                        case TLType.H:
                            // fall through
                        case TLType.Nickname:
                            if (!File2.Contains(Ending))
                                continue;
                            break;
                    }

                    bool DidEdit2 = false;
                    string[] Lines2 = File.ReadAllLines(File2);

                    Logger.LogDebug($"-{File2}");

                    for (int i = 0; i < Lines2.Count(); i++)
                    {
                        string Line2 = Lines2[i];
                        if (Line2.IsNullOrEmpty())
                            continue;
                        string[] Line2Split = Line2.Split('=');

                        if (CheckLineForErrors(Line2, File2, i + 1))
                            continue;

                        if (FormatUnTLText(ref Line2Split[0]))
                            DidEdit2 = true;

                        if (FormatTLText(ref Line2Split[1]))
                            DidEdit2 = true;

                        Lines2[i] = $"{Line2Split[0]}={Line2Split[1]}";

                        string JPText = Line2Split[0];
                        if (JPText.StartsWith(@"//"))
                            JPText = JPText.Substring(2, Line2Split[0].Length - 2);
                        JPText = JPText.Trim();

                        string TLText = Line2Split[1];

                        if (TLLines.TryGetValue(JPText, out string NewTLText))
                        {
                            if (TLText.IsNullOrEmpty())
                            {
                                Lines2[i] = $"{JPText}={NewTLText}";
                                DidEdit2 = true;
                                Logger.LogDebug($"Setting:{JPText}={NewTLText}");
                            }
                            else
                            {
                                if (TLText != NewTLText)
                                {
                                    StringBuilder sb = new StringBuilder("Translations do not match!").Append(Environment.NewLine);
                                    sb.Append(File1).Append(Environment.NewLine);
                                    sb.Append($"{JPText}={NewTLText}").Append(Environment.NewLine);
                                    sb.Append($"Line:{i + 1} {File2}").Append(Environment.NewLine);
                                    sb.Append($"{JPText}={TLText}");
                                    if (ForceOverwrite)
                                    {
                                        sb.Append(Environment.NewLine).Append("Overwriting...");
                                        Lines2[i] = $"{JPText}={NewTLText}";
                                        DidEdit2 = true;
                                    }
                                    Logger.Log(LogLevel.Warning, sb.ToString());
                                    continue;
                                }
                            }
                        }
                    }

                    if (DidEdit2)
                        SaveFile(File2, Lines2);
                }
            }
        }

        private enum TLType { Scenario, Communication, H, Nickname }

        private bool CheckLineForErrors(string line, string fileName, int lineNumber)
        {
            if (!line.Contains("="))
            {
                Logger.Log(LogLevel.Warning, $"File {fileName} Line {lineNumber } has no =");
                return true;
            }

            var Line2Split = line.Split('=');
            if (Line2Split.Count() > 2)
            {
                Logger.Log(LogLevel.Warning, $"File {fileName} Line {lineNumber} has more than one =");
                return true;
            }

            if (Line2Split[0].IsNullOrEmpty())
            {
                Logger.Log(LogLevel.Error, $"File {fileName} Line {lineNumber } has nothing on the left side of =");
                return true;
            }

            if (!Line2Split[0].StartsWith(@"//") && Line2Split[1].IsNullOrEmpty())
            {
                Logger.Log(LogLevel.Error, $"File {fileName} Line {lineNumber } is uncommented but has nothing on the right side of =");
                return true;
            }

            return false;
        }

        private void SaveFile(string filePath, string[] lines)
        {
            Logger.Log(LogLevel.Info, $"Saving file:{filePath}");
            File.WriteAllLines(filePath, lines);
        }

        private bool FormatTLText(ref string tlText)
        {
            bool DidEdit = false;
            string NewTLText = tlText;

            NewTLText = NewTLText.Trim();

            if (NewTLText.StartsWith("「") && NewTLText.EndsWith("」"))
                NewTLText = "“" + NewTLText.Substring(1, NewTLText.Length - 2) + "”";

            //if (NewTLText.StartsWith("\"") && NewTLText.EndsWith("\""))
            //{
            //    string temp = NewTLText.Substring(1, NewTLText.Length - 2);
            //    if (!temp.IsNullOrEmpty())
            //        NewTLText = "“" + NewTLText.Substring(1, NewTLText.Length - 2) + "”";
            //}

            if (NewTLText != tlText)
            {
                tlText = NewTLText;
                DidEdit = true;
            }

            return DidEdit;
        }

        private bool FormatUnTLText(ref string UnTLText)
        {
            bool DidEdit = false;
            string NewUnTLText = UnTLText;

            NewUnTLText = NewUnTLText.Trim();

            if (NewUnTLText != UnTLText)
            {
                UnTLText = NewUnTLText;
                DidEdit = true;
            }

            return DidEdit;
        }
    }
}
