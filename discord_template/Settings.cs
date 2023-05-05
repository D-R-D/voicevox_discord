using Newtonsoft.Json;
using System.Configuration;
using System.Text;
using System.Xml.Linq;

namespace voicevox_discord
{

    internal class Settings
    {
        private const string XmlFileName = "voicevox_engine_list.xml";
        private const string GuildSaveFile = "save/GuildSpeaker.json";
        private readonly object LockObject = new object();

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

        public readonly IReadOnlyDictionary<string, VoicevoxEngineApi> m_EngineDictionary;
        public readonly Dictionary<ulong, GuildSaveObject> m_GuildSaveObject;

        public readonly Dictionary<ulong, GuildSaveObject> m_GuildSaveObject;


#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
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

            m_EngineDictionary = GetServerXML();
            m_GuildSaveObject = GetGuildSettings();
        }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        
        private Dictionary<string, VoicevoxEngineApi> GetServerXML()
        {
            var engineDictionary = new Dictionary<string, VoicevoxEngineApi>();
            try
            {
                XElement engine_element = XElement.Load($"{Directory.GetCurrentDirectory()}/{XmlFileName}");

                foreach (var engine in engine_element.Elements("engine"))
                {
                    var name = engine.Element("name")!.Value;
                    var ip = engine.Element("ipaddress")!.Value;
                    var port = int.Parse(engine.Element("port")!.Value);

                    // エンジンが立っていない場合はエラーを吐いてスキップしたかった
                    var api = VoicevoxEngineApi.Create(ip, port, name);
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
                string jsonstr;
                using (StreamReader sr = new StreamReader($"{Directory.GetCurrentDirectory()}/{GuildSaveFile}"))
                {
                    jsonstr = sr.ReadToEnd();
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

        public void SaveGuildSettings()
        {
            while (m_GuildSaveObject == null)
            {
                Task.Yield();
            }

            if (!Monitor.TryEnter(LockObject))
            {
                return; // 他のスレッドが制御を持っていたら何もせず離れる
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

                } while (temp_GuildData != m_GuildSaveObject);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Monitor.Exit(LockObject);
            }
        }
    }
}