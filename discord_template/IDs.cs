using System.Configuration;

namespace discord_template
{
    public class Ids
    {
        public readonly string m_Token;
        public readonly string[] m_GuildIds;
        public readonly string m_ApplicationId;
        public readonly string[] m_AdminIds;

        public Ids(AppSettingsReader reader)
        {
            //config内のtokenを取得する
            string? token = (string?)reader.GetValue("token", typeof(string));
            if (token.IsNullOrEmpty()) { throw new Exception("tokenが不正です。\ntokenがnullもしくは空白です。"); }
            m_Token = token!;

            //config内の","で区切られたguild_idを取得する
            string? guildId = (string?)reader.GetValue("guild_id", typeof(string));
            if (guildId.IsNullOrEmpty()) { throw new Exception("guild_idが不正です。\nguild_idがnullもしくは空白です。"); }
            m_GuildIds = guildId!.Split(',');

            //config内のapplication_idを取得する
            string? applicationId = reader.GetValue("application_id", typeof(string)).ToString();
            if (applicationId.IsNullOrEmpty()) { throw new Exception("application_idが不正です。\napplication_idがnullもしくは空白です。"); }
            m_ApplicationId = applicationId!;

            string? adminId = (string?)reader.GetValue("admin_id", typeof(string));
            if (applicationId.IsNullOrEmpty()) { throw new Exception("application_idが不正です。\napplication_idがnullもしくは空白です。"); }
            m_AdminIds = applicationId!.Split(',');
        }

    }
}
