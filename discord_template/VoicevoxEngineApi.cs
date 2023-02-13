using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord
{
    internal class VoicevoxEngineApi
    {
        private static string engine_ipaddress = string.Empty;
        private static int engine_port = 0;

        public VoicevoxEngineApi(string ipaddress, int port) 
        {
            if (Tools.IsNullOrEmpty(ipaddress)) { throw new ArgumentNullException(nameof(ipaddress)); }
            if (port < 1 || port > 65535) { throw new ArgumentOutOfRangeException(nameof(port)); }

            engine_ipaddress = ipaddress;
            engine_port = port;
        }

        public string GetSpeakersJson()
        {
            string speaker_json = string.Empty;
            ManualResetEvent waitingjson = new ManualResetEvent(false);

            Task.Run(async () =>
            {
                speaker_json = await GetProcessAsync();
                waitingjson.Set();
            });

            waitingjson.WaitOne();
            return speaker_json!;
        }

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

        private static async Task<string> GetJsonFromApi(string url)
        {
            HttpClient client = new HttpClient();
            var result = await client.GetAsync(url);

            return result.Content.ReadAsStringAsync().Result;
        }

        public Stream GetWavFromApi(string id, string text)
        {
            Console.WriteLine($"{id}: {text}");

            ManualResetEvent waitforwav = new ManualResetEvent(false);
            Stream? wav = null;

            Task.Run(async() => {
                try
                {
                    HttpClient client = new HttpClient();
                    StringContent content = new StringContent("", Encoding.UTF8, @"application/json");

                    var result = await client.PostAsync(@$"http://{engine_ipaddress}:{engine_port}/audio_query?speaker=" + id + @"&text=" + text, content);
                    var json = await result.Content.ReadAsStringAsync();
                    StringContent conjson = new StringContent(json, Encoding.UTF8, @"application/json");

                    var res = await client.PostAsync(@$"http://{engine_ipaddress}:{engine_port}/synthesis?speaker=" + id, conjson);

                    wav = await res.Content.ReadAsStreamAsync();
                    Thread.Sleep(1000);
                    waitforwav.Set();
                }catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });

            waitforwav.WaitOne();
            return wav!;
        }
    }
}
