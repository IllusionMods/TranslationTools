using System.Collections.Generic;
using CharaCustom;
using Config;
using HS2;
using UploaderSystem;

namespace IllusionMods
{
    internal class HS2_LocalizationDumpHelper : AI_HS2_LocalizationDumpHelper
    {
        protected HS2_LocalizationDumpHelper(TextDump plugin) : base(plugin) { }

        public override IEnumerable<ITranslationDumper> GetInstanceLocalizers()
        {
            foreach (var localizer in base.GetInstanceLocalizers())
            {
                yield return localizer;
            }

            yield return MakeStandardInstanceLocalizer<ConfigWindow>("localizeIsInit", "localizeIsTitle");
            yield return MakeStandardInstanceLocalizer<SoundSetting>("strSelect");
            yield return MakeStandardInstanceLocalizer<TitleSetting>("strSelect");
            yield return MakeStandardInstanceLocalizer<CharaEditUI>("strConfirm");
            yield return MakeStandardInstanceLocalizer<ConciergeAchievementUI>("strNextCounts", "strExchange");
            yield return MakeStandardInstanceLocalizer<ConciergeMenuUI>("strHScene", "strCustom", "strSearch",
                "strGotoSPRoom");
            yield return MakeStandardInstanceLocalizer<FoundFemaleWindow>("strHigh", "strNormal", "strLow");
            yield return MakeStandardInstanceLocalizer<FurRoomAchievementUI>("strNextCounts", "strExchange");
            yield return MakeStandardInstanceLocalizer<FurRoomMapSelectUI>("strHScene");
            yield return MakeStandardInstanceLocalizer<FurRoomMenuUI>(
                "strCustom", "strSearch", "strReturnToHome", "strHScene");
            yield return MakeStandardInstanceLocalizer<GroupCharaParameterUI>("strResist", "strReset", "strCustom");
            // currently strSleeep is there, but putting strSleep in place in case it's renamed in future
            yield return MakeStandardInstanceLocalizer<HomeUI>("strWarning", "strToTitle", "strSleeep", "strSleep");
            yield return MakeStandardInstanceLocalizer<LeaveTheRoomUI>("strWarning", "strConfirm", "strGotoSPRoom");
            yield return MakeStandardInstanceLocalizer<LobbyMainUI>("strWarning", "strGotoSPRoom");
            yield return MakeStandardInstanceLocalizer<LobbyMapSelectUI>("strHScene");
            yield return MakeStandardInstanceLocalizer<LobbySelectUI>("strConfirm");
            yield return MakeStandardInstanceLocalizer<MaleCharaSelectUI>("strConfirm");
            yield return MakeStandardInstanceLocalizer<MapSelectUI>("strGotoMap");

            if (!(ResourceHelper is HS2_TextResourceHelper helper) || !helper.IsHS2DX()) yield break;

            StringTranslationDumper dumper = null;
            try
            {
                dumper = MakeStandardInstanceLocalizer<STRMainMenu>(
                    "strHScene", "strLobby", "strHome", "strCustom");
            }
            catch
            {
                dumper = null;
            }

            if (dumper != null) yield return dumper;

            try
            {
                dumper = MakeStandardInstanceLocalizer<STRMainMenu1>(
                    "strHScene", "strLobby", "strHome", "strCustom");
            }
            catch
            {
                dumper = null;
            }

            if (dumper != null) yield return dumper;

        }

        public override IEnumerable<ITranslationDumper> GetStaticLocalizers()
        {
            foreach (var localizer in base.GetStaticLocalizers())
            {
                yield return localizer;
            }

            yield return MakeStandardStaticLocalizer(typeof(CharaCustomDefine),
                "CustomCorrectTitle",
                "CustomColorTitle",
                "CustomCapSave",
                "CustomCapUpdate",
                "CustomNoneStr",
                "ColorPresetMessage",
                "ColorPresetNewMessage",
                "CustomHandBaseMsg",
                "CustomConfirmDelete",
                "CustomConfirmDeleteWithIncludeParam",
                "CustomConfirmOverwrite",
                "CustomConfirmOverwriteWithInitializeParam");


            yield return MakeStandardStaticLocalizer(typeof(NetworkDefine), "msgPressAnyKey", "msgServerCheck",
                "msgServerAccessInfoField",
                "msgServerAccessField", "msgUpCannotBeIdentified", "msgUpAlreadyUploaded", "msgUpCompressionHousing",
                "msgUpStartUploadHousing", "msgDownDeleteData",
                "msgDownDeleteCache", "msgDownUnknown", "msgDownDownloadData", "msgDownDownloaded", "msgDownFailed",
                "msgDownLikes", "msgDownFailedGetThumbnail",
                "msgDownNotUploadDataFound", "msgDownDecompressingFile", "msgDownFailedDecompressingFile",
                "msgDownConfirmDelete", "msgDownFailedDelete",
                "msgNetGetInfoFromServer", "msgNetGetVersion", "msgNetConfirmUser", "msgNetStartEntryHN",
                "msgNetGetAllHN", "msgNetGetAllCharaInfo",
                "msgNetGetAllHousingInfo", "msgNetReady", "msgNetNotReady", "msgNetFailedGetCharaInfo",
                "msgNetFailedGetHousingInfo", "msgNetReadyGetData",
                "msgNetFailedGetVersion", "msgNetFailedConfirmUser", "msgNetFailedUpdateHN", "msgNetUpdatedHN",
                "msgNetFailedGetAllHN");
        }

        protected override IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            foreach (var localizer in base.GetLocalizationGenerators())
            {
                yield return localizer;
            }

            /*yield return GetParameterNameLocalizers;*/
        }

        /*
        protected StringTranslationDumper MakeParameterNameLocalizer(string name,
            IReadOnlyDictionary<int, string> entries)
        {
            IDictionary<string, string> Localizer()
            {
                var results = new TranslationDictionary();
                if (entries == null) return results;
                foreach (var entry in entries)
                {
                    AddLocalizationToResults(results, entry.Value, string.Empty);
                }

                return results;
            }

            return new StringTranslationDumper($"ParameterName/{name}", Localizer);
        }

        protected IEnumerable<ITranslationDumper> GetParameterNameLocalizers()
        {
            if (!Game.IsInstance()) yield break;
            yield return MakeParameterNameLocalizer("HAttribute", Game.infoHAttributeTable);
            yield return MakeParameterNameLocalizer("Mind", Game.infoMindTable);
            yield return MakeParameterNameLocalizer("State", Game.infoStateTable);
            yield return MakeParameterNameLocalizer("Trait", Game.infoTraitTable);
        }

        */
    }
}
