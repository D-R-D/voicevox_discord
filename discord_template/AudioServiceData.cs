using Discord;
using Discord.Audio;
using NAudio.Dsp;

namespace voicevox_discord
{
    internal class AudioServiceData
    {
        public string name { get; private set; } = "四国めたん";
        public string engine_name { get; private set; } = "voicevox";
        public string style_name { get; private set; } = "ノーマル";
        public int id { get; private set; } = 2;
        public bool speaking { get; set; } = false;

        private ChatGpt? _ChatGPT;
        public ChatGpt ChatGPT {
            get {
                if (_ChatGPT == null) {
                    _ChatGPT = new ChatGpt(Settings.Shared.m_OpenAIKey);
                    _ChatGPT!.SetInitialMessage($"あなたはDiscordのチャットbotです。{name}として{style_name}な感じに振る舞いなさい。");
                }
                return _ChatGPT;
            }
        }

        public VoicevoxEngineApi? voicevoxEngineApi { get; private set; } = null;

        public IVoiceChannel? voiceChannel { get; set; } = null;
        public IAudioClient? audioclient { get; set; } = null;
        public AudioOutStream? audiooutstream { get; set; } = null;

        public AudioServiceData()
        {
            SetEngine(SpeakerInfo.GetEngineApiFromEngineName(engine_name));
        }

        public void SetEngine(VoicevoxEngineApi _voicevoxEngineApi)
        {
            voicevoxEngineApi = _voicevoxEngineApi;
        }

        public void SetSpeakerInfo(string _name, string _style_name, int _id)
        {
            style_name = _style_name;
            name = _name;
            id = _id;
        }

        public void SetEngineName(string _engine_name)
        {
            engine_name = _engine_name;
        }
    }
}
