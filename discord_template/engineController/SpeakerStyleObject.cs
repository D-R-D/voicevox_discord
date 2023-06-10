namespace voicevox_discord.engineController
{
    // voicevox APIの話者スタイルオブジェクト
    public class VoicevoxSpeakerStyle
    {
        public string? name { get; set; }
        public int? id { get; set; }
    }

    // Coeiroink-v2 APIの話者スタイルオブジェクト
    public class CoeiroinkSpeakerStyle
    {
        public string? styleName { get; set; }
        public int? styleId { get; set; }
        public string? base64Icon { get; set; }
        public string? base64Portrait { get; set; }
    }
}
