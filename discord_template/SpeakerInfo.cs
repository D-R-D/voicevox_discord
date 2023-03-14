using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord
{
    internal class SpeakerInfo
    {
        public static VoicevoxEngineApi GetEngineApiFromEngineName(string engineName)
        {
            VoicevoxEngineApi voicevoxEngineApi = new VoicevoxEngineApi(Program.s_EngineDictionary[engineName].Key, Program.s_EngineDictionary[engineName].Value, engineName);
            return voicevoxEngineApi;
        }
    }
}
