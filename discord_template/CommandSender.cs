using System.Text;

namespace voicevox_discord
{
    internal class CommandSender
    {

        /// <summary>
        /// ギルドコマンドを登録
        /// </summary>
        public static void RegisterGuildCommands()
        {
            var directoryPath = Directory.GetCurrentDirectory() + "/commands";
            if (directoryPath.IsNullOrEmpty()) { throw new Exception($"{nameof(directoryPath)}が不正です。\nnullもしくは空白です。"); }
            //コマンドファイル一覧を取得
            if (!Directory.Exists(directoryPath)) { throw new Exception($"指定されたパス{directoryPath}は存在しません。"); }
            var commandPathList = Directory.GetFiles(directoryPath, "*.json");
            if (commandPathList.Length <= 0) { throw new Exception("指定されたパス内にjsonファイルが存在しませんでした。"); }

            //コマンドファイルの中身を取り出す
            var CommandList = commandPathList.Select(_ => File.ReadAllText(_));
            foreach (string jsonCommand in CommandList) {
                foreach (string id in Settings.Shared.m_GuildIds) {
                    HttpRequestMessage request = GetHeader(id);

                    if (jsonCommand.IsNullOrEmpty()) { throw new Exception("json_commandが不正です。\njson_commandがnullもしくは空白です。"); }

                    //HttpRequestMessageのコンテンツを設定する
                    HttpRequestMessage sendRequest = RequestContentBuilder(request, jsonCommand);

                    //送信する
                    HttpClient client = new HttpClient();
                    HttpResponseMessage response = client.Send(sendRequest);
                    Console.WriteLine(response.ToString());
                }
            }
        }

        /// <summary>
        /// ヘッダーの追加処理
        /// </summary>
        private static HttpRequestMessage GetHeader(string guild_id)
        {
            if (guild_id.IsNullOrEmpty()) { throw new Exception("guild_idが不正です。\nguild_idがnull、もしくは空白です。"); }

            string url = $"https://discord.com/api/v{Settings.Shared.m_DiscordAPIVersion}/applications/{Settings.Shared.m_ApplicationId}/guilds/{guild_id}/commands";
            UriBuilder builder = new UriBuilder(new Uri(url));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, builder.Uri);
            request.Headers.Add("Authorization", "Bot " + Settings.Shared.m_Token);

            return request;
        }

        /// <summary>
        /// リクエストメッセージにコンテンツとしてjson形式のコマンド一覧を追加
        /// </summary>
        private static HttpRequestMessage RequestContentBuilder(HttpRequestMessage requestMessage, string jsonCommand)
        {
            Console.WriteLine(jsonCommand);

            //渡されたjson形式のコマンド情報をコンテンツに設定する
            requestMessage.Content = new StringContent(jsonCommand, Encoding.UTF8, "application/json");

            return requestMessage;
        }
    }
}