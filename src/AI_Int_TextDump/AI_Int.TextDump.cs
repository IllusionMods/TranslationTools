using BepInEx;
using System.Collections;
using UnityEngine.SceneManagement;

namespace IllusionMods
{
    /// <summary>
    /// Dumps untranslated text to .txt files
    /// </summary>
    //[BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_INT_TextDump";

        private const BepInEx.Logging.LogLevel WarnMessage = BepInEx.Logging.LogLevel.Warning | BepInEx.Logging.LogLevel.Message;

        private static bool almostLoaded = false;
        private static bool stuffLoaded = false;
        private static bool checksRunning = false;
        private bool resetDumpStatus = false;
        private float nextNotify = 0;

        static TextDump()
        {
            readyToDump = false;
            CurrentExecutionMode = TextDump.ExecutionMode.Startup;
            WriteOnDump = false;
        }

        public TextDump()
        {
            textResourceHelper = new AI_TextResourceHelper();
            localizationDumpHelper = new AI_Int_LocalizationDumpHelper(this);
            assetDumpHelper = new AI_Int_AssetDumpHelper(this);
        }

        internal void Awake()
        {
            Logger = base.Logger;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += this.SceneManager_sceneUnloaded;
            SceneManager.activeSceneChanged += this.SceneManager_activeSceneChanged;
        }

        internal void Update()
        {
            bool shouldDump = false;
            if (Enabled.Value)
            {
                if (CurrentExecutionMode == TextDump.ExecutionMode.Other && !DumpStarted && resetDumpStatus)
                {
                    shouldDump = true;
                }
                else if (CurrentExecutionMode == TextDump.ExecutionMode.Startup && resetDumpStatus && !DumpStarted)
                {
                    shouldDump = true;
                }
            }
            if (!resetDumpStatus && DumpCompleted)
            {
                // set back to initial state so second pass can run
                resetDumpStatus = true;
                DumpStarted = DumpCompleted = false;
            }
            if (shouldDump)
            {
                if (IsReadyToDump())
                {
                    Logger.LogInfo("[TextDump] Starting dump from Update");
                    WriteOnDump = true;
                    DumpText();
                    SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                }
                else
                {
                    if (!checksRunning)
                    {
                        StartCoroutine(CheckReadyToDump());
                    }
                    if (UnityEngine.Time.unscaledTime > nextNotify)
                    {
                        LogWithMessage(BepInEx.Logging.LogLevel.Warning, "[TextDump] Localizations not loaded. Try starting a new game or loading a save to start dump");
                        nextNotify = UnityEngine.Time.unscaledTime + 10;
                    }
                }
            }
        }

        private IEnumerator CheckReadyToDump()
        {
            checksRunning = true;
            yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on stuffLoaded");
            while (!stuffLoaded) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources");
            while (!Singleton<Manager.Resources>.IsInstance()) yield return null;
            while (!Singleton<Manager.Resources>.Instance.isActiveAndEnabled) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AgentProfile");
            while (Singleton<Manager.Resources>.Instance?.AgentProfile is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AnimalDefinePack");
            while (Singleton<Manager.Resources>.Instance?.AnimalDefinePack is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.AnimalTable");
            while (Singleton<Manager.Resources>.Instance?.AnimalTable is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.PlayerProfile");
            while (Singleton<Manager.Resources>.Instance?.PlayerProfile is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.PopupInfo");
            while (Singleton<Manager.Resources>.Instance?.PopupInfo is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Sound");
            while (Singleton<Manager.Resources>.Instance?.Sound is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.SoundPack");
            while (Singleton<Manager.Resources>.Instance?.SoundPack is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.StatusProfile");
            while (Singleton<Manager.Resources>.Instance?.StatusProfile is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.WaypointDataList");
            while (Singleton<Manager.Resources>.Instance?.WaypointDataList is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.HSceneTable");
            while (Singleton<Manager.Resources>.Instance?.HSceneTable is null) yield return null;
            
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Localize.GetHName");
            string dummy = null;
            while (string.IsNullOrEmpty(dummy))
            {
                yield return null;
                dummy = Singleton<Manager.Resources>.Instance.Localize.GetHName(1, 1);
            }

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.itemIconTables");
            while (Singleton<Manager.Resources>.Instance?.itemIconTables is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.Map");
            while (Singleton<Manager.Resources>.Instance?.Map is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.LocomotionProfile");
            while (Singleton<Manager.Resources>.Instance?.LocomotionProfile is null) yield return null;
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.MerchantProfile");
            while (Singleton<Manager.Resources>.Instance?.MerchantProfile is null) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Resources.GameInfo");
            while (!Singleton<Manager.Resources>.Instance.GameInfo.initialized) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Simulator");
            while (Singleton<Manager.Map>.Instance?.Simulator is null) yield return null;
            while (!Singleton<Manager.Map>.Instance.Simulator.IsActiveSimElement) yield return null;
            /*
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Simulator.EnabledTimeProgression");
            while (!Singleton<Manager.Map>.Instance.Simulator.EnabledTimeProgression) yield return null;
            */
            Logger.LogDebug("CheckReadyToDump: waiting for scene to finish loading");
            while (Singleton<Manager.Scene>.Instance.IsNowLoading || Singleton<Manager.Scene>.Instance.IsNowLoadingFade) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Player");
            while (Singleton<Manager.Map>.Instance?.Player is null) yield return null;

            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Player.PlayerController");
            while (Singleton<Manager.Map>.Instance?.Player?.PlayerController is null) yield return null;

            /**/
            Logger.LogDebug("CheckReadyToDump: waiting on Manager.Map.Instance.Player.PlayerController.State to be Normal");
            while (!(Singleton<Manager.Map>.Instance?.Player?.PlayerController.State is AIProject.Player.Normal)) yield return null;
            /**/
            yield return null;
            Logger.LogDebug("CheckReadyToDump: ready!");

            readyToDump = true;
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Logger?.LogWarning($"Loaded: {arg0.name}");

            stuffLoaded = almostLoaded;
            if (!stuffLoaded && !string.IsNullOrEmpty(arg0.name) && arg0.name.StartsWith("map_") && arg0.name.EndsWith("_data"))
            {
                almostLoaded = true;
            }
            else if (stuffLoaded && Enabled.Value && !DumpStarted && CurrentExecutionMode == TextDump.ExecutionMode.Other)
            {
                LogWithMessage(BepInEx.Logging.LogLevel.Warning, "[TextDump] Localizations not loaded. Try starting a new game to start dump");
            }

            if (!almostLoaded && !stuffLoaded && arg0.name == "Title")
            {
                Logger.LogInfo("[TextDump] Starting dump from sceneLoaded('Title')");
                DumpText();
                resetDumpStatus = false;
            }

            if (DumpCompleted && resetDumpStatus && stuffLoaded)
            {
                //SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            }
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            Logger?.LogFatal($"Unloaded: {arg0.name}");
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            Logger?.LogFatal($"Changed: {arg0.name} -> {arg1.name}");
            stuffLoaded = almostLoaded;
        }
    }
}
