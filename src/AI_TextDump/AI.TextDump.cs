using System;
using System.Collections;
using BepInEx;
using IllusionMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using static IllusionMods.TextDump.Helpers;

namespace IllusionMods
{
    /// <remarks>
    ///     Uses studio executable for single stage dump.
    /// </remarks>
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_TextDump";
        private TranslationCount _lastDelta = new TranslationCount();
        private TranslationCount _lastTotal = new TranslationCount();
        private int _stableCount = 0;
        private bool _waitOnRetry = false;

        private static readonly string[] AssetPathsToWaitOn = new[]
        {
            "list/h/sound/voice",
            "list/characustom",
            "adv/scenario"
        };

        private bool _startupLoaded;

        static TextDump()
        {
            CurrentExecutionMode = ExecutionMode.BeforeFirstLoad;
            DumpLevelMax = 3;
        }

        public TextDump()
        {
            TextResourceHelper = CreateHelper<AI_TextResourceHelper>();
            AssetDumpHelper = CreatePluginHelper<AI_AssetDumpHelper>();
            LocalizationDumpHelper = CreatePluginHelper<AI_LocalizationDumpHelper>();

            CheckReadyToDumpChecker = AI_CheckReadyToDump;

            TextDumpAwake += AI_TextDumpAwake;
            TextDumpLevelComplete += AI_TextDumpLevelComplete;
        }

        private IEnumerator RetryDelay(float seconds)
        {
            _waitOnRetry = true;
            yield return new WaitForSeconds(seconds);
            _waitOnRetry = false;
        }

        private IEnumerator AI_CheckReadyToDump()
        {
            Logger.LogDebug("CheckReadyToDump: waiting until dump 1 completes");
            while (DumpLevelCompleted < 1) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting for Lobby to load");

            while (!_startupLoaded) yield return CheckReadyToDumpDelay;

            SceneManager.sceneLoaded -= AI_sceneLoaded;


            Logger.LogDebug("CheckReadyToDump: waiting for start screen");

            while (Singleton<Manager.Scene>.Instance == null) yield return CheckReadyToDumpDelay;
            //while (Manager.Scene.IsNowLoadingFade) yield return CheckReadyToDumpDelay;

            //Logger.LogDebug("CheckReadyToDump: waiting for Manager.Voice");

            //while (Manager.Voice..infoTable == null || Voice.infoTable.Count == 0) yield return CheckReadyToDumpDelay;

            Logger.LogDebug($"CheckReadyToDump: waiting for Manager.GameSystem");
            while (Singleton<Manager.GameSystem>.Instance == null) yield return CheckReadyToDumpDelay;

            Logger.LogDebug($"Language = {Singleton<Manager.GameSystem>.Instance.language}");


            foreach (var pth in AssetPathsToWaitOn)
            {
                Logger.LogDebug($"CheckReadyToDump: waiting until we can list asset bundles for {pth}");

                while (true)
                {
                    var count = 0;
                    try
                    {
                        count = GetAssetBundleNameListFromPath(pth).Count;
                    }
                    catch
                    {
                        count = 0;
                    }

                    if (count != 0) break;

                    try
                    {
                        count = GetAssetBundleNameListFromPath(pth, true).Count;
                    }
                    catch
                    {
                        count = 0;
                    }

                    if (count != 0) break;

                    yield return CheckReadyToDumpDelay;
                }
            }

            Logger.LogDebug("CheckReadyToDump: waiting for remaining dumps");
            while (DumpLevelReady < DumpLevelMax)
            {
                if (DumpLevelReady <= DumpLevelCompleted)
                {
                    if (_waitOnRetry) Logger.LogDebug("CheckReadyToDump: waiting for retry delay");
                    while (_waitOnRetry) yield return CheckReadyToDumpDelay;
                    DumpLevelReady++;
                    Logger.LogDebug($"CheckReadyToDump: level {DumpLevelReady} ready!");
                }

                yield return CheckReadyToDumpDelay;
            }


        }

        private void AI_TextDumpAwake(TextDump sender, EventArgs eventArgs)
        {
            SceneManager.sceneLoaded += AI_sceneLoaded;
        }

        private void AI_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Logger.LogFatal($"{scene.name} {loadSceneMode}");
            if ((IsStudio && scene.name == "Studio" && loadSceneMode == LoadSceneMode.Single) ||
                (!IsStudio && scene.name == "map_title" && loadSceneMode == LoadSceneMode.Additive))
            {
                _startupLoaded = true;
            }

        }

        private void AI_TextDumpLevelComplete(TextDump sender, EventArgs eventArgs)
        {

            var delta = _total - _lastTotal;

            if (DumpLevelCompleted >= DumpLevelMax)
            {
                NotificationMessage = string.Empty;


                if (_total == _lastTotal)
                {
                    _stableCount++;
                }
                else
                {
                    _lastTotal = _total;
                    if (_stableCount != 0) _lastDelta = delta;
                    _stableCount = 0;
                }

                if (_stableCount < 3)
                {
                    StartCoroutine(RetryDelay(10));
                    if (_stableCount == 0)
                    {
                        NotificationMessage = $"Number of translations found is continuing to change ({delta})";
                    }
                    else
                    {
                        NotificationMessage = $"Number of translations unchanged";

                    }


                    NotificationMessage +=
                        $", will keep re-dumping until it's stable for {3 - _stableCount} more cycle(s)";
                    DumpLevelCompleted--;
                    DumpLevelReady = DumpLevelCompleted;
                }
            }
            else if (DumpLevelCompleted > 0)
            {
                NotificationMessage =
                    "Multiple brute-force dump attempts are required, please wait until you see a message saying files are available";
            }
        }
    }
}
