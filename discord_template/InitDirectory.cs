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

            checkdir("ffmpeg");
            checkdir("ffmpeg/audiofile");
        }

        private void checkdir(string dirname)
        {
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}/{dirname}"))
            {
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}/{dirname}");
            }
        }
    }
}
