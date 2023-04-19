using Discord;
using Discord.Audio;

namespace voicevox_discord
{
    internal class AudioServiceData
    {
        public AudioServiceData()
        {
            var speaker = Settings.Shared.m_EngineDictionary.First().Value.GetSpeakers().Result.First();
            var stylename = speaker.styles.First();

            GuildSpeakerInfo = new GuildSpeaker()
            {
                engineName = Settings.Shared.m_EngineDictionary.First().Key,
                name = speaker.name,
                style = stylename.name,
                speakerId = stylename.id,
            };
        }

        /*---------------話者設定---------------*/
        public GuildSpeaker GuildSpeakerInfo { get; private set; } 
        public VoicevoxEngineApi? VoicevoxEngineApi { get; private set; } = Settings.Shared.m_EngineDictionary["voicevox"];
        public bool IsSpeaking = false;
        /*--------------------------------------*/

        /*---------------ChatGPT---------------*/
        private Cache<ChatGpt, AudioServiceData> ChachedChatGPT = new Cache<ChatGpt, AudioServiceData>(_ => 
        {
            var chatGpt = new ChatGpt(Settings.Shared.m_OpenAIKey);
            chatGpt.SetInitialMessage(_.GuildSpeakerInfo.name, _.GuildSpeakerInfo.style);

            return chatGpt;
        });
        public ChatGpt ChatGpt => ChachedChatGPT.Get(this);
        /*-------------------------------------*/

        /*---------------AudioChannel---------------*/
        public ulong? guildId { get; private set; } = null;
        public IVoiceChannel? voiceChannel { get; set; } = null;
        public IAudioClient? audioclient { get; set; } = null;
        public AudioOutStream? audiooutstream { get; set; } = null;
        /*------------------------------------------*/


        public void SetSpeaker(string engineName, string speakerName, string styleName, int id)
        {
            GuildSpeakerInfo.engineName = engineName;
            GuildSpeakerInfo.name = speakerName;
            GuildSpeakerInfo.style = styleName;
            GuildSpeakerInfo.speakerId = id;

            VoicevoxEngineApi = Settings.Shared.m_EngineDictionary[GuildSpeakerInfo.engineName];
            SetInitialMessage();
        }
        public void SetSavedSpeaker(ulong guildid)
        {
            GuildSaveObject guildSaveObject = new GuildSaveObject();
            GuildSpeaker guildSpeaker = guildSaveObject.guildSpeaker;
            if (Settings.Shared.m_GuildSaveObject.ContainsKey(guildid))
            {
                guildSaveObject = Settings.Shared.m_GuildSaveObject[guildid];
                guildSpeaker = guildSaveObject.guildSpeaker;
            }

            guildId = guildid;

            SetSpeaker(guildSpeaker.engineName, guildSpeaker.name, guildSpeaker.style, guildSpeaker.speakerId);
        }

        public void SaveSpeaker(ulong guildid)
        {
            guildId = guildid;

            GuildSaveObject guildSaveObject = new GuildSaveObject();
            guildSaveObject.id = (ulong)guildId!;
            guildSaveObject.guildSpeaker = GuildSpeakerInfo;
            if (Settings.Shared.m_GuildSaveObject.ContainsKey((ulong)guildId!))
            {
                Settings.Shared.m_GuildSaveObject[(ulong)guildId!] = guildSaveObject;
            }
            else
            {
                Settings.Shared.m_GuildSaveObject.Add((ulong)guildId!, guildSaveObject);
            }
            Settings.Shared.SaveSettings();
        }

        private void SetInitialMessage()
        {
            ChatGpt.SetInitialMessage(GuildSpeakerInfo.name, GuildSpeakerInfo.style);
        }
    }
}
