using System;
using System.Collections;
using BepInEx;
using IllusionMods.Shared.TextDumpBase;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ActionGame.Communication.Info;
using Scene = UnityEngine.SceneManagement.Scene;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump
    {
        public const string PluginNameInternal = "KKS_TextDump";

        private TranslationCount _lastDelta = new TranslationCount();
        private TranslationCount _lastTotal = new TranslationCount();

        private int _stableCount;
        private readonly bool[] _startupScenesLoaded;
        private bool _waitOnRetry;

        private static readonly string[] StartupScenes;

        static TextDump()
        {
            StartupScenes = new [] {"Init", "Logo", "Title"};
            CurrentExecutionMode = ExecutionMode.BeforeFirstLoad;
            DumpLevelMax = 4;
        }

        public TextDump()
        {
            try
            {
                _startupScenesLoaded = new bool[StartupScenes.Length];
                SetTextResourceHelper(CreateHelper<KKS_TextResourceHelper>());
                AssetDumpHelper = CreatePluginHelper<KKS_AssetDumpHelper>();
                LocalizationDumpHelper = CreatePluginHelper<KKS_LocalizationDumpHelper>();

                CheckReadyToDumpChecker = KKS_CheckReadyToDump;

                TextDumpAwake += KKS_TextDumpAwake;
                TextDumpLevelComplete += KKS_TextDumpComplete;
            }
            catch (Exception err)
            {
                Enabled.Value = false;
                Logger.LogError($"Disabling {PluginName}: Unexpected error during initialization: {err.Message}");
                throw;
            }
        }

        private void KKS_TextDumpAwake(BaseTextDumpPlugin sender, EventArgs eventArgs)
        {
            SceneManager.sceneLoaded += KKS_sceneLoaded;
        }

        private void KKS_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var index = Array.IndexOf(StartupScenes, scene.name);
            if (index == -1) return;
            _startupScenesLoaded[index] = true;
        }

        private IEnumerator KKS_CheckReadyToDump()
        {
            Logger.LogFatal("CheckReadyToDump: waiting until dump 1 completes");
            while (DumpLevelCompleted < 1) yield return CheckReadyToDumpDelay;


            var startupSceneCount = _startupScenesLoaded.Length;
            for (var i = 0; i < startupSceneCount; i++)
            {
                Logger.LogFatal(
                    $"CheckReadyToDump: waiting for startup scenes {i}/{startupSceneCount}: {StartupScenes[i]}");
                while (!_startupScenesLoaded[i]) yield return CheckReadyToDumpDelay;
            }

            SceneManager.sceneLoaded -= KKS_sceneLoaded;


            Logger.LogFatal("CheckReadyToDump: waiting for menu to finish");

            while (Singleton<Manager.Scene>.Instance == null) yield return CheckReadyToDumpDelay;
            while (Manager.Scene.IsNowLoadingFade) yield return CheckReadyToDumpDelay;

            Logger.LogFatal("CheckReadyToDump: waiting for Manager.Voice");

            while (Voice.infoTable == null || Voice.infoTable.Count == 0) yield return CheckReadyToDumpDelay;


#if false
            foreach (var pth in AssetPathsToWaitOn)
            {
                Logger.LogFatal($"CheckReadyToDump: waiting until we can list asset bundles for {pth}");

                while (true)
                {
                    var count = 0;
                    try
                    {
                        count = Helpers.GetAssetBundleNameListFromPath(pth).Count;
                    }
                    catch
                    {
                        count = 0;
                    }

                    if (count != 0) break;

                    try
                    {
                        count = Helpers.GetAssetBundleNameListFromPath(pth, true).Count;
                    }
                    catch
                    {
                        count = 0;
                    }

                    if (count != 0) break;

                    yield return CheckReadyToDumpDelay;
                }
            }
#endif
            Logger.LogFatal("CheckReadyToDump: waiting for remaining dumps");
            while (DumpLevelReady < DumpLevelMax)
            {
                if (DumpLevelReady <= DumpLevelCompleted)
                {
                    if (_waitOnRetry) Logger.LogFatal("CheckReadyToDump: waiting for retry delay");
                    while (_waitOnRetry) yield return CheckReadyToDumpDelay;
                    DumpLevelReady++;
                    Logger.LogFatal($"CheckReadyToDump: level {DumpLevelReady} ready!");
                }

                yield return CheckReadyToDumpDelay;
            }
        }

        private void KKS_TextDumpComplete(BaseTextDumpPlugin sender, EventArgs eventArgs)
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
                        NotificationMessage = "Number of translations unchanged";
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

        private IEnumerator RetryDelay(float seconds)
        {
            _waitOnRetry = true;
            yield return new WaitForSeconds(seconds);
            _waitOnRetry = false;
        }

        protected override Version GetGameVersion()
        {
            return GameSystem.GameVersion;
        }
    }
}
