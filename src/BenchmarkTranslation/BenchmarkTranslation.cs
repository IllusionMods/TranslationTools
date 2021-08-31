using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using IllusionMods.Shared;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUAPluginData = XUnity.AutoTranslator.Plugin.Core.Constants.PluginData;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(XUAPluginData.Identifier)]
    public class BenchmarkTranslation : BaseUnityPlugin
    {
        public const string GUID = "com.illusionmods.translationtools.benchmarktranslation";
        public const string PluginName = "Benchmark Translation";
        public const string Version = "0.3.0.3";
        public const string PluginNameInternal = PluginName;

        private const int MaxOutstandingJobs = 500;

        private static long _outstandingJobs;
        private static bool _inProgress;

        private static int _consecutiveFrames = -1;
        private static int _lastQueuedFrame = -1;
        private static int _queuedThisFrame;
        private static int _consecutiveSeconds = -1;
        private static int _lastQueuedSecond = -1;
        private static int _queuedThisSecond;

        private readonly List<BenchmarkResult> _results = new List<BenchmarkResult>();

        public static ConfigEntry<bool> Enabled { get; private set; }
        public static ConfigEntry<string> CorpusPath { get; private set; }

        public static ConfigEntry<int> LoopCount { get; private set; }

        public static ConfigEntry<int> TranslationScope { get; private set; }
        public static ConfigEntry<KeyboardShortcut> BenchmarkHotkey { get; private set; }

        internal void Update()
        {
            if (_inProgress || !Enabled.Value || !BenchmarkHotkey.Value.IsPressed()) return;

            _inProgress = true;
            var path = CorpusPath.Value;
            if (string.IsNullOrEmpty(path?.Trim()))
            {
                Logger.Log(LogLevel.Message | LogLevel.Warning, "Please configure Corpus Path and try again");
                _inProgress = false;
                return;
            }

            if (File.Exists(path))
            {
                Logger.Log(LogLevel.Message | LogLevel.Error, $"Corpus Path must be a directory: {path}");
                _inProgress = false;
                return;
            }

            if (!Directory.Exists(path))
            {
                Logger.Log(LogLevel.Message | LogLevel.Error, $"Corpus Path does not exist: {path}");
                _inProgress = false;
                return;
            }

            StartCoroutine(RunBenchmark());
        }

        internal void Main()
        {
            Enabled = Config.Bind("Settings", "Enabled", true, "Whether the plugin is enabled");
            TranslationScope = Config.Bind("Config", "Scope", -1,
                "Translation scope to use during benchmark (-1 to disable)");
            CorpusPath = Config.Bind("Config", "Corpus Path", string.Empty,
                "Directory containing files to use for benchmarking");
            LoopCount = Config.Bind("Config", "Loop Count", 3,
                "Number of times to process corpus");
            BenchmarkHotkey = Config.Bind("Keyboard Shortcuts", "Benchmark Hotkey",
                new KeyboardShortcut(KeyCode.LeftBracket, KeyCode.LeftAlt),
                "Press to start benchmark (may take some time, application may appear to freeze up)");
        }

        internal IEnumerable<string> GetLinesFromCorpus()
        {
            var path = CorpusPath.Value;

            foreach (var file in Directory.GetFiles(CorpusPath.Value, "*.txt", SearchOption.AllDirectories).Ordered())
            {
                using (var reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line.Trim();
                    }
                }
            }
        }


        internal void TimeTranslation(string originalText, Action onComplete)
        {
            var stopwatch = new Stopwatch();

            void OnComplete(TranslationResult result)
            {
                stopwatch.Stop();
                RecordResult(originalText, result, stopwatch.Elapsed);
                onComplete();
            }

            var scope = TranslationScope.Value;
            stopwatch.Start();
            AutoTranslator.Default.TranslateAsync(originalText, scope, OnComplete);
            TrackTranslationRate();
        }

        private static string GetWorkFileName(string path, string tag, string extension)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
            var result = string.Empty;
            var pluginSlug = PluginName.Replace(" ", "_");

            for (var i = 0; string.IsNullOrEmpty(result) || File.Exists(result); i++)
            {
                result = Path.Combine(path,
                    string.Join(".", new[] {pluginSlug, tag, timestamp, i.ToString(), extension}));
            }

            return result;
        }

        private void TrackTranslationRate(bool startingJob = true)
        {
            var currentFrame = Time.frameCount;
            var lastFrame = currentFrame - 1;

            var currentSecond = (int) Time.time;
            var lastSecond = currentSecond - 1;

            if (currentFrame != _lastQueuedFrame)
            {
                _queuedThisFrame = startingJob ? 1 : 0;
                if (lastFrame == _lastQueuedFrame)
                {
                    _consecutiveFrames++;
                }
                else
                {
                    _consecutiveFrames = startingJob ? 1 : 0;
                }
            }
            else if (startingJob)
            {
                _queuedThisFrame++;
            }

            if (currentSecond != _lastQueuedSecond)
            {
                _queuedThisSecond = startingJob ? 1 : 0;
                if (lastSecond == _lastQueuedSecond)
                {
                    _consecutiveSeconds++;
                }
                else
                {
                    _consecutiveSeconds = startingJob ? 1 : 0;
                }
            }
            else if (startingJob)
            {
                _queuedThisSecond++;
            }

            if (!startingJob) return;
            _lastQueuedFrame = currentFrame;
            _lastQueuedSecond = currentSecond;
        }

        private IEnumerator RunBenchmark()
        {
            Logger.Log(LogLevel.Info | LogLevel.Message, $"Starting benchmark using {CorpusPath.Value}");
            yield return PrimeTranslationEngine();
            yield return Benchmark();
            yield return new WaitForSeconds(3f);
            _inProgress = false;
        }

        private IEnumerator PrimeTranslationEngine()
        {
            yield return null;
            var stillRunning = true;

            Logger.LogDebug("Priming translator");
            AutoTranslator.Default.TranslateAsync("こんにちは", -1, _ => stillRunning = false);
            yield return new WaitUntil(() => !stillRunning);
            yield return null;
            Logger.LogDebug("Translator ready");
        }

        private bool CanStartJob()
        {
            return (_consecutiveFrames < 30 || Time.frameCount > _lastQueuedFrame + 2) &&
                   (_consecutiveSeconds < 30 || (int) Time.time > _lastQueuedSecond + 2) &&
                   Interlocked.Read(ref _outstandingJobs) < MaxOutstandingJobs;
        }

        private IEnumerator WaitUntilJobsBelowThreshold(int threshold, IEnumerator delay = null)
        {
            while (Interlocked.Read(ref _outstandingJobs) >= threshold) yield return delay;
        }

        private IEnumerator WaitForJobsRate()
        {
            var jobThreshold = (int) Math.Ceiling(MaxOutstandingJobs / 2.0);
            var thresholdDelay = new WaitForSecondsRealtime(0.5f);
            var timeDelay = new WaitForSecondsRealtime(1.1f);

            while (true)
            {
                if (_consecutiveFrames >= 30)
                {
                    yield return null;
                }
                else if (_consecutiveSeconds >= 30)
                {
                    yield return timeDelay;
                }
                else if (Interlocked.Read(ref _outstandingJobs) >= jobThreshold)
                {
                    yield return thresholdDelay;
                }
                else if (CanStartJob())
                {
                    yield break;
                }

                TrackTranslationRate(false);
            }
        }

        private IEnumerator Benchmark()
        {
            var count = 0;
            var loopCount = LoopCount.Value;

            _results.Clear();
            for (var i = 0; i < loopCount; i++)
            {
                foreach (var line in GetLinesFromCorpus())
                {
                    if (!CanStartJob())
                    {
                        Logger.LogDebug(
                            $"Inserting delay to avoid spam protection (does not effect benchmark timing) [{count}:{_consecutiveFrames}({_queuedThisSecond}):{_consecutiveSeconds}({_queuedThisSecond}):{_outstandingJobs}]");
                        yield return WaitForJobsRate();
                        TrackTranslationRate(false);
                        Logger.LogDebug(
                            $"Delay complete [{count}:{_consecutiveFrames}({_queuedThisSecond}):{_consecutiveSeconds}({_queuedThisSecond}):{_outstandingJobs}]");
                    }

                    Interlocked.Increment(ref _outstandingJobs);

                    TimeTranslation(line, () => Interlocked.Decrement(ref _outstandingJobs));
                    count++;
                }
            }

            SaveResults();

            var totalTime = _results.Aggregate(TimeSpan.Zero, (sum, result) => sum += result.Elapsed);
            var avgTime = TimeSpan.FromTicks(totalTime.Ticks / _results.Count);
            Logger.LogInfo($"Processed: {_results.Count}");
            Logger.LogInfo($"Succeeded: {_results.Percent(x => x.TranslationSucceeded)}%");
            Logger.LogInfo($"Unchanged: {_results.Percent(x => x.Unchanged)}%");
            Logger.LogInfo($"Total Time: {totalTime}");
            Logger.LogInfo($"Average Time: {avgTime}");

            // reset
            _results.Clear();
        }


        private void RecordResult(string originalText, TranslationResult result, TimeSpan elapsed)
        {
            _results.Add(new BenchmarkResult(originalText, result, elapsed));
        }

        private void SaveResults()
        {
            var output = new StringBuilder();
            output.AppendLine(BenchmarkResult.GetCSVHeaderLine());
            foreach (var result in _results.OrderByDescending(r => r.Elapsed).ThenBy(r => r.OriginalText))
            {
                output.AppendLine(result.GetCSVLine());
            }

            var resultFile = GetWorkFileName(Path.Combine(Paths.CachePath, GUID), "results", "csv");
            var dir = Path.GetDirectoryName(resultFile);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(resultFile, output.ToString());
            Logger.Log(LogLevel.Message | LogLevel.Info, $"Results saved to {resultFile}");
        }
    }
}
