using Discord;
using Discord.WebSocket;

namespace voicevox_discord
{
    internal class SelectMenuController
    {
        private SocketMessageComponent _component;

        public SelectMenuController(SocketMessageComponent component)
        {
            if (component == null) { throw new ArgumentNullException(nameof(component)); }
            _component = component;
        }

        public async Task<(string? label, ComponentBuilder? builder)> BuildSelectmenu()
        {
            string[] CustomID = _component.Data.CustomId.Split(':');          // コマンド名 : [エンジン名] : [話者名] : チャンネルモード
            string[] CustomValue = _component.Data.Values.First().Split('@'); // 内部コマンド名 @ コマンド値
            int CommandMode = int.Parse(CustomID.Last());

            string commandName = CustomID.First();
            string InnerCommandName = CustomValue.First();
            string InnerCommandValue = CustomValue.Last();

            if(commandName == "engine")
            {
                if(InnerCommandName == "page")
                {
                    var menuBuilder = SelectMenuEditor.CreateEngineMenu(int.Parse(InnerCommandValue), CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ("以下の選択肢からエンジンを選択してください", builder);
                }

                if(InnerCommandName == "engine")
                {
                    var menuBuilder = await SelectMenuEditor.CreateSpeakerMenu(InnerCommandValue, 0, CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ("以下の選択肢から話者を選択してください", builder);
                }
            }

            if( commandName == "speaker")
            {
                if(InnerCommandName == "page")
                {
                    var menuBuilder = await SelectMenuEditor.CreateSpeakerMenu(CustomID[1], int.Parse(InnerCommandValue), CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ("以下の選択肢から話者を選択してください", builder);
                }

                if(InnerCommandName == "speaker")
                {
                    var menuBuilder = await SelectMenuEditor.CreateStyleMenu(CustomID[1], InnerCommandValue, 0, CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ("以下の選択肢から話者のスタイルを選択してください", builder);
                }
            }

            if(commandName == "speaker_id")
            {
                // ファイル生成、話者選択など
                if (InnerCommandName == "page")
                {
                    var menuBuilder = await SelectMenuEditor.CreateStyleMenu(CustomID[1], InnerCommandValue, int.Parse(InnerCommandValue), CommandMode);
                    var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                    return ("以下の選択肢からスタイルを選択してください", builder);
                }
                
                if(InnerCommandName == "id")
                {
                    return ("Go Modal", null);
                }
            }

            return (null, null);
        }
    }
}
