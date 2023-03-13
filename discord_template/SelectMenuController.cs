using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public KeyValuePair<string, ComponentBuilder> BuildSelectmenu(string[] commandid, bool commandmode, Dictionary<string, List<Dictionary<string, object>>> ?paged_core_speakers = null, Dictionary<string, Dictionary<string, object>> ?core_speakers = null)
        {
            var selecteditem = string.Join(", ", _component.Data.Values).Split('@'); //コマンド@コマンド名

            if (selecteditem[0] == "page")
            {
                var menuBuilder = SelectMenuEditor.CreateSpeakerMenu(paged_core_speakers![commandid[1]], selecteditem[1], commandid[1], commandmode);
                var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                return new KeyValuePair<string, ComponentBuilder>("以下の選択肢から話者を選択してください", builder);
            }
            else
            {
                var menuBuilder = SelectMenuEditor.CreateStyleMenu(core_speakers![commandid[1]], selecteditem[1], commandid[1], commandmode);
                var builder = new ComponentBuilder().WithSelectMenu(menuBuilder);

                return new KeyValuePair<string, ComponentBuilder>("以下の選択肢から話者のスタイルを選択してください", builder);
            }
        }
    }
}
