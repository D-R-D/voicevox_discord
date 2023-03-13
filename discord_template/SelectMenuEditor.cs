using Discord;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord
{
    internal class SelectMenuEditor
    {
        public static SelectMenuBuilder CreateSpeakerMenu(List<Dictionary<string,object>> pagedspeakers, string page, string corename, bool voicechannel = false)
        {
            string commandmode = "0";
            if (voicechannel) { commandmode = "1"; }
            SelectMenuBuilder builder = new SelectMenuBuilder().WithPlaceholder($"話者一覧 p.{page}").WithCustomId($"speaker:{corename}:{commandmode}").WithMinValues(1).WithMaxValues(1);
            int nowpage =int.Parse(page);

            if(nowpage > 0) 
            {
                builder.AddOption("Previous page.", $"page@{nowpage - 1}", $"Go to page {(int.Parse(page) - 1)}."); 
            }

            foreach (KeyValuePair<string, object> speaker in pagedspeakers[int.Parse(page)])
            {
                try
                {
                    builder.AddOption(speaker.Key, $"speaker@{speaker.Key}", $"{((Dictionary<string,string>)speaker.Value).Count} params found.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if(nowpage < (pagedspeakers.Count - 1))
            {
                builder.AddOption("Next page.", $"page@{nowpage + 1}", $"Go to page {nowpage + 1}.");
            }

            return builder;
        }

        public static SelectMenuBuilder CreateStyleMenu(Dictionary<string,object> speaker, string speakername, string corename, bool voicechannel = false)
        {
            string commandmode = "0";
            if (voicechannel) { commandmode = "1"; }
            SelectMenuBuilder builder = new SelectMenuBuilder().WithPlaceholder("スタイル一覧").WithCustomId($"speaker_id:{corename}:{commandmode}").WithMinValues(1).WithMaxValues(1);
            Dictionary<string,string> styles = new Dictionary<string, string>((Dictionary<string,string>)speaker[speakername]);

            foreach (KeyValuePair<string, string> style in styles)
            {
                try
                {
                    builder.AddOption(style.Key, $"id@{speakername}@{style.Key}", $"selected speaker : {speakername}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return builder;
        }
    }
}
