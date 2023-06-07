using Discord;
using Discord.WebSocket;

namespace voicevox_discord
{
    public class ComponentController
    {
        private SocketMessageComponent _component;

        public ComponentController(SocketMessageComponent component)
        {
            if (component == null) { throw new ArgumentNullException(nameof(component)); }
            _component = component;
        }

        public async Task<(string? label, ComponentBuilder? builder)> BuildComponent()
        {
            string[] CustomID = _component.Data.CustomId.Split(':');          // コマンド名 : [エンジン名] : [話者名@uuid] : チャンネルモード
            string[] CustomValue = _component.Data.Values.First().Split('@'); // 内部コマンド名 @ コマンド値
            string CommandMode = CustomID.Last();

            string commandName = CustomID.First();
            string InnerCommandName = CustomValue.First();
            string InnerCommandValue = CustomValue.Last();

            if(commandName == "engine")
            {
                if(InnerCommandName == "page")
                {
                    var menuBuilder = await SelectMenuEditor.CreateEngineMenu(int.Parse(InnerCommandValue), CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ($"[/{CommandMode}](p.{InnerCommandValue})\n以下の選択肢からエンジンを選択してください", builder);
                }

                if(InnerCommandName == "engine")
                {
                    if (CommandMode == "reload")
                    {
                        return (CommandMode, null);
                    }

                    if ((await Settings.Shared.m_EngineList[InnerCommandValue].Engine.GetSpeakers()).Count() <= 0)
                    {
                        return ("FailedEngine", null);
                    }

                    if (CommandMode.Split('.')[0] == "dict")
                    {
                        return (CommandMode, null);
                    }

                    var menuBuilder = await SelectMenuEditor.CreateSpeakerMenu(InnerCommandValue, 0, CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ($"[/{CommandMode}]@{InnerCommandValue.ToUpper()}(p.0)\n以下の選択肢から話者を選択してください", builder);
                }
            }

            if( commandName == "speaker")
            {
                if(InnerCommandName == "page")
                {
                    var menuBuilder = await SelectMenuEditor.CreateSpeakerMenu(CustomID[1], int.Parse(InnerCommandValue), CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ($"[/{CommandMode}]@{CustomID[1].ToUpper()}(p.{InnerCommandValue})\n以下の選択肢から話者を選択してください", builder);
                }

                if(InnerCommandName == "speaker")
                {
                    var menuBuilder = await SelectMenuEditor.CreateStyleMenu(CustomID[1], $"{InnerCommandValue.Split('*')[0]}@{InnerCommandValue.Split('*')[1]}", 0, CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ($"[/{CommandMode}]@{CustomID[1].ToUpper()}:{InnerCommandValue}(p.0)\n以下の選択肢から話者のスタイルを選択してください", builder);
                }
            }

            if(commandName == "speaker_id")
            {
                // ファイル生成、話者選択など
                if (InnerCommandName == "page")
                {
                    var menuBuilder = await SelectMenuEditor.CreateStyleMenu(CustomID[1], InnerCommandValue, int.Parse(InnerCommandValue), CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ($"[/{CommandMode}]@{CustomID[1].ToUpper()}(p.{InnerCommandValue})\n以下の選択肢からスタイルを選択してください", builder);
                }
                
                if(InnerCommandName == "id")
                {
                    return (CommandMode, null);
                }
            }

            return (null, null);
        }
    }
}
