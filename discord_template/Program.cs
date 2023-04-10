using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Text;

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
            _client.Log += Log;
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.ModalSubmitted += ModalHandler;
            _client.SelectMenuExecuted += SelectMenuHandler;

            _commands = new CommandService();
            _commands.Log += Log;

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
        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            _ = Task.Run(async () => 
            {
                try
                {
                    if (Settings.Shared.m_EngineDictionary.Count == 0)
                    {
                        await command.RespondAsync("エンジンリスト、話者データが存在しません。");
                        return;
                    }

                    if (!command.GuildId.HasValue)
                    {
                        await command.RespondAsync("ごめんね、guild専用なんだ");
                        return;
                    }
                    ulong guildid = command.GuildId.Value;

                    SelectMenuBuilder? menuBuilder = null;
                    ComponentBuilder? builder = null;
                    string commandname = command.Data.Name;

                    //
                    //join, leave
                    if (commandname == "voicechannel")
                    {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;

                        //join
                        await s_AudioService!.JoinOperation(command, firstval);

                        return;
                    }

                    //
                    //set中の話者が読み上げる
                    if (commandname == "read")
                    {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        await s_AudioService.TextReader(command, firstval);

                        return;
                    }
                    //
                    //ChatGPTの返答をset中の話者が読み上げる
                    if (commandname == "chat")
                    {
                        await command.DeferAsync();
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        await s_AudioService.Chat(command, firstval);

                        return;
                    }

                    //
                    //ユーザー辞書設定用
                    if (commandname == "dict")
                    {
                        //エンジンのリストを作成
                        string message = $"[/{commandname}]@(p.0)\n以下の選択肢からエンジンを選択してください";
                        Console.WriteLine(message);
                        string firstval = command.Data.Options.First().Value.ToString()!;
                        menuBuilder = await SelectMenuEditor.CreateEngineMenu(0, $"{commandname}.{firstval}");
                        builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                        await command.RespondAsync(message, components: builder.Build(), ephemeral: true);
                        return;
                    }

                    //
                    //reload     エンジンデータ再読み込み
                    //setspeaker voicechannel話者変更
                    //voice      音声ファイル投げるやつ
                    if (true)
                    {
                        string message = $"[/{commandname}]@(p.0)\n以下の選択肢からエンジンを選択してください";
                        menuBuilder = await SelectMenuEditor.CreateEngineMenu(0, commandname);
                        builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                        await command.RespondAsync(message, components: builder.Build(), ephemeral: true);
                        return;
                    }
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
                    ComponentController selectMenuController = new ComponentController(arg);

                    string[] CustomID = arg.Data.CustomId.Split(':');          // コマンド名[.機能名] : [エンジン名] : [話者名] : コマンドモード
                    string[] CustomValue = arg.Data.Values.First().Split('@'); // 内部コマンド名 @ コマンド値

                    Console.WriteLine($"[{arg.Data.CustomId}] : [{arg.Data.Values.First()}]");

                    string commandName = CustomID.First();
                    string InnerCommandName = CustomValue.First();
                    string InnerCommandValue = CustomValue.Last();
                    string CommandMode = CustomID.Last();

                    var respondcontent = await selectMenuController.BuildComponent();
                    //
                    /*--------------------エラー処理--------------------*/
                    if (respondcontent.label == null && respondcontent.builder == null)
                    {
                        await arg.RespondAsync("値の破損を確認しました。処理をスキップします。");
                        return;
                    }

                    if (respondcontent.label == "FailedEngine")
                    {
                        try
                        {
                            Settings.Shared.m_EngineDictionary[InnerCommandValue].LoadSpeakers();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            await arg.RespondAsync($"[{InnerCommandValue}]:エラーが発生したから話者リストを再読み込みたけど失敗しちゃった…\n{ex.Message}");
                            return;
                        }

                        await arg.RespondAsync($"[{InnerCommandValue}]:エラーが発生したから話者リスト再読み込みしたよ。");
                        return;
                    }
                    /*--------------------エラー処理--------------------*/
                    //

                    //
                    //InnerCommandValue == {enginename}
                    if (respondcontent.label!.Split('.')[0] == "dict")
                    {
                        string dictmode = respondcontent.label!.Split('.')[1];
                        if (dictmode == "set")
                        {
                            // 登録内容のフォームを作成
                            var surface = new TextInputBuilder().WithLabel("SURFACE").WithCustomId("surface").WithStyle(TextInputStyle.Short).WithRequired(true).WithPlaceholder("辞書に登録する単語").WithMaxLength(100);
                            var pronunciation = new TextInputBuilder().WithLabel("PRONUNCIATION").WithCustomId("pronunciation").WithStyle(TextInputStyle.Short).WithRequired(true).WithPlaceholder("カタカナでの読み方").WithMaxLength(500);
                            var modalbuilder = new ModalBuilder().WithTitle("Input Text").WithCustomId($"user_dict:{InnerCommandValue}").AddTextInput(surface).AddTextInput(pronunciation);

                            await arg.RespondWithModalAsync(modalbuilder.Build());
                            return;
                        }

                        if (dictmode == "show")
                        {
                            string response = await Settings.Shared.m_EngineDictionary[InnerCommandValue].GetUserDictionary();
                            dynamic jsonobj = JsonConvert.DeserializeObject(response)!;
                            response = JsonConvert.SerializeObject(jsonobj, Formatting.Indented);

                            //Optional<IEnumerable<FileAttachment>> optional = new();
                            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(response)))
                            {
                                //ファイル添付に必用な処理
                                FileAttachment fa = new FileAttachment(stream, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
                                List<FileAttachment> flis = new List<FileAttachment>() { fa };
                                //optional = new Optional<IEnumerable<FileAttachment>>(flis);

                                await arg.RespondWithFilesAsync(flis,"ユーザ辞書、若しくはエラーメッセージ");
                            }
                            return;
                        }
                    }

                    //
                    // /voice コマンド、wav生成の内容記述
                    if (respondcontent.label == "voice")
                    {
                        var textitem = new TextInputBuilder().WithLabel("INPUT TEXT").WithCustomId($"{CustomID[2]}@{InnerCommandValue}").WithStyle(TextInputStyle.Paragraph).WithRequired(true).WithPlaceholder("話して欲しい文章を入力");
                        var builder = new ModalBuilder().WithTitle("Input Text").WithCustomId($"speak_text:{CustomID[1]}").AddTextInput(textitem);

                        await arg.RespondWithModalAsync(builder.Build());
                        return;
                    }

                    //
                    // /setspeaker ギルド話者の登録処理
                    if (respondcontent.label == "setspeaker")
                    {
                        ulong guildid = arg.GuildId!.Value;

                        string speakername = CustomID[2];
                        string stylename = InnerCommandValue;
                        int speakerId = await Settings.Shared.m_EngineDictionary[CustomID[1]].GetSpeakerId(speakername, stylename);
                        s_AudioService.SetSpeaker(guildid, speakername, stylename, speakerId, CustomID[1]);

                        await arg.RespondAsync($"話者を[{CustomID[1].ToUpper()}:{speakername} @ {stylename} (id:{speakerId})]に変更しました");
                        return;
                    }

                    //
                    //話者データの再読み込み
                    if (respondcontent.label == "reload")
                    {
                        try
                        {
                            Settings.Shared.m_EngineDictionary[InnerCommandValue].LoadSpeakers();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                            await arg.RespondAsync(ex.Message);
                            return;
                        }

                        await arg.RespondAsync($"{InnerCommandValue}:Reload Finished!!");
                        return;
                    }

                    //
                    //リストの生成 エンジン・話者・話者ID選択画面
                    await arg.RespondAsync(respondcontent.label, components: respondcontent.builder!.Build(), ephemeral: true);
                    return;
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

                List<SocketMessageComponentData> components = modal.Data.Components.ToList();
                string[] CustomID = modal.Data.CustomId.Split(':');
                var command = CustomID[0];
                var engineName = CustomID[1];

                try 
                {
                    if (command == "speak_text")
                    {
                        VoicevoxEngineApi voicevoxEngineApi = Settings.Shared.m_EngineDictionary[engineName];
                        var speakerValues = components[0].CustomId.Split("@");
                        var speakerName = speakerValues[0];
                        var styleName = speakerValues[1];

                        var speakerId = await voicevoxEngineApi.GetSpeakerId(speakerName, styleName);
                        string text = components.First().Value;
                        //VoicevoxEngineからWavファイルをもらう
                        Stream stream = await voicevoxEngineApi!.GetWavFromApi(speakerId, text);

                        //ファイル添付に必用な処理
                        FileAttachment fa = new FileAttachment(stream, text.Replace("\n", "") + ".wav");
                        List<FileAttachment> flis = new List<FileAttachment>() { fa };
                        Optional<IEnumerable<FileAttachment>> optional = new Optional<IEnumerable<FileAttachment>>(flis);

                        //話者"もち子さん"はクレジットに記載する名前が話者リストの名前と違うので別にクレジット記載用の処理を追加
                        string content = $"{modal.User.Mention}\n話者[ {engineName.ToUpper()}:{ToCreditName(speakerName)} ]\nstyle[ {styleName} ( id:{speakerId} ) ]\n";
                        content += $"受け取った文字列: {(text.Length > 100 ? $"{text.Substring(0, 100)} ..." : text.Substring(0, text.Length))}"; //長さ100以上のテキストを切り捨てる

                        await modal.ModifyOriginalResponseAsync(m => {
                            m.Content = content;
                            m.Attachments = optional;
                        });
                        return;
                    }
                    
                    if(command == "user_dict")
                    {
                        string surface = components[0].Value;
                        string pronunciation = components[1].Value;

                        if (!Tools.IsKatakana(pronunciation))
                        {
                            await modal.ModifyOriginalResponseAsync(m => { m.Content = $"surface:{surface} OK\npronunciation:{pronunciation} に片仮名以外が含まれています。"; });
                            return;
                        }

                        if (!await Settings.Shared.m_EngineDictionary[engineName].SetUserDictionary(surface, pronunciation))
                        {
                            await modal.ModifyOriginalResponseAsync(m => {
                                m.Content = $"辞書登録に失敗しました。\n```\nTargetEngine:{engineName}\nsurface:{surface}\npronunciation:{pronunciation}\n```"; 
                            });
                            return;
                        }
                        await modal.ModifyOriginalResponseAsync(m => {
                            m.Content = $"辞書登録が完了しました。\n```\nTargetEngine:{engineName}\nsurface:{surface}\npronunciation:{pronunciation}\n```";
                        });
                        return;
                    }
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.ToString());
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