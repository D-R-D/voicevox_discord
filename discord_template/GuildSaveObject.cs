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
        public string engineType { get; set; }
        public string name { get; set; }
        public string uuid { get; set; }
        public string style { get; set; }
        public int speakerId { get; set; }

        public GuildSpeaker()
        {
            engineName = "voicevox";
            engineType = "voicevox";
            uuid = "7ffcb7ce-00ec-4bdc-82cd-45a8889e43ff";
            name = "四国めたん";
            style = "ノーマル";
            speakerId = 2;
        }
    }
}
