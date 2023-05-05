using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord
{
    internal class InitDirectory
    {
        public void init()
        {
            checkdir("commands");
            checkdir("audiofile");
            checkdir("save");

            checkfile("save/GuildSpeaker.json");
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
                File.Create($"{Directory.GetCurrentDirectory()}/{filename}");
            }
        }
    }
}
