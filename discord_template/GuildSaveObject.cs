namespace voicevox_discord
{
    public class GuildSaveObject
    {
        public ulong id { get; set; }
        public GuildSpeaker guildSpeaker { get; set; }

        public GuildSaveObject()
        {
            guildSpeaker = new GuildSpeaker();
        }
    }

    public class GuildSpeaker
    {
        public string engineName { get; set; }
        public string name { get; set; }
        public string style { get; set; }
        public int speakerId { get; set; }

        public GuildSpeaker()
        {
            engineName = "voicevox";
            name = "四国めたん";
            style = "ノーマル";
            speakerId = 2;
        }
    }
}
