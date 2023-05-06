using Newtonsoft.Json;
using System.Text;

namespace voicevox_discord
{
    public class VoicevoxEngineApi
    {
        private readonly string m_EngineIPAddress;
        private readonly int m_EnginePort;
        private readonly string m_EngineName;

        private IList<Speaker>? m_Speakers;
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

        public static VoicevoxEngineApi Create(string ipAddress, int port, string name)
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

        public void WriteInfo()
        {
            Console.WriteLine($"[{m_EngineName}.Info]@{m_EngineIPAddress}:{m_EnginePort}");
        }

        public async void LoadSpeakers()
        {
            string json = await GetFromApi($@"http://{m_EngineIPAddress}:{m_EnginePort}/speakers");
            m_Speakers = JsonConvert.DeserializeObject<IList<Speaker>>(json)!;
        }

        //
        // ユーザー辞書登録を行う
        public async Task<bool> SetUserDictionary(string surface, string pronunciation)
        {
            HttpClient client = new HttpClient();
            StringContent content = new StringContent("", Encoding.UTF8, @"application/json");
            var res = await client.PostAsync(@$"http://{m_EngineIPAddress}:{m_EnginePort}/user_dict_word?surface={surface}&pronunciation={pronunciation}&accent_type=1", content);

            if (!res.IsSuccessStatusCode)
            {
                return false;
            }

            return true;
        }

        //
        //ユーザー辞書を取得する
        public async Task<string> GetUserDictionary()
        {
            try
            {
                string result = await GetFromApi($"http://{m_EngineIPAddress}:{m_EnginePort}/user_dict");
                
                return result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //
        //渡されたurlからGetする
        private async Task<string> GetFromApi(string url)
        {
            HttpClient client = new HttpClient();
            var result = await client.GetAsync(url);

            if(!result.IsSuccessStatusCode)
            {
                return "サーバーへ正常に接続できませんでした。";
            }

            return await result.Content.ReadAsStringAsync();
        }

        //
        //Wavファイルを取得してStreamで返す
        public async Task<Stream> GetWavFromApi(int id, string text)
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

        public async Task<int> GetSpeakerId(string name, string style)
        {
            var speakers = await GetSpeakers();
            var id = speakers.Where(_ => _.name == name).FirstOrDefault()?.styles.Where(_ => _.name == style).FirstOrDefault()?.id;
            if (id == null) 
            {
                throw new Exception($"speakerIDが存在しません。");
            }
            return id!.Value;
        }

        public async Task<Speaker[]> GetSpeakers()
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.ToArray();
        }
        public async Task<Speaker[]> GetSpeakers(int page)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.ToArray().Skip(16 * page).Take(16).ToArray();
        }
        public async Task<bool> SpeakerPageExist(int page)
        {
            if (page < 0)
            {
                return false;
            }
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.ToArray().Length > page * 16;
        }

        public async Task<Style[]> GetStyles(string speakername, int page)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.Where(_ => _.name == speakername).First().styles.ToArray().Skip(16 * page).Take(16).ToArray();
        }
        public async Task<bool> StylePageExist(string speakername, int page)
        {
            if (page < 0)
            {
                return false;
            }
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.Where(_ => _.name == speakername).FirstOrDefault()!.styles.ToArray().Length > page * 16;
        }
    }

#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
    public class Speaker
    {
        public string name { get; set; }
        public string speaker_uuid { get; set; }
        public IList<Style> styles { get; set; }
        public string version { get; set; }
    }
    public class Style
    {
        public string name { get; set; }
        public int id { get; set; }
    }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
}
