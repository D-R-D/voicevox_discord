namespace voicevox_discord
{
    internal class InitDirectory
    {
        public void init()
        {
            checkdir("commands");
            checkdir("audiofile");
            checkdir("save");

            checkdir("save/setting");
            checkdir("save/dictionary");

            checkfile("save/setting/GuildSpeaker.json");
        }

        private void checkdir(string dirname)
        {
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}/{dirname}"))
            {
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}/{dirname}");
            }
        }

        private void checkfile(string filename)
        {
            if (!File.Exists($"{Directory.GetCurrentDirectory()}/{filename}"))
            {
                File.Create($"{Directory.GetCurrentDirectory()}/{filename}").Close();
            }
        }
    }
}
