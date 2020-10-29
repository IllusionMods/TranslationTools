using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using IllusionMods.Shared;
using BepInEx.Configuration;
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

        public static ConfigEntry<KeyboardShortcut> ManualDumpHotkey { get; private set; }

        private static bool _waitForTitleUnload = false;

        internal enum DumpLevels
        {
            Initial = 1,
            Secondary = 2,
            Main = 3,
            Manual = 4,
        }

        static TextDump()
        {
            DumpLevelMax = (int) DumpLevels.Manual;
            CurrentExecutionMode = ExecutionMode.BeforeFirstLoad;
            //CurrentAssetDumpMode = AssetDumpMode.FirstAndLastOnly;
        }

        public TextDump()
        {
            Logger = base.Logger;
            TextResourceHelper = CreateHelper<KK_TextResourceHelper>();
            AssetDumpHelper = CreatePluginHelper<KKP_AssetDumpHelper>();
            LocalizationDumpHelper = CreatePluginHelper<KKP_LocalizationDumpHelper>();

            CheckReadyToDumpChecker = KKP_CheckReadyToDump;

            TextDumpAwake += KKP_TextDumpAwake;
            TextDumpLevelComplete += KKP_TextDumpLevelComplete;
        }

        private void KKP_TextDumpLevelComplete(TextDump sender, EventArgs eventArgs)
        {
            _waitForTitleUnload |= (DumpLevelCompleted > 0 && DumpLevelCompleted < (int) DumpLevels.Main);

            if (DumpLevelCompleted < (int) DumpLevels.Initial)
            {
                NotificationMessage = string.Empty;
            }
            /*
            else if (DumpLevelCompleted < (int) DumpLevels.Maker)
            {
                NotificationMessage =
                    "Localizations not fully loaded. Please launch the character maker.";
            }
            */
            else if (DumpLevelCompleted < (int) DumpLevels.Main)
            {
                NotificationMessage =
                    "Localizations not fully loaded. Start/Load main game and play until you have control of the player";
            }

            if (DumpLevelCompleted == (int)DumpLevels.Main)
            {
                TextDumpUpdate += TextDump_TextDumpUpdate;
                NotificationMessage =
                    $"Visit as many dialogs/screens as possible then press {ManualDumpHotkey.Value} to execute dump";
            }

            if (DumpLevelCompleted < DumpLevelMax) return;

            NotificationMessage = string.Empty;
            SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            TextDumpUpdate -= TextDump_TextDumpUpdate;
        }

        private void TextDump_TextDumpUpdate(TextDump sender, EventArgs eventArgs)
        {
            if (Enabled.Value && DumpLevelCompleted == (int) DumpLevels.Main &&
                DumpLevelReady < DumpLevelMax &&
                ManualDumpHotkey.Value.IsPressed())
            {
                DumpLevelReady = (int)DumpLevels.Manual;
            }
        }

        private void KKP_TextDumpAwake(TextDump sender, EventArgs eventArgs)
        {
            ManualDumpHotkey = Config.Bind("Keyboard Shortcuts", "Dump Translations",
                new KeyboardShortcut(KeyCode.F9, KeyCode.LeftControl, KeyCode.LeftAlt),
                "Once you have control of the player and have visited all the screens you'd like to collect press this key to start dump");
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            Logger.LogDebug($"UNLOAD SCENE: {arg0.name}");
            if (_waitForTitleUnload && arg0.name == "Title")
            {
                _waitForTitleUnload = false;
            }
        }

        private void KKP_TextDumpUpdate(TextDump sender, EventArgs eventArgs) { }


        private IEnumerator KKP_CheckReadyToDump()
        {
            yield return CheckReadyToDumpDelay;

            while (DumpLevelReady < (int) DumpLevels.Secondary)
            {
                if (DumpLevelCompleted < DumpLevelReady)
                {
                    Logger.LogDebug($"CheckReadyToDump: waiting on {(DumpLevels)DumpLevelReady} dump");
                    while (DumpLevelCompleted < DumpLevelReady) yield return CheckReadyToDumpDelay;
                    continue;
                }

                if (DumpLevelCompleted == (int) DumpLevels.Secondary - 1)
                {
                    Logger.LogDebug($"CheckReadyToDump: waiting to leave title screen");
                    while (_waitForTitleUnload) yield return CheckReadyToDumpDelay;
                }

                DumpLevelReady++;
                Logger.LogDebug($"CheckReadyToDump: Ready for {(DumpLevels)DumpLevelReady}");
            }

            Logger.LogDebug("CheckReadyToDump: waiting for Localize.Translate.Manager");
            while (!Localize.Translate.Manager.initialized) yield return CheckReadyToDumpDelay;

            /*
            while (DumpLevelReady < (int) DumpLevels.Maker)
            {
                if (DumpLevelCompleted < DumpLevelReady)
                {
                    Logger.LogDebug($"CheckReadyToDump: waiting on {(DumpLevels) DumpLevelReady} dump");
                    while (DumpLevelCompleted < DumpLevelReady) yield return CheckReadyToDumpDelay;
                    continue;
                }

                Logger.LogDebug("CheckReadyToDump: waiting for ChaCustom.CustomBase (maker launch)");
                while (Singleton<ChaCustom.CustomBase>.Instance == null) yield return CheckReadyToDumpDelay;

                while (Singleton<Manager.Scene>.Instance.IsNowLoading ||
                       Singleton<Manager.Scene>.Instance.IsNowLoadingFade)
                {
                    yield return CheckReadyToDumpDelay;
                }

                DumpLevelReady++;
                Logger.LogDebug($"CheckReadyToDump: Ready for {(DumpLevels) DumpLevelReady}");
            }
            */

            while (DumpLevelReady < (int)DumpLevels.Secondary)
            {
                if (DumpLevelCompleted < DumpLevelReady)
                {
                    Logger.LogDebug($"CheckReadyToDump: waiting on {(DumpLevels)DumpLevelReady} dump");
                    while (DumpLevelCompleted < DumpLevelReady) yield return CheckReadyToDumpDelay;
                    continue;
                }

                Logger.LogDebug($"CheckReadyToDump: waiting to leave title screen");
                while (_waitForTitleUnload) yield return CheckReadyToDumpDelay;
                DumpLevelReady++;
            }

            while (DumpLevelReady < (int) DumpLevels.Main)
            {
                if (DumpLevelCompleted < DumpLevelReady)
                {
                    Logger.LogDebug($"CheckReadyToDump: waiting on {(DumpLevels)DumpLevelReady} dump");
                    while (DumpLevelCompleted < DumpLevelReady) yield return CheckReadyToDumpDelay;
                    continue;
                }

                Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game");
                while (!Singleton<Game>.IsInstance()) yield return CheckReadyToDumpDelay;

                Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.actScene");
                while (Singleton<Game>.Instance?.actScene == null) yield return CheckReadyToDumpDelay;

                Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.actScene.Player");
                while (Singleton<Game>.Instance?.actScene?.Player == null) yield return CheckReadyToDumpDelay;

                Logger.LogDebug("CheckReadyToDump: waiting on Manager.Game.Player.isActive");
                while (!Singleton<Game>.Instance.actScene.Player.isActive) yield return CheckReadyToDumpDelay;

                while (Singleton<Manager.Scene>.Instance.IsNowLoading ||
                       Singleton<Manager.Scene>.Instance.IsNowLoadingFade)
                {
                    yield return CheckReadyToDumpDelay;
                }

                DumpLevelReady++;
                Logger.LogDebug($"CheckReadyToDump: Ready for {(DumpLevels)DumpLevelReady}");
            }

            while (DumpLevelReady < (int)DumpLevels.Main)
            {
                Logger.LogDebug($"CheckReadyToDump: Waiting for {(DumpLevels)DumpLevelReady} to complete");
                while (DumpLevelCompleted < DumpLevelReady) yield return CheckReadyToDumpDelay;
                DumpLevelReady++;
                Logger.LogDebug($"CheckReadyToDump: Ready for {(DumpLevels)DumpLevelReady}");
            }

            //Logger.LogDebug($"CheckReadyToDump: everything is ready!");
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Logger.LogDebug($"LOAD SCENE: {arg0.name}");
        }
    }
}
