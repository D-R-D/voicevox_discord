using CliWrap;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Xml.Linq;

namespace voicevox_discord
{
    class Program
    {
        private static AudioService s_AudioService = new AudioService();
        private DiscordSocketClient? _client;
        private static CommandService? _commands;

        public static void Main(string[] args)
        {
            InitDirectory initDirectory = new InitDirectory();
            initDirectory.init();

            //コマンドファイルからjson形式でコマンドを取得・設定する
            CommandSender.RegisterGuildCommands();
            Console.WriteLine("CommandSender SUCCESS!!");

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

            await _client.LoginAsync(TokenType.Bot, Settings.Shared.m_Token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage message)
        {
            if (message.Exception is CommandException cmdException) {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}" + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            } else { Console.WriteLine($"[General/{message.Severity}] {message}"); }

            return Task.CompletedTask;
        }
        public async Task Client_Ready()
        {
            //クライアント立ち上げ時の処理
            await Task.CompletedTask;
        }

        //
        //スラッシュコマンドのイベント処理
        [Command(RunMode = RunMode.Async)]
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            _ = Task.Run(async () => {
                try {
                    if (!command.GuildId.HasValue) {
                        await command.RespondAsync("ごめんね、guild専用なんだ");
                        return;
                    }
                    ulong guildid = command.GuildId.Value;

                    SelectMenuBuilder? menuBuilder = null;
                    ComponentBuilder? builder = null;
                    string commandname = command.Data.Name;

                    //
                    //guildidの登録処理
                    if (commandname == "voicechannel") {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;

                        //join
                        await s_AudioService!.JoinOperation(command, firstval);

                        return;
                    }
                    if (commandname == "setspeaker") {
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        menuBuilder = await SelectMenuEditor.CreateSpeakerMenu(firstval, 0, true);
                        builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                        await command.RespondAsync("以下の選択肢から話者を選択してください", components: builder.Build(), ephemeral: true);

                        return;
                    }
                    if (commandname == "read") {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        await s_AudioService.TextReader(command, firstval);

                        return;
                    }

                    if (commandname == "chat") {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        await s_AudioService.Chat(command, firstval);

                        return;
                    }

                    menuBuilder = await SelectMenuEditor.CreateSpeakerMenu(commandname, 0, false);
                    builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    await command.RespondAsync("以下の選択肢から話者を選択してください", components: builder.Build(), ephemeral: true);

                    return;
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    if (command.HasResponded) {
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
            _ = Task.Run(async () => {
                try {
                    SelectMenuController selectMenuController = new SelectMenuController(arg);

                    string[] commandid = arg.Data.CustomId.Split(':'); //speaker:corename:commandmode
                    string[] selecteditem = arg.Data.Values.First().Split('@'); //コマンド@コマンド名
                    bool commandmode = false;
                    if (commandid[2] == "1") { commandmode = true; }

                    if (commandid[0] == "speaker") {
                        var respondcontent = await selectMenuController.BuildSelectmenu(commandid, commandmode);

                        await arg.RespondAsync(respondcontent.label, components: respondcontent.builder.Build(), ephemeral: true);
                    } else if (commandid[0] == "speaker_id") {
                        string value = string.Join(", ", arg.Data.Values);
                        if (!commandmode) {
                            var textitem = new TextInputBuilder().WithLabel("INPUT TEXT").WithCustomId(value).WithStyle(TextInputStyle.Paragraph).WithRequired(true).WithPlaceholder("話して欲しい文章を入力");
                            var builder = new ModalBuilder().WithTitle("Input Text").WithCustomId($"speak_text:{commandid[1]}").AddTextInput(textitem);

                            await arg.RespondWithModalAsync(builder.Build());
                        }
                        //
                        //guildidの登録処理
                        else {
                            ulong guildid = arg.GuildId!.Value;

                            //setspeaker
                            string speakername = selecteditem[1];
                            string stylename = selecteditem[2];
                            int speakerId = await Settings.Shared.m_EngineDictionary[commandid[1]].GetSpeakerId(speakername, stylename);
                            s_AudioService.SetSpeaker(guildid, speakername, stylename, speakerId, commandid[1]);

                            await arg.RespondAsync($"話者を[{commandid[1].ToUpper()}:{speakername} @ {stylename} (id:{speakerId})]に変更しました");
                        }
                    }

                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    if (arg.HasResponded) {
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
            _ = Task.Run(async () => {
                await modal.DeferAsync();
                string[] commandid = modal.Data.CustomId.Split(':');
                var command = commandid[0];
                var engineName = commandid[1];
                Console.WriteLine(command + ":" + engineName);
                var guildId = modal.GuildId!.Value;

                try {
                    if (command == "speak_text") {
                        VoicevoxEngineApi voicevoxEngineApi = Settings.Shared.m_EngineDictionary[engineName];
                        List<SocketMessageComponentData> components = modal.Data.Components.ToList();
                        var speakerValues = components.First().CustomId.Split("@");
                        var speakerName = speakerValues[1];
                        var styleName = speakerValues[2];

                        var speakerId = await voicevoxEngineApi.GetSpeakerId(speakerName, styleName);
                        string text = components.First().Value;
                        //VoicevoxEngineからWavファイルをもらう
                        Stream stream = await voicevoxEngineApi!.GetWavFromApi(speakerId, text);

                        //ファイル添付に必用な処理
                        FileAttachment fa = new FileAttachment(stream, text.Replace("\n", "") + ".wav");
                        List<FileAttachment> flis = new List<FileAttachment>();
                        flis.Add(fa);
                        Optional<IEnumerable<FileAttachment>> optional = new Optional<IEnumerable<FileAttachment>>(flis);

                        //話者"もち子さん"はクレジットに記載する名前が話者リストの名前と違うので別にクレジット記載用の処理を追加
                        string content = $"{modal.User.Mention}\n話者[ {engineName}:{ToCreditName(speakerName)} ]\nstyle[ {styleName} ( id:{speakerId} ) ]\n";
                        content += $"受け取った文字列: {(text.Length > 100 ? $"{text.Substring(0, 100)} ..." : text.Substring(0, text.Length))}"; //長さ100以上のテキストを切り捨てる

                        await modal.ModifyOriginalResponseAsync(m => {
                            m.Content = content;
                            m.Attachments = optional;
                        });
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    await modal.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
                }
            });

            await Task.CompletedTask;
        }

        private static string ToCreditName(string name)
        {
            return name switch {
                "もち子さん" => "もち子(cv 明日葉よもぎ)",
                _ => name
            };
        }
    }
}