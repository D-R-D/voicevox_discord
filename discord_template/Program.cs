using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Configuration;

namespace voicevox_discord
{
    class Program
    {
        public static AppSettingsReader reader = new AppSettingsReader();
        private static VoicevoxEngineApi? voicevoxEngineApi = null;
        private static Dictionary<string, object> speakers = new();
        private static List<Dictionary<string, object>> paged_speakers = new();

        private DiscordSocketClient? _client;
        private static CommandService? _commands;

        public static void Main(string[] args)
        {
            Ids ids = new Ids(reader);

            
            //コマンドファイルからjson形式でコマンドを取得・設定する
            CommandSender commandSender = new CommandSender(Directory.GetCurrentDirectory() + "/commands", ids);
            commandSender.RequestSender();
            
            
            //voicevox_engineのwebapiからjson形式の話者一覧を取得
            voicevoxEngineApi = new VoicevoxEngineApi(reader.GetValue("voicevox_engine", typeof(string)).ToString()!, 50021);
            string speaker_json = voicevoxEngineApi.GetSpeakersJson();

            //1ページ16話者としてリストを作成
            string speakerstr;
            using (StreamReader sr = new StreamReader("speaker.json"))
            {
                speakerstr = sr.ReadToEnd();
            }
            speakers = JsonEditor.ObjectFromJson(speaker_json);
            paged_speakers = JsonEditor.CreatePagedObject(speakers, 16);


            _ = new Program().MainAsync();

            Thread.Sleep(-1);

            /*
            for (int j = 0; j < paged_speakers.Count; j++) {
                foreach (KeyValuePair<string, object> speaker in paged_speakers[j])
                {
                    Console.Write($"\"{speaker.Key}\":");

                    Console.WriteLine($"{((Dictionary<string, string>)speaker.Value).Count} Params Found.");
                }
            }
            */
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _client.Log += Log;
            _commands.Log += Log;

            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.ModalSubmitted += ModalHandler;
            _client.SelectMenuExecuted += SelectMenuHandler;

            await _client.LoginAsync(TokenType.Bot, reader.GetValue("token", typeof(string)).ToString());
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}" + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else { Console.WriteLine($"[General/{message.Severity}] {message}"); }

            return Task.CompletedTask;
        }
        public async Task Client_Ready()
        {
            //クライアント立ち上げ時の処理
            await Task.CompletedTask;
        }

        //
        //スラッシュコマンドのイベント処理
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            //コマンド受信時の処理
            if (command.Data.Name == "voicevox")
            {
                var menuBuilder = SelectMenuEditor.CreateSpeakerMenu(paged_speakers, "0");
                var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                await command.RespondAsync("以下の選択肢から話者を選択してください", components: builder.Build(), ephemeral: true);
            }
        }

        //
        //テキストボックスのイベント処理
        private static async Task ModalHandler(SocketModal modal)
        {
            
            await modal.RespondAsync("PROCESSING...");
            
            try
            {
                if (modal.Data.CustomId == "speak_text")
                {
                    List<SocketMessageComponentData> components = modal.Data.Components.ToList();

                    string speakervalue = components.First().CustomId.ToString();
                    string text = components.First(x => x.CustomId == speakervalue).Value;

                    string[] speakervalues = speakervalue.Split('@');
                    string id = ((Dictionary<string, string>)speakers[speakervalues[1]])[speakervalues[2]];
                    Stream stream = voicevoxEngineApi!.GetWavFromApi(id, text);

                    FileAttachment fa = new FileAttachment(stream, text.Replace("\n", "") + ".wav");
                    List<FileAttachment> flis = new List<FileAttachment>();
                    flis.Add(fa);
                    Optional<IEnumerable<FileAttachment>> optional = new Optional<IEnumerable<FileAttachment>>(flis);
                    string content = "";

                    if (speakervalues[1] != "もち子さん") { content = modal.User.Mention + "\n話者[ VOICEVOX:" + speakervalues[1] + " ] \nstyle[ " + speakervalues[2] + " ( id:" + id + " ) ]\n"; }
                    else { content = modal.User.Mention + "\n話者[ VOICEVOX:もち子(cv 明日葉よもぎ) ] \nstyle[ " + speakervalues[2] + " ( id:" + id + " ) ]\n"; }

                    content += "受け取った文字列: " + (text.Length > 100 ? text.Substring(0, 100) + "..." : text.Substring(0, text.Length));

                    await modal.ModifyOriginalResponseAsync(m =>
                    {
                        m.Content = content;
                        m.Attachments = optional;
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await modal.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
            }
            
        }

        //
        //セレクトメニューのイベント処理
        private async Task SelectMenuHandler(SocketMessageComponent arg)
        {
            try
            {
                if (arg.Data.CustomId.ToString() == "speaker")
                {
                    var selecteditem = string.Join(", ", arg.Data.Values).Split('@'); //コマンド@コマンド名

                    if (selecteditem[0] == "page")
                    {
                        var menuBuilder = SelectMenuEditor.CreateSpeakerMenu(paged_speakers, selecteditem[1]);
                        var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                        await arg.RespondAsync("以下の選択肢から話者を選択してください", components: builder.Build(), ephemeral: true);
                    }
                    else
                    {
                        var menuBuilder = SelectMenuEditor.CreateStyleMenu(speakers, selecteditem[1]);
                        var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                        //応答
                        await arg.RespondAsync("以下の選択肢から話者のスタイルを選択してください", components: builder.Build(), ephemeral: true);
                    }
                    }
                else if (arg.Data.CustomId.ToString() == "speaker_id")
                {
                    string value = string.Join(", ", arg.Data.Values);
                    var textitem = new TextInputBuilder().WithLabel("VOICEVOX SPEAK TEXT").WithCustomId(value).WithStyle(TextInputStyle.Paragraph).WithRequired(true).WithPlaceholder("話して欲しい文章を入力");
                    var builder = new ModalBuilder().WithTitle("Input Text").WithCustomId("speak_text").AddTextInput(textitem);

                    await arg.RespondWithModalAsync(builder.Build());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await arg.RespondAsync(ex.Message);
            }
        }
    }
}