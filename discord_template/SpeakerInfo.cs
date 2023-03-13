using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord
{
    internal class SpeakerInfo
    {
        public static VoicevoxEngineApi GetEngineApiFromengine_name(string engine_name)
        {
            VoicevoxEngineApi voicevoxEngineApi = new VoicevoxEngineApi(Program.enginedictionary[engine_name].Key, Program.enginedictionary[engine_name].Value, engine_name);
            return voicevoxEngineApi;
        }
    }
}
