using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace voicevox_discord
{
    internal class EngineList
    {
        public void GetListFromXmlfile(string xmlname, Dictionary<string, KeyValuePair<string, int>> enginedictionary, Dictionary<string, Dictionary<string, object>> core_speakers, Dictionary<string, List<Dictionary<string, object>>> paged_core_speakers)
        {
            try
            {
                XElement engine_element = XElement.Load($"{Directory.GetCurrentDirectory()}/{xmlname}");
                IEnumerable<XElement> enginelist = engine_element.Elements("engine")!;
                foreach (XElement engine in enginelist)
                {
                    KeyValuePair<string, int> enginenet = new KeyValuePair<string, int>(engine.Element("ipaddress")!.Value, int.Parse(engine.Element("port")!.Value));
                    enginedictionary.Add(engine.Element("name")!.Value, enginenet);

                    Console.WriteLine(engine.Element("ipaddress")!.Value + " : " + int.Parse(engine.Element("port")!.Value));

                    //voicevox_engineのwebapiからstring型でjson形式の話者一覧を取得
                    VoicevoxEngineApi voicevoxEngineApi = new VoicevoxEngineApi(enginenet.Key, enginenet.Value, engine.Element("name")!.Value);
                    string speaker_json = voicevoxEngineApi.GetSpeakersJson();
                    Console.WriteLine($"[{engine.Element("name")!.Value}]:VoicevoxEngineApi GetSpeakersJson SUCCESS!!");

                    //1ページ16話者としてリストを作成
                    core_speakers.Add(engine.Element("name")!.Value, JsonEditor.ObjectFromJson(speaker_json));
                    List<Dictionary<string, object>> paged_speakers = JsonEditor.CreatePagedObject(core_speakers[engine.Element("name")!.Value], 16);
                    Console.WriteLine($"[{engine.Element("name")!.Value}]paged_speakers Created!!");
                    paged_core_speakers.Add(engine.Element("name")!.Value, paged_speakers);
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
