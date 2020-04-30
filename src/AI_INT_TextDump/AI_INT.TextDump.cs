using System;
using System.Collections;
using AIProject.Player;
using BepInEx;
using HarmonyLib;
using IllusionMods.Shared;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UploaderSystem;
using Resources = Manager.Resources;
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

            TextResourceHelper = new AI_TextResourceHelper();
            LocalizationDumpHelper = new AI_INT_LocalizationDumpHelper(this);
            AssetDumpHelper = new AI_INT_AssetDumpHelper(this);

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
            while (DumpLevelCompleted < 2) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting dataLoaded");
            while (!_dataLoaded) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources");
            while (!Singleton<Resources>.IsInstance()) yield return new WaitForSeconds(1);
            while (!Singleton<Resources>.Instance.isActiveAndEnabled) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AgentProfile");
            while (Singleton<Resources>.Instance?.AgentProfile is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AnimalDefinePack");
            while (Singleton<Resources>.Instance?.AnimalDefinePack is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AnimalTable");
            while (Singleton<Resources>.Instance?.AnimalTable is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.PlayerProfile");
            while (Singleton<Resources>.Instance?.PlayerProfile is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.PopupInfo");
            while (Singleton<Resources>.Instance?.PopupInfo is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Sound");
            while (Singleton<Resources>.Instance?.Sound is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.SoundPack");
            while (Singleton<Resources>.Instance?.SoundPack is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.StatusProfile");
            while (Singleton<Resources>.Instance?.StatusProfile is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.WaypointDataList");
            while (Singleton<Resources>.Instance?.WaypointDataList is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.HSceneTable");
            while (Singleton<Resources>.Instance?.HSceneTable is null) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Localize.GetHName");
            string dummy = null;
            while (string.IsNullOrEmpty(dummy))
            {
                yield return new WaitForSeconds(1);
                dummy = Singleton<Resources>.Instance.Localize.GetHName(1, 1);
            }

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.itemIconTables");
            while (Singleton<Resources>.Instance?.itemIconTables is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Map");
            while (Singleton<Resources>.Instance?.Map is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.LocomotionProfile");
            while (Singleton<Resources>.Instance?.LocomotionProfile is null) yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.MerchantProfile");
            while (Singleton<Resources>.Instance?.MerchantProfile is null) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.GameInfo");
            while (!Singleton<Resources>.Instance.GameInfo.initialized) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Simulator");
            while (Singleton<Map>.Instance?.Simulator is null) yield return new WaitForSeconds(1);
            while (!Singleton<Map>.Instance.Simulator.IsActiveSimElement) yield return new WaitForSeconds(1);
            /*
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Simulator.EnabledTimeProgression");
            while (!Singleton<Manager.Map>.Instance.Simulator.EnabledTimeProgression) yield return new WaitForSeconds(1);
            */

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Player");
            while (Singleton<Map>.Instance?.Player is null) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Player.PlayerController");
            while (Singleton<Map>.Instance?.Player?.PlayerController is null) yield return new WaitForSeconds(1);

            /**/
            Logger.LogDebug(
                "CheckReadyToDump: waiting on Manager.Map.Instance.Player.PlayerController.State to be Normal");
            while (!(Singleton<Map>.Instance?.Player?.PlayerController.State is Normal))
            {
                yield return new WaitForSeconds(1);
            }
            /**/

            Logger.LogDebug("CheckReadyToDump: waiting for scene to finish loading");
            while (Singleton<Manager.Scene>.Instance.IsNowLoading || Singleton<Manager.Scene>.Instance.IsNowLoadingFade)
            {
                yield return new WaitForSeconds(1);
            }


            Logger.LogDebug("CheckReadyToDump: waiting for remaining dumps");
            while (DumpLevelReady < DumpLevelMax)
            {
                if (DumpLevelReady <= DumpLevelCompleted)
                {
                    DumpLevelReady++;
                    Logger.LogDebug($"CheckReadyToDump: level {DumpLevelReady} ready!");
                }

                yield return new WaitForSeconds(1);
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Logger?.LogWarning($"Loaded: {arg0.name}");

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
