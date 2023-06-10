using Newtonsoft.Json;
using System.Text;
using voicevox_discord.engineController;

namespace voicevox_discord
{
    public class VoicevoxEngineApi : SynthesisEngine
    {
        private readonly string m_EngineIPAddress;
        private readonly int m_EnginePort;
        private readonly string m_EngineName;

        private IList<VoicevoxSpeaker>? m_Speakers;
        //
        //ipaddressとportを設定する
        public VoicevoxEngineApi(string ipAddress, int port, string name)
        {
            if (Tools.IsNullOrEmpty(ipAddress)) { throw new ArgumentNullException("ipaddress"); }
            if (Tools.IsNotPortNumber(port)) { throw new Exception($"port がポート番号でない、もしくはNullです。"); }
            if (Tools.IsNullOrEmpty(name)) { throw new ArgumentNullException("name"); }

            m_EngineIPAddress = ipAddress;
            m_EnginePort = port;
            m_EngineName = name;
        }

        //
        // インスタンスを生成して返す
        public override VoicevoxEngineApi Create(string ipAddress, int port, string name)
        {
            var instance = new VoicevoxEngineApi(ipAddress, port, name);
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    instance.LoadSpeakers();
                    break;
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            return instance;
        }

        //
        //
        public override async void LoadSpeakers()
        {
            string json = await GetFromApi($@"http://{m_EngineIPAddress}:{m_EnginePort}/speakers");
            m_Speakers = JsonConvert.DeserializeObject<IList<VoicevoxSpeaker>>(json)!;
        }

        //
        //
        public override void WriteInfo()
        {
            Console.WriteLine($"[{m_EngineName}.Info]@{m_EngineIPAddress}:{m_EnginePort}");
        }

        //
        //渡されたurlからGetする
        private async Task<string> GetFromApi(string url)
        {
            HttpClient client = new HttpClient();
            var result = await client.GetAsync(url);

            if(!result.IsSuccessStatusCode)
            {
                throw new Exception($"[{m_EngineName}]:サーバーに接続できませんでした。");
            }

            return await result.Content.ReadAsStringAsync();
        }

        //
        //Wavファイルを取得してStreamで返す
        public override async Task<Stream> GetWavFromApi(string uuid, int id, string text)
        {
            HttpClient client = new HttpClient();
            StringContent content = new StringContent("", Encoding.UTF8, @"application/json");

            var result = await client.PostAsync(@$"http://{m_EngineIPAddress}:{m_EnginePort}/audio_query?speaker=" + id + @"&text=" + text, content);
            var json = await result.Content.ReadAsStringAsync();
            StringContent conjson = new StringContent(json, Encoding.UTF8, @"application/json");

            var res = await client.PostAsync(@$"http://{m_EngineIPAddress}:{m_EnginePort}/synthesis?speaker=" + id, conjson);

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception($"[{m_EngineName}]:サーバーに接続できませんでした。");
            }

            return await res.Content.ReadAsStreamAsync();
        }

        public override async Task<List<string>> GetSpeakers()
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.Select(_ => _.name).ToList()!;
        }
        public override async Task<List<string>> GetPagedSpeakers(int page)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.Select(_ => _.name).ToArray().Skip(16 * page).Take(16).ToList()!;
        }
        public override async Task<bool> SpeakerPageExist(int page)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            if (page < 0)
            {
                return false;
            }
            return m_Speakers.Count() > page * 16;
        }
        public override async Task<string> GetSpeakerUUID(string speakername)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.First(_ => _.name == speakername).speaker_uuid!;
        }

        public override async Task<List<string>> GetStyles(string speakername)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.First(_ => _.name == speakername).styles!.Select(_ => _.name).ToList()!;
        }
        public override async Task<List<string>> GetPagedStyles(string speakername, int page)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.First(_ => _.name == speakername).styles!.Select(_ => _.name).ToArray().Skip(16 * page).Take(16).ToList()!;
        }
        public override async Task<bool> StylePageExist(string speakername, int page)
        {
            if (page < 0)
            {
                return false;
            }
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.FirstOrDefault(_ => _.name == speakername)!.styles!.Count() > page * 16;
        }
        public override async Task<int> GetStyleId(string name, string style)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            var id = m_Speakers.FirstOrDefault(_ => _.name == name)!.styles!.Where(_ => _.name == style).FirstOrDefault()?.id;
            if (id == null)
            {
                throw new Exception($"speakerIDが存在しません。");
            }
            return id.Value;
        }
    }
}


