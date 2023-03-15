using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord
{
    public class ChatCompletionResponse
    {
        public string? id { get; set; }
        public string? @object { get; set; }
        public long? created { get; set; }
        public string? model { get; set; }
        public Usage? usage { get; set; }
        public List<Choice>? choices { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class Message
    {
        public string? role { get; set; }
        public string? content { get; set; }
    }

    public class Choice
    {
        public Message? message { get; set; }
        public object? finish_reason { get; set; }
        public int index { get; set; }
    }

    internal class ChatGpt
    {
        private string OPENAI_APIKEY = string.Empty;
        private string InitMessage = string.Empty;
        private HttpClient? Client = null;

        public ChatGpt(string _OPENAI_APIKEY)
        {
            OPENAI_APIKEY = _OPENAI_APIKEY;
            InitMessage = "これからの会話に適切な返答をしなさい。";
        }

        public void SetInitialMessage(string Name, string styleName)
        {
            InitMessage = $"You are a chat bot on discord. You are {Name}. You are {styleName} and concise in your responses. You use Japanese.";
        }

        public async Task<string> RequestSender(string message)
        {
            if(Client == null)
            {
                Client = new HttpClient();
            }

            //ヘッダを作成する
            HttpRequestMessage request = GetHeader();
            //HttpRequestMessageのコンテンツを設定する
            HttpRequestMessage sendRequest = RequestContentBuilder(request, BuildMessage_json(message));

            //送信する
            HttpResponseMessage response = await Client.SendAsync(sendRequest);

            string jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonResponse);
            ChatCompletionResponse chatCompletionResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(jsonResponse)!;
            // choices -> message -> contentの中身をstringで取得する
            return chatCompletionResponse.choices![0].message!.content!;
        }

        private string BuildMessage_json(string message)
        {
            List<Dictionary<string, string>> messages = new List<Dictionary<string, string>>();
            Dictionary<string, string> system_content = new Dictionary<string, string>();
            system_content.Add("role", "system");
            system_content.Add("content", InitMessage);
            messages.Add(system_content);
            Dictionary<string, string> user_content = new Dictionary<string, string>();
            user_content.Add("role", "user");
            user_content.Add("content", message);
            messages.Add(user_content);

            Dictionary<string, object> body = new Dictionary<string, object>();
            body.Add("model", "gpt-3.5-turbo");
            body.Add("messages", messages);

            return JsonConvert.SerializeObject(body);
        }

        //
        //ヘッダーの追加処理
        private HttpRequestMessage GetHeader()
        {
            string url = $"https://api.openai.com/v1/chat/completions";
            UriBuilder builder = new UriBuilder(new Uri(url));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, builder.Uri);
            request.Headers.Add("Authorization", "Bearer " + OPENAI_APIKEY);

            return request;
        }

        //
        //リクエストメッセージにコンテンツとしてjson形式のコマンド一覧を追加
        private HttpRequestMessage RequestContentBuilder(HttpRequestMessage requestMessage, string body)
        {
            //渡されたjson形式のコマンド情報をコンテンツに設定する
            requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");

            return requestMessage;
        }
    }
}
