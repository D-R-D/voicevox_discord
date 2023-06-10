using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord.engineController
{

    // voicevox APIの話者オブジェクト
    public class VoicevoxSpeaker
    {
        public string? name { get; set; }
        public string? speaker_uuid { get; set; }
        public IList<VoicevoxSpeakerStyle>? styles { get; set; }
        public string? version { get; set; }
    }

    // Coeiroink-v2 APIの話者オブジェクト
    public class CoeiroinkSpeaker
    {
        public string? speakerName { get; set; }
        public string? speakerUuid { get; set; }
        public IList<CoeiroinkSpeakerStyle>? styles { get; set; }
        public string? version { get; set; }
        public string? base64Portrait { get; set; }
    }
}
