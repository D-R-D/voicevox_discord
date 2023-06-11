using Discord;

namespace voicevox_discord
{
    public class SelectMenuEditor
    {
        /// <summary>
        /// エンジン一覧
        /// CustomId == "engine : {commandmode]"
        /// 
        /// ：ページ選択時：
        /// OptionValue == "page @ {pageNumber}"
        /// OptionValue == "engine @ {engineName}"
        /// </summary>
        /// <param name="page"></param>
        /// <param name="voiceChannel"></param>
        /// <returns></returns>
        public static async Task<SelectMenuBuilder> CreateEngineMenu(int page, string CommandMode)
        {
            SelectMenuBuilder builder = new SelectMenuBuilder().WithPlaceholder($"エンジン一覧 p.{page}").WithCustomId($"engine:{CommandMode}").WithMinValues(1).WithMaxValues(1);
            
            var engines = Settings.Shared.m_EngineList.Keys.Skip(16 * page).Take(16).ToArray();
            if (page > 0)
            {
                builder.AddOption("Previous page.", $"page@{page - 1}", $"Go to page {(page - 1)}.");
            }

            foreach ( var engineName in engines )
            {
                try
                {
                    builder.AddOption(engineName.ToUpper(), $"engine@{engineName}", $"{(await Settings.Shared.m_EngineList[engineName].Engine.GetSpeakers()).Count} params found.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (Settings.Shared.m_EngineList.Keys.ToArray().Count() > (16 * (page + 1)))
            {
                builder.AddOption("Next page.", $"page@{page + 1}", $"Go to page {page + 1}.");
            }

            return builder;
        }


        /// <summary>
        /// 話者一覧
        /// CustomId == "speaker_id : {engineName} : {commandmode]"
        /// 
        /// ：ページ選択時：
        /// OptionValue == "page@{pageNumber}"
        /// OptionValue == "speaker@{speakerName}"
        /// </summary>
        /// <param name="engineName"></param>
        /// <param name="page"></param>
        /// <param name="voiceChannel"></param>
        /// <returns></returns>
        public async static Task<SelectMenuBuilder> CreateSpeakerMenu(string engineName, int page, string CommandMode)
        {
            SelectMenuBuilder builder = new SelectMenuBuilder().WithPlaceholder($"話者一覧 p.{page}").WithCustomId($"speaker:{engineName}:{CommandMode}").WithMinValues(1).WithMaxValues(1);

            if (page > 0)
            {
                builder.AddOption("Previous page.", $"page@{page - 1}", $"Go to page {(page - 1)}.");
            }

            var speakers = await Settings.Shared.m_EngineList[engineName].Engine.GetPagedSpeakers(page);

            foreach (var speaker in speakers)
            {
                try
                {
                    var styles = await Settings.Shared.m_EngineList[engineName].Engine.GetStyles(speaker);
                    builder.AddOption(speaker, $"speaker@{speaker}", $"{styles.Count} params found.");
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (await Settings.Shared.m_EngineList[engineName].Engine.SpeakerPageExist(page + 1))
            {
                builder.AddOption("Next page.", $"page@{page + 1}", $"Go to page {page + 1}.");
            }

            return builder;
        }

        /// <summary>
        /// 話者のスタイル一覧
        /// CustomId == "speaker_id : {engineName} : {speakerName} : {commandmode]"
        /// 
        /// ：ページ選択時：
        /// OptionValue == "page@{pageNumber}"
        /// OptionValue == "id@{speakerName}@{styleName}"
        /// </summary>
        /// <param name="engineName"></param>
        /// <param name="speakerName"></param>
        /// <param name="page"></param>
        /// <param name="CommandMode"></param>
        /// <returns></returns>
        public async static Task<SelectMenuBuilder> CreateStyleMenu(string engineName, string speakerName, int page, string CommandMode)
        {
            SelectMenuBuilder builder = new SelectMenuBuilder().WithPlaceholder($"スタイル一覧 p.{page}").WithCustomId($"speaker_id:{engineName}:{speakerName}:{CommandMode}").WithMinValues(1).WithMaxValues(1);

            if (page > 0)
            {
                builder.AddOption("Previous page.", $"page@{page - 1}", $"Go to page {(page - 1)}.");
            }

            var styles = await Settings.Shared.m_EngineList[engineName].Engine.GetPagedStyles(speakerName, page);

            foreach (var style in styles) 
            {
                try
                {
                    builder.AddOption(style, $"id@{style}", $"selected speaker : {speakerName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (await Settings.Shared.m_EngineList[engineName].Engine.StylePageExist(speakerName, page + 1))
            {
                builder.AddOption("Next page.", $"page@{page + 1}", $"Go to page {page + 1}.");
            }

            return builder;
        }
    }
}
