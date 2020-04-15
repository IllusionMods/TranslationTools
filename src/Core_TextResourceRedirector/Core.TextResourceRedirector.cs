using BepInEx;
using BepInEx.Logging;
using System;
using XUnity.AutoTranslator.Plugin.Core;

#if AI
using AIChara;
#endif

//Adopted from gravydevsupreme's TextResourceRedirector
namespace IllusionMods
{
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public const string PluginName = "Text Resource Redirector";
        public const string GUID = "com.deathweasel.bepinex.textresourceredirector";
        public const string Version = "1.2.0";
        internal static new ManualLogSource Logger;

        internal ExcelDataResourceRedirector _excelRedirector;
        internal ScenarioDataResourceRedirector _scenarioRedirector;
        //internal TextAssetResourceRedirector _textAssetResourceRedirector;
        internal TextResourceHelper _textResourceHelper;
        internal static TextAssetTableHelper _textAssetTableHelper;
        internal static TextAssetTableHandler _textAssetTableHandler;

        public delegate void TextResourceRedirectorAwakeHandler(TextResourceRedirector sender, EventArgs eventArgs);

        public event TextResourceRedirectorAwakeHandler TextResourceRedirectorAwake;
        internal void Awake()
        {
            Logger = Logger ?? base.Logger;
            _textResourceHelper = GetTextResourceHelper();
            _textAssetTableHelper = GetTextAssetTableHelper();

            _excelRedirector = new ExcelDataResourceRedirector();
            _scenarioRedirector = new ScenarioDataResourceRedirector(_textResourceHelper);
            //_textAssetResourceRedirector = new TextAssetResourceRedirector(_textAssetTableHelper);
            _textAssetTableHandler = new TextAssetTableHandler(_textAssetTableHelper);

            this.OnTextResourceRedirectorAwake(EventArgs.Empty);

            enabled = false;
        }

        internal void Main()
        {
            Logger = Logger ?? base.Logger;
#if !HS
            TextResourceRedirectorAwake += AddChaListDataHandler;
#endif
        }

        internal void OnTextResourceRedirectorAwake(EventArgs eventArgs)
        {
            TextResourceRedirectorAwake?.Invoke(this, eventArgs);
        }

#if !HS
        private void AddChaListDataHandler(TextResourceRedirector sender, EventArgs eventArgs)
        {
            TextAssetMessagePackHelper.RegisterHandler<ChaListData>(
                translate: ChaListDataTranslate,
                mark: ChaListData.ChaListDataMark);
        }

        protected virtual bool ChaListDataTranslate(ref ChaListData chaListData, SimpleTextTranslationCache cache, string calculatedModificationPath)
        {
            var idx = chaListData.lstKey.IndexOf("Name");
            bool result = false;
            if (idx != -1)
            {
                foreach (var entry in chaListData.dictList.Values)
                {
                    if (entry.Count > idx && cache.TryGetTranslation(entry[idx], true, out string translation))
                    {
                        TranslationHelper.RegisterRedirectedResourceTextToPath(translation, calculatedModificationPath);
                        result = true;
                        entry[idx] = translation;
                    }
                }
            }
            return result;
        }
#endif
    }
}
