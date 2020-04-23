using System;
using System.Collections;
using BepInEx;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
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

            CheckReadyToDumpChecker = KKP_CheckReadyToDump;

            TextDumpAwake += KKP_TextDumpAwake;
            TextDumpUpdate += KKP_TextDumpUpdate;
        }


        private void KKP_TextDumpAwake(TextDump sender, EventArgs eventArgs)
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void KKP_TextDumpUpdate(TextDump sender, EventArgs eventArgs)
        {
            switch (DumpLevelCompleted)
            {
                case 1:
                case 2:
                    CheckReadyNotificationMessage =
                        "Localizations not loaded. Start/Load game and play until you have control of the player";
                    break;

                default:
                    CheckReadyNotificationMessage = string.Empty;
                    break;
            }
        }


        private static IEnumerator KKP_CheckReadyToDump()
        {
            yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on level 2 dump completes");
            yield return new WaitUntil(() => DumpLevelCompleted >= 2);

            Logger.LogDebug("CheckReadyToDump: waiting for Localize.Translate.Manager");
            while (!Localize.Translate.Manager.initialized) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game");
            while (!Singleton<Game>.IsInstance()) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.actScene");
            while (Singleton<Game>.Instance?.actScene == null) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.actScene.Player");
            while (Singleton<Game>.Instance?.actScene?.Player == null) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.Player.isActive");
            while (!Singleton<Game>.Instance.actScene.Player.isActive) yield return null;

            Logger.LogDebug($"CheckReadyToDump: waiting until {DumpLevelMax - 1} completes");
            yield return new WaitUntil(() => DumpLevelCompleted >= DumpLevelMax - 1);

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
