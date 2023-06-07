using Discord.Rest;
using Newtonsoft.Json;
using System.Configuration;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Xml.Linq;
using voicevox_discord.engineController;
using voicevox_discord.engines;

namespace voicevox_discord
{

    public class Settings
    {
        private const string XmlFileName = "voicevox_engine_list.xml";
        private const string GuildSaveFile = "save/setting/GuildSpeaker.json";
        private const string GuildDictDir = "save/dictionary";
        private readonly object LockSet = new object();
        private readonly object LockDir = new object();

        private static Cache<Settings> CachedSettings = new Cache<Settings>(() => new Settings());
        public static Settings Shared => CachedSettings.Value;

        public readonly string m_OpenAIKey;
        public readonly string m_GptModel;          // chatgptのバージョン
        public readonly string m_GptInitialMessage; // chatgptの初期化メッセージ

        public readonly string m_Token;
        public readonly string m_DiscordAPIVersion;
        public readonly string m_ApplicationId;
        public readonly string[] m_GuildIds;
        public readonly string[] m_AdminIds;

        // (エンジン名, エンジン)
        public readonly IReadOnlyDictionary<string, EngineController> m_EngineList;
        public readonly Dictionary<ulong, GuildSaveObject> m_GuildSaveObject;
        public readonly Dictionary<ulong, Dictionary<string, string>> m_GuildDictionary;

        public Settings()
        {
            #region AppSettingsReaderからの設定読み込み
            var reader = new AppSettingsReader();

            m_OpenAIKey = (string)reader.GetValue("OPENAI_APIKEY", typeof(string));
            if (m_OpenAIKey.IsNullOrEmpty()) { throw new Exception($"{nameof(m_OpenAIKey)}.\nAPIKEYがnullもしくは空白です。"); }

            m_GptModel = (string)reader.GetValue("gpt_model", typeof(string));
            if (m_OpenAIKey.IsNullOrEmpty()) { throw new Exception($"{nameof(m_GptModel)}.\ngpt_modelがnullもしくは空白です。"); }

            m_GptInitialMessage = (string)reader.GetValue("gpt_initialmessage", typeof(string));
            if (m_OpenAIKey.IsNullOrEmpty()) { throw new Exception($"{nameof(m_GptInitialMessage)}.\ngpt_initialmessageがnullもしくは空白です。"); }

            //config内のtokenを取得する
            m_Token = (string)reader.GetValue("token", typeof(string));
            if (m_Token.IsNullOrEmpty()) { throw new Exception($"{nameof(m_Token)}\ntokenがnullもしくは空白です。"); }

            //config内のdiscordapi_versionを取得する
            m_DiscordAPIVersion = (string)reader.GetValue("discordapi_version", typeof(string));
            if (m_DiscordAPIVersion.IsNullOrEmpty()) { throw new Exception($"{m_DiscordAPIVersion}.\ndiscordapi_versionがnullもしくは空白です。"); }

            //config内のapplication_idを取得する
            m_ApplicationId = (string)reader.GetValue("application_id", typeof(string));
            if (m_ApplicationId.IsNullOrEmpty()) { throw new Exception($"{nameof(m_ApplicationId)}.\napplication_idがnullもしくは空白です。"); }

            //config内の","で区切られたguild_idを取得する
            var guildId = (string)reader.GetValue("guild_id", typeof(string));
            if (guildId.IsNullOrEmpty()) { throw new Exception($"{nameof(guildId)}.\nguild_idがnullもしくは空白です。"); }
            m_GuildIds = guildId!.Split(',');

            //config内の","で区切られたadmin_idを取得する
            string adminId = (string)reader.GetValue("admin_id", typeof(string));
            if (adminId.IsNullOrEmpty()) { throw new Exception($"{nameof(adminId)}.\nadmin_idがnullもしくは空白です。"); }
            m_AdminIds = adminId!.Split(',');
            #endregion

            m_EngineList = GetServerXML();
            m_GuildSaveObject = GetGuildSettings();
            m_GuildDictionary = GetGuildDictionary();
        }
        
