using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.Text;

namespace voicevox_discord.commandCtrl
{
    internal static class VD_CommandHandler
    {
        internal static async void CommandRunner(SocketSlashCommand command)
        {
            if (Settings.Shared.m_EngineList.Count == 0)
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
            SelectMenuBuilder menuBuilder;
            ComponentBuilder builder;
            string commandname = command.Data.Name;

            //
            //join, leave
            if (commandname == "voicechannel")
            {
                await command.DeferAsync();
                string firstval = command.Data.Options.First().Value.ToString()!;
                await AudioService.JoinOperation(command, firstval);

                return;
            }

            //
            //set中の話者が読み上げる
            if (commandname == "read")
            {
                await command.DeferAsync();
                string firstval = command.Data.Options.First().Value.ToString()!;
                await AudioService.TextReader(command, firstval);

                return;
            }
            //
            //ChatGPTの返答をset中の話者が読み上げる
            if (commandname == "chat")
            {
                await command.DeferAsync();
                string firstval = command.Data.Options.First().Value.ToString()!;
                await AudioService.Chat(command, firstval);

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

                if (firstval == "set")
                {
                    // 登録内容のフォームを作成
                    var surface = new TextInputBuilder().WithLabel("SURFACE").WithCustomId("surface").WithStyle(TextInputStyle.Short).WithRequired(true).WithPlaceholder("辞書に登録する単語").WithMaxLength(100);
                    var pronunciation = new TextInputBuilder().WithLabel("PRONUNCIATION").WithCustomId("pronunciation").WithStyle(TextInputStyle.Short).WithRequired(true).WithPlaceholder("カタカナでの読み方").WithMaxLength(500);
                    var modalbuilder = new ModalBuilder().WithTitle("Input Text").WithCustomId("user_dict:ANY").AddTextInput(surface).AddTextInput(pronunciation);

                    await command.RespondWithModalAsync(modalbuilder.Build());
                    return;
                }
                else
                {
                    string jsondict = JsonConvert.SerializeObject(Settings.Shared.m_GuildDictionary[guildid]);

                    using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsondict)))
                    {
                        //ファイル添付に必用な処理
                        FileAttachment fa = new FileAttachment(stream, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
                        List<FileAttachment> flis = new List<FileAttachment>() { fa };
                        await command.RespondWithFilesAsync(flis, "ユーザ辞書、若しくはエラーメッセージ");
                    }

                    return;
                }
            }

            //
            //reload        エンジン固有話者リストの更新
            //setspeaker    guildデフォルト話者の変更
            if (commandname == "settings")
            {
                string firstval = command.Data.Options.First().Value.ToString()!;

                string message = $"[/{firstval}]@(p.0)\n以下の選択肢からエンジンを選択してください";
                menuBuilder = await SelectMenuEditor.CreateEngineMenu(0, firstval);
                builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                await command.RespondAsync(message, components: builder.Build(), ephemeral: true);

                return;
            }

            //
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
    }
}
