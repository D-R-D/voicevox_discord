using System.Text;

namespace voicevox_discord
{
    internal class VoicevoxEngineApi
    {
        private string engine_ipaddress = string.Empty;
        private int engine_port = 0;
        private string engine_name = string.Empty;

        //
        //ipaddressとportを設定する
        public VoicevoxEngineApi(string ipaddress, int port, string name)
        {
            if (Tools.IsNullOrEmpty(ipaddress)) { throw new ArgumentNullException("ipaddress"); }
            if (Tools.IsNotPortNumber(port)) { throw new Exception($"port がポート番号でない、もしくはNullです。"); }
            if (Tools.IsNullOrEmpty(name)) { throw new ArgumentNullException("name"); }

            engine_ipaddress = ipaddress;
            engine_port = port;
            engine_name = name;
        }

        public void WriteInfo() 
        {
            Console.WriteLine($"[{engine_name}.Info]@{engine_ipaddress}:{engine_port}");
        }

        //
        //話者リストをjson形式で取得する
        public string GetSpeakersJson()
        {
            if (Tools.IsNullOrEmpty(engine_ipaddress)) { throw new Exception($"{nameof(engine_ipaddress)}がNullもしくは空です。"); }
            if (Tools.IsNotPortNumber(engine_port)) { throw new Exception($"{nameof(engine_port)}が不正な値({engine_port})です。"); }

            string speaker_json = string.Empty;
            ManualResetEvent waitingjson = new ManualResetEvent(false);

            //VoicevoxEngineからjson形式の話者リストを取得する
            Task.Run(async () =>
            {
                speaker_json = await GetProcessAsync();
                waitingjson.Set();
            });

            waitingjson.WaitOne();
            return speaker_json!;
        }

        //
        //指定した情報でデータを取得できるまで粘る
        private async Task<string> GetProcessAsync()
        {
            string speaker_json = string.Empty;

            while (speaker_json == "" || speaker_json == null)
            {
                try
                {
                    speaker_json = await GetJsonFromApi($@"http://{engine_ipaddress}:{engine_port}/speakers");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Thread.Sleep(100);
            }

            return speaker_json!;
        }

        //
        //渡されたurlからGetする
        private static async Task<string> GetJsonFromApi(string url)
        {
            HttpClient client = new HttpClient();
            var result = await client.GetAsync(url);

            return result.Content.ReadAsStringAsync().Result;
        }

        //
        //Wavファイルを取得してStreamで返す
        public Stream GetWavFromApi(string id, string text)
        {
            ManualResetEvent waitforwav = new ManualResetEvent(false);
            Stream? wav = null;

            Task.Run(async () =>
            {
                try
                {
                    HttpClient client = new HttpClient();
                    StringContent content = new StringContent("", Encoding.UTF8, @"application/json");

                    var result = await client.PostAsync(@$"http://{engine_ipaddress}:{engine_port}/audio_query?speaker=" + id + @"&text=" + text, content);
                    var json = await result.Content.ReadAsStringAsync();
                    StringContent conjson = new StringContent(json, Encoding.UTF8, @"application/json");

                    var res = await client.PostAsync(@$"http://{engine_ipaddress}:{engine_port}/synthesis?speaker=" + id, conjson);

                    wav = await res.Content.ReadAsStreamAsync();
                    waitforwav.Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });

            waitforwav.WaitOne();
            return wav!;
        }
    }
}