        private Dictionary<string, EngineController> GetServerXML()
        {
            var engineDictionary = new Dictionary<string, EngineController>();
            try
            {
                XElement engine_element = XElement.Load($"{Directory.GetCurrentDirectory()}/{XmlFileName}");

                foreach (var engine in engine_element.Elements("engine"))
                {
                    var name = engine.Element("name")!.Value;
                    var type = engine.Element("type")!.Value;
                    var ip = engine.Element("ipaddress")!.Value;
                    var port = int.Parse(engine.Element("port")!.Value);

                    // エンジンが立っていない場合はエラーを吐いてスキップしたかった
                    var api = EngineController.Create(ip, port, name, type);
                    //var api = VoicevoxEngineApi.Create(ip, port, name);
                    engineDictionary.Add(name, api);

                    Console.WriteLine(engine.Element("ipaddress")!.Value + " : " + int.Parse(engine.Element("port")!.Value));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return engineDictionary;
        }

        private Dictionary<ulong, GuildSaveObject> GetGuildSettings()
        {
            var guildSettingDictionary = new Dictionary<ulong, GuildSaveObject>();
            try
            {
                string jsonstr = string.Empty;
                using (StreamReader sr = new StreamReader($"{Directory.GetCurrentDirectory()}/{GuildSaveFile}"))
                {
                    jsonstr += sr.ReadToEnd();
                    sr.Close();
                }

                List<GuildSaveObject> guildSaveObjects = JsonConvert.DeserializeObject<List<GuildSaveObject>>(jsonstr)!;

                foreach (var guildSaveObject in guildSaveObjects)
                {
                    guildSettingDictionary.Add(guildSaveObject.id, guildSaveObject);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return guildSettingDictionary;
        }

        private Dictionary<ulong, Dictionary<string, string>> GetGuildDictionary()
        {
            Dictionary<ulong ,Dictionary<string, string>> guildDictionary = new();

            try
            {
                string[] guilds = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/{GuildDictDir}");

                foreach (var guild in guilds)
                {
                    string jsonstr = string.Empty;
                    ulong guildid = 0;
                    if (!ulong.TryParse(Path.GetFileName(guild), out guildid))
                    {
                        Console.WriteLine("[WARN]: 不正な辞書ファイルをスキップします。");
                    }
                    else
                    {
                        using (StreamReader sr = new StreamReader($"{Directory.GetCurrentDirectory()}/{guildid.ToString()}.json"))
                        {
                            jsonstr += sr.ReadToEnd();
                            sr.Close();
                        }

                        Dictionary<string, string> guildDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonstr)!;
                        guildDictionary.Add(guildid, guildDict);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return guildDictionary;
        }

        public void SaveGuildSettings()
        {
            while (m_GuildSaveObject == null)
            {
                Task.Yield();
            }

            while (!Monitor.TryEnter(LockSet))
            {
                Task.Yield(); // 他のスレッドが制御を持っていたら待機する
            }

            try
            {
                List<GuildSaveObject> guildSaveObjects;
                Dictionary<ulong, GuildSaveObject> temp_GuildData;

                do
                {
                    temp_GuildData = new(m_GuildSaveObject);
                    guildSaveObjects = new(temp_GuildData.Values);
                    string savejson = JsonConvert.SerializeObject(guildSaveObjects, Formatting.Indented);

                    using (StreamWriter sw = new StreamWriter($"{Directory.GetCurrentDirectory()}/{GuildSaveFile}", false, Encoding.UTF8))
                    {
                        sw.Write(savejson);
                    }

                } while ( (temp_GuildData.Count != m_GuildSaveObject.Count) || temp_GuildData.All(kvp => !m_GuildSaveObject.TryGetValue(kvp.Key, out var value) || value != kvp.Value) );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Monitor.Exit(LockSet);
            }
        }

        public void SaveGuildDictionary(ulong guildid)
        {
            while (m_GuildDictionary == null)
            {
                Task.Yield();
            }

            while (!Monitor.TryEnter(LockDir))
            {
                Task.Yield(); // 他のスレッドが制御を持っていたら待機する
            }

            try
            {
                Dictionary<string, string> saveDictionary;
                do
                {
                    saveDictionary = new(m_GuildDictionary[guildid]);
                    string savejson = JsonConvert.SerializeObject(saveDictionary, Formatting.Indented);

                    using (StreamWriter sw = new StreamWriter($"{Directory.GetCurrentDirectory()}/{GuildSaveFile}", false, Encoding.UTF8))
                    {
                        sw.Write(savejson);
                    }

                } while ((saveDictionary.Count != m_GuildSaveObject.Count) || saveDictionary.All(_ => !m_GuildDictionary[guildid].TryGetValue(_.Key, out var value) || value != _.Value));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Monitor.Exit(LockDir);
            }
        }
    }
}