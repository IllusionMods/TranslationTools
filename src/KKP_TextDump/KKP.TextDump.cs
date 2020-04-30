using System;
using System.Collections;
using BepInEx;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using IllusionMods.Shared;
using Scene = UnityEngine.SceneManagement.Scene;

namespace IllusionMods
{
    /// <remarks>
    ///     Uses multi-stage dump against main game application (not Studio). After startup start or load
    ///     save and wait for notification that dump is available.
    /// </remarks>
    [BepInProcess(Constants.MainGameProcessNameSteam)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KKP_TextDump";


        static TextDump()
        {
            DumpLevelMax = 3;
            CurrentExecutionMode = ExecutionMode.BeforeFirstLoad;
        }

        public TextDump()
        {
            Logger = base.Logger;
            TextResourceHelper = new KK_TextResourceHelper();
            AssetDumpHelper = new KKP_AssetDumpHelper(this);
            LocalizationDumpHelper = new KKP_LocalizationDumpHelper(this);

            CheckReadyToDumpChecker = KKP_CheckReadyToDump;

            TextDumpAwake += KKP_TextDumpAwake;
            TextDumpLevelComplete += KKP_TextDumpLevelComplete;
        }

        private void KKP_TextDumpLevelComplete(TextDump sender, EventArgs eventArgs)
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

        private void KKP_TextDumpAwake(TextDump sender, EventArgs eventArgs)
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void KKP_TextDumpUpdate(TextDump sender, EventArgs eventArgs) { }


        private static IEnumerator KKP_CheckReadyToDump()
        {
            yield return new WaitForSeconds(1);
            Logger.LogDebug("CheckReadyToDump: waiting on level 2 dump completes");
            while (DumpLevelCompleted < 2) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting for Localize.Translate.Manager");
            while (!Localize.Translate.Manager.initialized) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game");
            while (!Singleton<Game>.IsInstance()) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.actScene");
            while (Singleton<Game>.Instance?.actScene == null) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.actScene.Player");
            while (Singleton<Game>.Instance?.actScene?.Player == null) yield return new WaitForSeconds(1);

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.Player.isActive");
            while (!Singleton<Game>.Instance.actScene.Player.isActive) yield return new WaitForSeconds(1);

            while (Singleton<Manager.Scene>.Instance.IsNowLoading || Singleton<Manager.Scene>.Instance.IsNowLoadingFade)
            {
                yield return new WaitForSeconds(1);
            }

            Logger.LogDebug($"CheckReadyToDump: waiting until {DumpLevelMax - 1} completes");
            while (DumpLevelCompleted < DumpLevelMax - 1) yield return new WaitForSeconds(1);


            Logger.LogDebug("CheckReadyToDump: ready!");

            DumpLevelReady = DumpLevelMax;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (DumpLevelReady >= 2 || arg0.name != "Action") return;

            DumpLevelReady = 2;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
        }
    }
}
