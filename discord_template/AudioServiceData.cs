using Discord;
using Discord.Audio;
using voicevox_discord.engineController;

namespace voicevox_discord
{
    public class AudioServiceData
    {
        public AudioServiceData(ulong guildid)
        {
            guildId = guildid;
            
            var guildEngine = Settings.Shared.m_EngineList.First().Value.Engine;
            var speaker = guildEngine.GetSpeakers().Result.First();
            var stylename = guildEngine.GetStyles(speaker).Result.First();

            GuildSave = new GuildSaveObject();
            GuildSave.guildSpeaker = new GuildSpeaker
            {
                engineName = Settings.Shared.m_EngineList.First().Key,
                engineType = Settings.Shared.m_EngineList.First().Value.engineType,
                name = speaker,
                uuid = guildEngine.GetSpeakerUUID(speaker).Result,
                style = stylename,
                speakerId = guildEngine.GetStyleId(speaker, stylename).Result,
            };
        }

        //
        // インスタンスを生成する
        public static AudioServiceData Create(ulong guildid)
        {
            return new AudioServiceData(guildid);
        }

        /*---------------話者設定---------------*/
        public GuildSaveObject GuildSave { get; private set; } 
        public SynthesisEngine EngineApi { get; private set; } = Settings.Shared.m_EngineList["voicevox"].Engine;
        public bool IsSpeaking = false;
        /*--------------------------------------*/

        /*---------------ChatGPT---------------*/
        private Cache<ChatGpt, AudioServiceData> ChachedChatGPT = new Cache<ChatGpt, AudioServiceData>(_ => 
        {
            var chatGpt = new ChatGpt(Settings.Shared.m_OpenAIKey);
            chatGpt.SetInitialMessage(_.GuildSave.guildSpeaker.name, _.GuildSave.guildSpeaker.style);

            return chatGpt;
        });
        public ChatGpt ChatGpt => ChachedChatGPT.Get(this);
        /*-------------------------------------*/

        /*---------------AudioChannel---------------*/
        public ulong guildId { get; private set; }
        public IVoiceChannel? voiceChannel { get; set; }
        public IAudioClient? audioclient { get; set; }
        public AudioOutStream? audiooutstream { get; set; }
        /*------------------------------------------*/


        public void SetSpeaker(string engineName, string speakerName, string uuid,string styleName, int id)
        {
            GuildSave.guildSpeaker.engineName = engineName;
            GuildSave.guildSpeaker.engineType = Settings.Shared.m_EngineList[engineName].engineType;
            GuildSave.guildSpeaker.name = speakerName;
            GuildSave.guildSpeaker.uuid = uuid;
            GuildSave.guildSpeaker.style = styleName;
            GuildSave.guildSpeaker.speakerId = id;

            EngineApi = Settings.Shared.m_EngineList[GuildSave.guildSpeaker.engineName].Engine;
            ChatGpt.SetInitialMessage(GuildSave.guildSpeaker.name, GuildSave.guildSpeaker.style);
        }
        public void SetSavedSpeaker()
        {
            GuildSaveObject guildSaveObject = new GuildSaveObject();
            GuildSpeaker guildSpeaker = guildSaveObject.guildSpeaker;
            if (Settings.Shared.m_GuildSaveObject.ContainsKey(guildId))
            {
                guildSaveObject = Settings.Shared.m_GuildSaveObject[guildId];
                guildSpeaker = guildSaveObject.guildSpeaker;
            }

            SetSpeaker(guildSpeaker.engineName, guildSpeaker.name, guildSpeaker.uuid,guildSpeaker.style, guildSpeaker.speakerId);
        }

        public void SaveSpeaker()
        {
            GuildSaveObject guildSaveObject = new GuildSaveObject();
            guildSaveObject.id = guildId;
            guildSaveObject.guildSpeaker = GuildSave.guildSpeaker;
            if (Settings.Shared.m_GuildSaveObject.ContainsKey(guildId))
            {
                Settings.Shared.m_GuildSaveObject[guildId] = guildSaveObject;
            }
            else
            {
                Settings.Shared.m_GuildSaveObject.Add(guildId, guildSaveObject);
            }
            Settings.Shared.SaveGuildSettings();
        }

        public void SaveDictionary()
        {
            
        }
    }
}
