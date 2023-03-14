using Discord;
using Discord.Audio;

namespace voicevox_discord
{
    internal class AudioServiceData
    {
        private const string DefaultEngineName = "voicevox";

        public string Name { get; private set; } = "四国めたん";
        public string EngineName { get; private set; } = DefaultEngineName;
        public string CreditName {
            get {
                return Name switch {
                    "もち子さん" => "もち子(cv 明日葉よもぎ)",
                    _ => Name
                };
            }
        }
        public string StyleName { get; private set; } = "ノーマル";
        public int Id { get; private set; } = 2;
        public bool m_IsSpeaking = false;

        public Cache<ChatGpt> ChachedChatGPT = new Cache<ChatGpt>(() => new ChatGpt(Settings.Shared.m_OpenAIKey));

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
            VoicevoxEngineApi = Settings.Shared.m_EngineDictionary[DefaultEngineName];
            ChachedChatGPT.Value.SetInitialMessage($"あなたはDiscordのチャットbotです。{Name}として{StyleName}な感じに振る舞いなさい。");
        }
    }
}
