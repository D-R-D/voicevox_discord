using Discord;
using Discord.Audio;

namespace voicevox_discord
{
    internal class AudioServiceData
    {
        private const string DefaultEngineName = "voicevox";

        public AudioServiceData()
        {
            var speaker = Settings.Shared.m_EngineDictionary.First().Value.GetSpeakers().Result.First();
            var style = speaker.styles.First();

            EngineName = Settings.Shared.m_EngineDictionary.First().Key;
            Name = speaker.name;
            StyleName = style.name;
            Id = style.id;
        }

        public string Name { get; private set; } = "四国めたん";
        public string EngineName { get; private set; } = DefaultEngineName;
        public string StyleName { get; private set; } = "ノーマル";
        public int Id { get; private set; } = 2;
        public bool m_IsSpeaking = false;

        private Cache<ChatGpt, AudioServiceData> ChachedChatGPT = new Cache<ChatGpt, AudioServiceData>(_ => 
        {
            var chatGpt = new ChatGpt(Settings.Shared.m_OpenAIKey);
            chatGpt.SetInitialMessage(_.Name, _.StyleName);

            return chatGpt;
        });

        public ChatGpt ChatGpt => ChachedChatGPT.Get(this);

        public VoicevoxEngineApi? VoicevoxEngineApi { get; private set; } = Settings.Shared.m_EngineDictionary[DefaultEngineName];

        public IVoiceChannel? voiceChannel { get; set; } = null;
        public IAudioClient? audioclient { get; set; } = null;
        public AudioOutStream? audiooutstream { get; set; } = null;

        public void SetSpeaker(string engineName, string speakerName, string styleName, int id)
        {
            EngineName = engineName;
            Name = speakerName;
            StyleName = styleName;
            Id = id;
            VoicevoxEngineApi = Settings.Shared.m_EngineDictionary[EngineName];
            SetInitialMessage();
        }

        private void SetInitialMessage()
        {
            ChatGpt.SetInitialMessage(Name, StyleName);
        }
    }
}
