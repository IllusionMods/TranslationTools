using System.Collections.Generic;
using CharaCustom;
using Config;
using UploaderSystem;

namespace IllusionMods
{
    internal class HS2_LocalizationDumpHelper : AI_HS2_LocalizationDumpHelper
    {
        protected HS2_LocalizationDumpHelper(TextDump plugin) : base(plugin)
        {


        }



        public override IEnumerable<ITranslationDumper> GetInstanceLocalizers()
        {
            foreach (var localizer in base.GetInstanceLocalizers())
            {
                yield return localizer;
            }

            yield return MakeStandardInstanceLocalizer<Config.ConfigWindow>("localizeIsInit", "localizeIsTitle");
            yield return MakeStandardInstanceLocalizer<Config.SoundSetting>("strSelect");
            yield return MakeStandardInstanceLocalizer<Config.TitleSetting>("strSelect");
            yield return MakeStandardInstanceLocalizer<HS2.CharaEditUI>("strConfirm");
            yield return MakeStandardInstanceLocalizer<HS2.ConciergeAchievementUI>("strNextCounts", "strExchange");
            yield return MakeStandardInstanceLocalizer<HS2.ConciergeMenuUI>("strHScene", "strCustom", "strSearch");
            yield return MakeStandardInstanceLocalizer<HS2.FoundFemaleWindow>("strHigh", "strNormal", "strLow");
            yield return MakeStandardInstanceLocalizer<HS2.FurRoomAchievementUI>("strNextCounts", "strExchange");
            yield return MakeStandardInstanceLocalizer<HS2.FurRoomMapSelectUI>("strHScene");
            yield return MakeStandardInstanceLocalizer<HS2.FurRoomMenuUI>("strCustom", "strSearch", "strReturnToHome");
            yield return MakeStandardInstanceLocalizer<HS2.GroupCharaParameterUI>("strResist", "strReset", "strCustom");
            // currently strSleeep is there, but putting strSleep in place in case it's renamed in future
            yield return MakeStandardInstanceLocalizer<HS2.HomeUI>("strWarning", "strToTitle", "strSleeep", "strSleep");
            yield return MakeStandardInstanceLocalizer<HS2.LeaveTheRoomUI>("strWarning", "strConfirm");
            yield return MakeStandardInstanceLocalizer<HS2.LobbyMainUI>("strWarning");
            yield return MakeStandardInstanceLocalizer<HS2.LobbyMapSelectUI>("strHScene");
            yield return MakeStandardInstanceLocalizer<HS2.LobbySelectUI>("strConfirm");
            yield return MakeStandardInstanceLocalizer<HS2.MaleCharaSelectUI>("strConfirm");
            yield return MakeStandardInstanceLocalizer<HS2.MapSelectUI>("strGotoMap");

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
    }
}
