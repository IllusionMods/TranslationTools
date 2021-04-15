using System;
using System.Collections;
using AIProject.Player;
using BepInEx;
using HarmonyLib;
using IllusionMods.Shared;
using Manager;
using UnityEngine.SceneManagement;
using UploaderSystem;
using Scene = UnityEngine.SceneManagement.Scene;

namespace IllusionMods
{
    /// <remarks>
    ///     Uses multi-stage dump against main game application (not Studio). After startup start or load
    ///     save and wait for notification that dump is available.
    /// </remarks>
    [BepInProcess(Constants.MainGameProcessNameSteam)]
    [BepInProcess(Constants.MainGameProcessName)] // IllusionFixes may have changed it
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_INT_TextDump";

        private bool _dataLoaded;

        static TextDump()
        {
            if (typeof(DownloadScene).GetProperty("isSteam", AccessTools.all) != null)
            {
                CurrentExecutionMode = ExecutionMode.Startup;
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
                    "[TextDump] Incorrect plugin for this application. Remove AI_INT_TextDump and use AI_TextDump.");
            }

            TextResourceHelper = CreateHelper<AI_TextResourceHelper>();
            LocalizationDumpHelper = CreatePluginHelper<AI_INT_LocalizationDumpHelper>();
            AssetDumpHelper = CreatePluginHelper<AI_INT_AssetDumpHelper>();

            CheckReadyToDumpChecker = AI_INT_CheckReadyToDump;

            TextDumpAwake += AI_INT_TextDumpAwake;
            TextDumpLevelComplete += AI_INT_TextDumpLevelComplete;
        }

        private void AI_INT_TextDumpLevelComplete(TextDump sender, EventArgs eventArgs)
        {
            if (DumpLevelCompleted >= DumpLevelMax)
            {
                NotificationMessage = string.Empty;
            }
            else if (DumpLevelCompleted > 0)
            {
                NotificationMessage =
                    "Localizations not loaded. Start/Load game and play until you have control of the player";
            }
        }


        private void AI_INT_TextDumpAwake(TextDump sender, EventArgs eventArgs)
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }


        private IEnumerator AI_INT_CheckReadyToDump()
        {
            Logger.LogDebug("CheckReadyToDump: waiting until dump 2 completes");
            while (DumpLevelCompleted < 2) yield return CheckReadyToDumpDelay;

            Logger.LogDebug("CheckReadyToDump: waiting dataLoaded");
            while (!_dataLoaded) yield return CheckReadyToDumpDelay;

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

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Localize.GetHName");
            string dummy = null;
            while (string.IsNullOrEmpty(dummy))
            {
                yield return CheckReadyToDumpDelay;
                dummy = Singleton<Resources>.Instance.Localize.GetHName(1, 1);
            }

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


            Logger.LogDebug("CheckReadyToDump: waiting for remaining dumps");
            while (DumpLevelReady < DumpLevelMax)
            {
                if (DumpLevelReady <= DumpLevelCompleted)
                {
                    DumpLevelReady++;
                    Logger.LogDebug($"CheckReadyToDump: level {DumpLevelReady} ready!");
                }

                yield return CheckReadyToDumpDelay;
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Logger?.DebugLogDebug($"Loaded: {arg0.name}");

            if (!string.IsNullOrEmpty(arg0.name) && arg0.name.StartsWith("map_") && arg0.name.EndsWith("_data"))
            {
                _dataLoaded = true;
            }

            if (DumpLevelReady < 2 && arg0.name == "Title")
            {
                DumpLevelReady = 2;
            }
        }
    }
}
