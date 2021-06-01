using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using AIProject.Player;
using BepInEx;
using HarmonyLib;
using IllusionMods.Shared;
using IllusionMods.Shared.TextDumpBase;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UploaderSystem;
using static IllusionMods.TextDump.Helpers;
using Resources = Manager.Resources;
using Scene = UnityEngine.SceneManagement.Scene;


namespace IllusionMods
{
    /// <remarks>
    ///     Uses multi-stage dump against main game application (not Studio). After startup start or load
    ///     save and wait for notification that dump is available.
    /// </remarks>
    [BepInProcess(Constants.MainGameProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump 
    {
        public const string PluginNameInternal = "AI_TextDump";

        private static readonly string[] AssetPathsToWaitOn =
        {
            "list/h/sound/voice",
            "list/characustom",
            "adv/scenario"
        };

        private bool _dataLoaded;
        private TranslationCount _lastDelta = new TranslationCount();
        private TranslationCount _lastTotal = new TranslationCount();
        private int _stableCount;

        private bool _startupLoaded;
        private bool _waitOnRetry;

        [SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline",
            Justification = "Dynamic initialization")]
        static TextDump()
        {
            if (typeof(DownloadScene).GetProperty("isSteam", AccessTools.all) == null)
            {
                CurrentExecutionMode = ExecutionMode.BeforeFirstLoad;
                DumpLevelMax = 4;
            }
            else
            {
                CurrentExecutionMode = ExecutionMode.Other;
                DumpLevelMax = 0;
            }
        }

        public TextDump()
        {
            if (CurrentExecutionMode == ExecutionMode.Other && DumpLevelMax == 0)
            {
                InitPluginSettings();
                Enabled.Value = false;
                Logger.LogFatal(
                    "[TextDump] Incorrect plugin for this application. Remove AI_TextDump and use AI_INT_TextDump.");
            }

            SetTextResourceHelper(CreateHelper<AI_TextResourceHelper>());
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
            Logger.LogDebug("CheckReadyToDump: waiting until dump 2 completes");
            while (DumpLevelCompleted < 2) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting dataLoaded");
            while (!_dataLoaded) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting for Lobby to load");
            while (!_startupLoaded) yield return CheckReadyToDumpDelay;

            SceneManager.sceneLoaded -= AI_sceneLoaded;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources");
            while (!Singleton<Resources>.IsInstance() || Singleton<Resources>.Instance is null)
                yield return CheckReadyToDumpDelay;
            while (!Singleton<Resources>.Instance.isActiveAndEnabled) yield return CheckReadyToDumpDelay;
            var resources = Singleton<Resources>.Instance;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AgentProfile");
            while (resources.AgentProfile is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AnimalDefinePack");
            while (resources.AnimalDefinePack is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AnimalTable");
            while (resources.AnimalTable is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.PlayerProfile");
            while (resources.PlayerProfile is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.PopupInfo");
            while (resources.PopupInfo is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Sound");
            while (resources.Sound is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.SoundPack");
            while (resources.SoundPack is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.StatusProfile");
            while (resources.StatusProfile is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.WaypointDataList");
            while (resources.WaypointDataList is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.HSceneTable");
            while (resources.HSceneTable is null) yield return CheckReadyToDumpDelay;


            Logger.LogDebug("CheckReadyToDump: waiting for start screen");

            while (Singleton<Manager.Scene>.Instance is null) yield return CheckReadyToDumpDelay;
            //while (Manager.Scene.IsNowLoadingFade) yield return CheckReadyToDumpDelay;

            //Logger.LogDebug("CheckReadyToDump: waiting for Manager.Voice");

            //while (Manager.Voice..infoTable == null || Voice.infoTable.Count == 0) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting for Manager.GameSystem");
            while (Singleton<GameSystem>.Instance is null) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.itemIconTables");
            while (resources.itemIconTables is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Map");
            while (resources.Map is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.LocomotionProfile");
            while (resources.LocomotionProfile is null) yield return CheckReadyToDumpDelay;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.MerchantProfile");
            while (resources.MerchantProfile is null) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.GameInfo");
            while (resources.GameInfo is null || !resources.GameInfo.initialized) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Simulator");
            while (Singleton<Map>.Instance is null) yield return CheckReadyToDumpDelay;
            while (Singleton<Map>.Instance.Simulator is null) yield return CheckReadyToDumpDelay;
            while (!Singleton<Map>.Instance.Simulator.IsActiveSimElement) yield return CheckReadyToDumpDelay;
            /*
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Simulator.EnabledTimeProgression");
            while (!Singleton<Manager.Map>.Instance.Simulator.EnabledTimeProgression) yield return CheckReadyToDumpDelay;
            */

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Player");
            while (Singleton<Map>.Instance.Player is null) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Player.PlayerController");
            while (Singleton<Map>.Instance.Player.PlayerController is null) yield return CheckReadyToDumpDelay;

            /**/
            Logger.LogDebug(
                "CheckReadyToDump: waiting on Manager.Map.Instance.Player.PlayerController.State to be Normal");
            while (!(Singleton<Map>.Instance.Player.PlayerController.State is Normal))
            {
                yield return CheckReadyToDumpDelay;
            }
            /**/

            Logger.LogDebug("CheckReadyToDump: waiting for scene to finish loading");
            while (Singleton<Manager.Scene>.Instance.IsNowLoading || Singleton<Manager.Scene>.Instance.IsNowLoadingFade)
            {
                yield return CheckReadyToDumpDelay;
            }

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
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
                    {
                        count = 0;
                    }
#pragma warning restore CA1031 // Do not catch general exception types

                    if (count != 0) break;

                    try
                    {
                        count = GetAssetBundleNameListFromPath(pth, true).Count;
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
                    {
                        count = 0;
                    }
#pragma warning restore CA1031 // Do not catch general exception types

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

            Logger.LogDebug($"Language = {Singleton<GameSystem>.Instance.language}");
        }

        private void AI_TextDumpAwake(BaseTextDumpPlugin sender, EventArgs eventArgs)
        {
            SceneManager.sceneLoaded += AI_sceneLoaded;
        }

        private void AI_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            Logger.LogDebug($"{scene.name} {loadSceneMode}");
            if (IsStudio && scene.name == "Studio" && loadSceneMode == LoadSceneMode.Single ||
                !IsStudio && scene.name == "map_title" && loadSceneMode == LoadSceneMode.Additive)
            {
                _startupLoaded = true;
            }

            if (!string.IsNullOrEmpty(scene.name) && scene.name.StartsWith("map_") && scene.name.EndsWith("_data"))
            {
                _dataLoaded = true;
            }

            if (DumpLevelReady < 2 && scene.name == "Title")
            {
                DumpLevelReady = 2;
            }
        }

        private void AI_TextDumpLevelComplete(BaseTextDumpPlugin sender, EventArgs eventArgs)
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

                if (_stableCount >= 3) return;
                StartCoroutine(RetryDelay(10));
                NotificationMessage = _stableCount == 0 ?
                    $"Number of translations found is continuing to change ({delta})" : 
                    "Number of translations unchanged";

                NotificationMessage +=
                    $", will keep re-dumping until it's stable for {3 - _stableCount} more cycle(s)";
                DumpLevelCompleted--;
                DumpLevelReady = DumpLevelCompleted;
            }
            else if (DumpLevelCompleted > 0)
            {
                NotificationMessage =
                    "Multiple brute-force dump attempts are required, Start/Load game and play until you have control of the player and wait until you see a message saying files are available";
            }
        }
    }
}
