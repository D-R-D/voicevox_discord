using System.Configuration;

namespace voicevox_discord
{

    internal class Settings
    {
        private static Settings? _Shared;
        public static Settings Shared {
            get {
                _Shared ??= new Settings();
                return _Shared;
            }
        }

        public readonly string m_OpenAIKey;
        public readonly string m_Token;
        public readonly string[] m_GuildIds;
        public readonly string m_ApplicationId;
        public readonly string[] m_AdminIds;
        public readonly string m_DiscordAPIVersion;

        public Settings()
        {
            var reader = new AppSettingsReader();

            m_OpenAIKey = (string)reader.GetValue("OPENAI_APIKEY", typeof(string));
            if (m_OpenAIKey.IsNullOrEmpty()) { throw new Exception("OPENAI_APIKEY。\ntokenがnullもしくは空白です。"); }

            //config内のtokenを取得する
            m_Token = (string)reader.GetValue("token", typeof(string));
            if (m_Token.IsNullOrEmpty()) { throw new Exception("tokenが不正です。\ntokenがnullもしくは空白です。"); }

            //config内の","で区切られたguild_idを取得する
            var guildId = (string)reader.GetValue("guild_id", typeof(string));
            if (guildId.IsNullOrEmpty()) { throw new Exception("guild_idが不正です。\nguild_idがnullもしくは空白です。"); }
            m_GuildIds = guildId!.Split(',');

            //config内のapplication_idを取得する
            m_ApplicationId = (string)reader.GetValue("application_id", typeof(string));
            if (m_ApplicationId.IsNullOrEmpty()) { throw new Exception("application_idが不正です。\napplication_idがnullもしくは空白です。"); }

            string adminId = (string)reader.GetValue("admin_id", typeof(string));
            if (adminId.IsNullOrEmpty()) { throw new Exception("admin_id。\napplication_idがnullもしくは空白です。"); }
            m_AdminIds = adminId!.Split(',');

            m_DiscordAPIVersion = (string)reader.GetValue("discordapi_version", typeof(string));
            if (m_DiscordAPIVersion.IsNullOrEmpty()) { throw new Exception("discordapi_version。\napplication_idがnullもしくは空白です。"); }

        }

    }
}
