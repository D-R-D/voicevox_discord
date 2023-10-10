﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using voicevox_discord.commandCtrl;
using voicevox_discord.engineController;

namespace voicevox_discord
{
    class Program
    {
        private static DiscordSocketClient? _client;
        private static CommandService? _commands;

        public static void Main(string[] args)
        {
            // 仕様ファイル・ディレクトリの存在チェック
            InitDirectory.init();

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
            while (true)
            {
                await Task.Yield();
            }
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
                    VD_CommandHandler.CommandRunner(command);
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
                    if (arg.GuildId == null)
                    {
                        await arg.RespondAsync("不明なコマンドが実行されました。");
                        return;
                    }
                    ulong guildid = arg.GuildId.Value;

                    ComponentController selectMenuController = new ComponentController(arg);

                    Console.WriteLine($"[{arg.Data.CustomId}] : [{arg.Data.Values.First()}]");
                    string[] CustomID = arg.Data.CustomId.Split(':');          // コマンド名[.機能名] : [エンジン名] : [話者名] : コマンドモード
                    string[] CustomValue = arg.Data.Values.First().Split('@'); // 内部コマンド名 @ コマンド値

                    string commandName = CustomID.First();
                    string InnerCommandName = CustomValue.First();
                    string InnerCommandValue = CustomValue.Last();
                    string CommandMode = CustomID.Last();

                    var respondcontent = await selectMenuController.BuildComponent();

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
                        string speakername = CustomID[2];
                        string stylename = InnerCommandValue;
                        int speakerId = await Settings.Shared.m_EngineList[CustomID[1]].Engine.GetStyleId(speakername, stylename);
                        AudioService.SetSpeaker(guildid, speakername, stylename, speakerId, CustomID[1]);

                        await arg.RespondAsync($"話者を[{CustomID[1].ToUpper()}:{speakername}@{stylename} (id:{speakerId})]に変更しました");
                        return;
                    }

                    //
                    //話者データの再読み込み
                    if (respondcontent.label == "reload")
                    {
                        try
                        {
                            Settings.Shared.m_EngineList[InnerCommandValue].Engine.LoadSpeakers();
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
                if (modal.GuildId == null)
                {
                    await modal.RespondAsync("不正なコマンドが実行されました。");
                    return;
                }
                ulong guildid = modal.GuildId.Value;

                await modal.RespondAsync("PROCESSING...");

                List<SocketMessageComponentData> components = modal.Data.Components.ToList();
                string[] CustomID = modal.Data.CustomId.Split(':');
                var command = CustomID[0];
                var engineName = CustomID[1];

                try 
                {
                    if (command == "speak_text")
                    {
                        SynthesisEngine EngineApi = Settings.Shared.m_EngineList[engineName].Engine;
                        var speakerValues = components[0].CustomId.Split("@");
                        var speakerName = speakerValues[0].Split('@')[0];
                        var speakerUUID = speakerValues[0].Split('@')[1];
                        var styleName = speakerValues[1];
                        var speakerId = await EngineApi.GetStyleId(speakerName, styleName);
                        string text = components.First().Value;

                        //VoicevoxEngineからWavファイルをもらう
                        Stream stream = await EngineApi.GetWavFromApi(speakerUUID, speakerId, text);

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

                        Settings.Shared.m_GuildDictionary[guildid].Add(surface, pronunciation);
                        Settings.Shared.SaveGuildDictionary(guildid);

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