using System;
using BepInEx;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;


#if AI
using AIChara;
#endif

//Adopted from gravydevsupreme's TextResourceRedirector
namespace IllusionMods
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier, XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier, XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Version)]
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public delegate void TextResourceRedirectorAwakeHandler(TextResourceRedirector sender, EventArgs eventArgs);

        public const string PluginName = "Text Resource Redirector";
        public const string GUID = "com.deathweasel.bepinex.textresourceredirector";
        public const string Version = "1.2.1";
        internal new static ManualLogSource Logger;
        internal static TextAssetTableHelper TextAssetTableHelper;
#if !HS
        internal ChaListDataHandler ChaListDataHandler;
#endif

        internal ExcelDataHandler ExcelDataHandler;
        internal ScenarioDataHandler ScenarioDataHandler;

        internal TextAssetTableHandler TextAssetTableHandler;
        //internal TextAssetResourceRedirector _textAssetResourceRedirector;
        internal TextResourceHelper TextResourceHelper;

        public event TextResourceRedirectorAwakeHandler TextResourceRedirectorAwake;

        internal void Awake()
        {
            Logger = Logger ?? base.Logger;
            TextResourceHelper = GetTextResourceHelper();
            TextAssetTableHelper = GetTextAssetTableHelper();

            ExcelDataHandler = new ExcelDataHandler(TextResourceHelper);
            ScenarioDataHandler = new ScenarioDataHandler(TextResourceHelper);
            TextAssetTableHandler = new TextAssetTableHandler(TextAssetTableHelper);
            enabled = false;
#if !HS
            ChaListDataHandler = new ChaListDataHandler();
#endif
            OnTextResourceRedirectorAwake(EventArgs.Empty);
        }

        internal void Main()
        {
            Logger = Logger ?? base.Logger;
        }

        internal void OnTextResourceRedirectorAwake(EventArgs eventArgs)
        {
            TextResourceRedirectorAwake?.Invoke(this, eventArgs);
        }

#if !HS
        protected virtual bool ChaListDataTranslate(ref ChaListData chaListData, SimpleTextTranslationCache cache,
            string calculatedModificationPath)
        {
            var idx = chaListData.lstKey.IndexOf("Name");
            var result = false;
            if (idx == -1) return result;
            foreach (var entry in chaListData.dictList.Values)
            {
                if (entry.Count <= idx || !cache.TryGetTranslation(entry[idx], true, out var translation)) continue;

                TranslationHelper.RegisterRedirectedResourceTextToPath(translation, calculatedModificationPath);
                result = true;
                entry[idx] = translation;
            }

            return result;
        }
#endif
    }
}
