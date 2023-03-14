using CliWrap;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Configuration;

namespace voicevox_discord
{
    class Program
    {
        public static AppSettingsReader reader = new AppSettingsReader();
        public static Dictionary<string, Dictionary<string, object>> core_speakers { get; private set; } = new();
        public static Dictionary<string, List<Dictionary<string, object>>> paged_core_speakers { get; private set; } = new();
        public static Dictionary<string, KeyValuePair<string, int>> enginedictionary { get; private set; } = new();

        private static AudioService audioService = new AudioService();
        private static string? OPENAI_APIKEY = null;

        private DiscordSocketClient? _client;
        private static CommandService? _commands;

        public static void Main(string[] args)
        {
            InitDirectory initDirectory = new InitDirectory();
            initDirectory.init();

            OPENAI_APIKEY = reader.GetValue("OPENAI_APIKEY", typeof(string)).ToString();

            //configから各IDを取得
            Ids ids = new Ids(reader);

            //コマンドファイルからjson形式でコマンドを取得・設定する
            CommandSender commandSender = new CommandSender(Directory.GetCurrentDirectory() + "/commands", ids, reader.GetValue("discordapi_version",typeof(string)).ToString()!);
            commandSender.RequestSender();
            Console.WriteLine("CommandSender SUCCESS!!");

            EngineList engineList = new EngineList();
            engineList.GetListFromXmlfile("voicevox_engine_list.xml", enginedictionary, core_speakers, paged_core_speakers);
            
            _ = new Program().MainAsync();

            Thread.Sleep(-1);
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
        [Command(RunMode=RunMode.Async)]
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    SelectMenuBuilder? menuBuilder = null;
                    ComponentBuilder? builder = null;

                    //check HasGuildId
                    if (!command.GuildId.HasValue)
                    {
                        await command.DeleteOriginalResponseAsync();
                        return;
                    }
                    ulong guildid = command.GuildId.Value;
                    string commandname = command.Data.Name;

                    //
                    //guildidの登録処理
                    if (commandname == "voicechannel")
                    {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;

                        //join
                        await audioService!.JoinOperation(command, firstval);

                        return;
                    }
                    if (commandname == "setspeaker")
                    {
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        menuBuilder = SelectMenuEditor.CreateSpeakerMenu(paged_core_speakers[firstval], "0", firstval, true);
                        builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                        await command.RespondAsync("以下の選択肢から話者を選択してください", components: builder.Build(), ephemeral: true);

                        return;
                    }
                    if (commandname == "read")
                    {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        await audioService.TextReader(command, firstval);

                        return;
                    }

                    if (commandname == "chat")
                    {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        await audioService.Chat(command, OPENAI_APIKEY!, firstval);

                        return;
                    }

                    //
                    //その他のコマンドはwavを投げるやつのみ
                    menuBuilder = SelectMenuEditor.CreateSpeakerMenu(paged_core_speakers[commandname], "0", commandname, false);
                    builder = new ComponentBuilder().WithSelectMenu(menuBuilder);
                    await command.RespondAsync("以下の選択肢から話者を選択してください", components: builder.Build(), ephemeral: true);

                    return;

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    if (command.HasResponded)
                    {
                        await command.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
                        return;
                    }
                    await command.RespondAsync(ex.Message);
                }
            });

            await Task.CompletedTask;
        }

        //
        //セレクトメニューのイベント処理
        private static async Task SelectMenuHandler(SocketMessageComponent arg)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    SelectMenuController selectMenuController = new SelectMenuController(arg);

                    ulong guildid = arg.GuildId!.Value;
                    string[] commandid = arg.Data.CustomId.Split(':'); //speaker:corename:commandmode
                    string[] selecteditem = string.Join(", ", arg.Data.Values).Split('@'); //コマンド@コマンド名
                    bool commandmode = false;
                    if (commandid[2] == "1") { commandmode = true; }

                    if (commandid[0] == "speaker")
                    {
                        KeyValuePair<string, ComponentBuilder> respondcontent = selectMenuController.BuildSelectmenu(commandid, commandmode, paged_core_speakers, core_speakers);

                        await arg.RespondAsync(respondcontent.Key, components: respondcontent.Value.Build(), ephemeral: true);
                    }
                    else if (commandid[0] == "speaker_id")
                    {
                        string value = string.Join(", ", arg.Data.Values);
                        if (!commandmode)
                        {
                            var textitem = new TextInputBuilder().WithLabel("INPUT TEXT").WithCustomId(value).WithStyle(TextInputStyle.Paragraph).WithRequired(true).WithPlaceholder("話して欲しい文章を入力");
                            var builder = new ModalBuilder().WithTitle("Input Text").WithCustomId($"speak_text:{commandid[1]}").AddTextInput(textitem);

                            await arg.RespondWithModalAsync(builder.Build());
                        }
                        //
                        //guildidの登録処理
                        else
                        {
                            //setspeaker
                            string speakername = selecteditem[1];
                            string stylename = selecteditem[2];
                            int speakerid = int.Parse(((Dictionary<string, string>)core_speakers[commandid[1]][speakername])[stylename]);
                            audioService.SetSpeaker(guildid, speakername, stylename, speakerid, commandid[1]);

                            await arg.RespondAsync($"話者を[{commandid[1].ToUpper()}:{speakername} @ {stylename} (id:{speakerid})]に変更しました");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    if (arg.HasResponded)
                    {
                        await arg.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
                        return;
                    }
                    await arg.RespondAsync(ex.Message);
                }
            });

            await Task.CompletedTask;
        }

        //
        //テキストボックスのイベント処理
        private static async Task ModalHandler(SocketModal modal)
        {
            _ = Task.Run(async () =>
            {
                await modal.RespondAsync("PROCESSING...");
                string[] commandid = modal.Data.CustomId.Split(':');

                Console.WriteLine(commandid[0] + ":" + commandid[1]);

                try
                {
                    if (commandid[0] == "speak_text")
                    {
                        VoicevoxEngineApi voicevoxEngineApi = new VoicevoxEngineApi(enginedictionary[commandid[1]].Key, enginedictionary[commandid[1]].Value, commandid[1]);
                        List<SocketMessageComponentData> components = modal.Data.Components.ToList();

                        //話者情報と入力されたテキストを取り出す
                        string speakervalue = components.First().CustomId.ToString();
                        string text = components.First(x => x.CustomId == speakervalue).Value;
                        string[] speakervalues = speakervalue.Split('@');
                        string id = ((Dictionary<string, string>)core_speakers[commandid[1]][speakervalues[1]])[speakervalues[2]];
                        //VoicevoxEngineからWavファイルをもらう
                        Stream stream = voicevoxEngineApi!.GetWavFromApi(id, text);

                        //ファイル添付に必用な処理
                        FileAttachment fa = new FileAttachment(stream, text.Replace("\n", "") + ".wav");
                        List<FileAttachment> flis = new List<FileAttachment>();
                        flis.Add(fa);
                        Optional<IEnumerable<FileAttachment>> optional = new Optional<IEnumerable<FileAttachment>>(flis);

                        //話者"もち子さん"はクレジットに記載する名前が話者リストの名前と違うので別にクレジット記載用の処理を追加
                        string content = "";
                        if (speakervalues[1] != "もち子さん") { content = $"{modal.User.Mention}\n話者[ {commandid[1].ToUpper()}:{speakervalues[1]} ]\nstyle[ {speakervalues[2]} ( id:{id} ) ]\n"; }
                        else { content = $"{modal.User.Mention}\n話者[ {commandid[1].ToUpper()}:もち子(cv 明日葉よもぎ) ]\nstyle[ {speakervalues[2]} ( id:{id} ) ]\n"; }

                        content += $"受け取った文字列: {(text.Length > 100 ? $"{text.Substring(0, 100)} ..." : text.Substring(0, text.Length))}"; //長さ100以上のテキストを切り捨てる

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
            });

            await Task.CompletedTask;
        }
    }
}