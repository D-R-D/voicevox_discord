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

        public async Task<(string label, ComponentBuilder builder)> BuildSelectmenu(string[] commandid, bool commandmode)
        {
            var selecteditem = _component.Data.Values.FirstOrDefault()!.Split('@'); //コマンド@コマンド名

            if (selecteditem[0] == "page") {
                var menuBuilder = await SelectMenuEditor.CreateSpeakerMenu(commandid[1], int.Parse(selecteditem[1]), commandmode);
                var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                return ("以下の選択肢から話者を選択してください", builder);
            } else {
                var menuBuilder = await SelectMenuEditor.CreateStyleMenu(commandid[1], selecteditem[1], commandmode);
                var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                return ("以下の選択肢から話者のスタイルを選択してください", builder);
            }
        }
    }
}
