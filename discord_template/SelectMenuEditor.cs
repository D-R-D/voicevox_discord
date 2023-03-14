using Discord;

namespace voicevox_discord
{
    internal class SelectMenuEditor
    {
        public async static Task<SelectMenuBuilder> CreateSpeakerMenu(string engineName, int page, bool voicechannel = false)
        {
            string commandmode = "0";
            if (voicechannel) { commandmode = "1"; }
            SelectMenuBuilder builder = new SelectMenuBuilder().WithPlaceholder($"話者一覧 p.{page}").WithCustomId($"speaker:{engineName}:{commandmode}").WithMinValues(1).WithMaxValues(1);

            if (page > 0) {
                builder.AddOption("Previous page.", $"page@{page - 1}", $"Go to page {(page - 1)}.");
            }

            var speakers = await Settings.Shared.m_EngineDictionary[engineName].GetSpeakers(page);

            foreach (var speaker in speakers) {
                try {
                    builder.AddOption(speaker.name, $"speaker@{speaker.name}", $"{speaker.styles.Count} params found.");
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }


            if (await Settings.Shared.m_EngineDictionary[engineName].PageExist(page + 1)) {
                builder.AddOption("Next page.", $"page@{page + 1}", $"Go to page {page + 1}.");
            }

            return builder;
        }

        public async static Task<SelectMenuBuilder> CreateStyleMenu(string engineName, string speakerName, bool voiceChannel = false)
        {
            string commandmode = "0";
            if (voiceChannel) { commandmode = "1"; }
            SelectMenuBuilder builder = new SelectMenuBuilder().WithPlaceholder("スタイル一覧").WithCustomId($"speaker_id:{engineName}:{commandmode}").WithMinValues(1).WithMaxValues(1);
            var speakers = await Settings.Shared.m_EngineDictionary[engineName].GetSpeakers();
            var speaker = speakers.Where(_ => _.name == speakerName).FirstOrDefault();

            foreach (var style in speaker!.styles) {
                try {
                    builder.AddOption(style.name, $"id@{speakerName}@{style.name}", $"selected speaker : {speakerName}");
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }

            return builder;
        }
    }
}
