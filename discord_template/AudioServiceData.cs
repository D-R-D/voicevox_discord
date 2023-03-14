using Discord.Audio;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord
{
    internal class AudioServiceData
    {
        public string name { get; private set; } = "四国めたん";
        public string engine_name { get; private set; } = "voicevox";
        public string style_name { get; private set; } = "ノーマル";
        public int id { get; private set; } = 2;
        public bool speaking { get; set; } = false;

        public ChatGpt? chatGpt { get; private set; } = null;
        public VoicevoxEngineApi? voicevoxEngineApi { get; private set; } = null;

        public IVoiceChannel? voiceChannel { get; set; } = null;
        public IAudioClient? audioclient { get; set; } = null;
        public AudioOutStream? audiooutstream { get; set; } = null;

        public AudioServiceData()
        {
            SetEngine(SpeakerInfo.GetEngineApiFromengine_name(engine_name));
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

        public void initChatGpt(string OPENAI_APIKEY)
        {
            chatGpt = new ChatGpt(OPENAI_APIKEY);
            setInitialMessage();
        }

        public void setInitialMessage()
        {
            chatGpt!.SetInitialMessage($"あなたはDiscordのチャットbotです。{name}として{style_name}な感じに振る舞いなさい。");
        }
    }
}
