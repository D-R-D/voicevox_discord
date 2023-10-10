using voicevox_discord.engineController;

namespace voicevox_discord.engines
{
    public class EngineController
    {
        public SynthesisEngine Engine { get; private set; }
        public EngineType.Engine engineType { get; private set; }

        private EngineController(string ip, int port, string name, EngineType.Engine type)
        {
            if (Tools.IsNullOrEmpty(ip)) { throw new ArgumentNullException("ipaddress"); }
            if (Tools.IsNotPortNumber(port)) { throw new Exception($"port がポート番号でない、もしくはNullです。"); }
            if (Tools.IsNullOrEmpty(name)) { throw new ArgumentNullException("name"); }

            if (type == EngineType.Engine.VOICEVOX || type == EngineType.Engine.COEIROINK_v1)
            {
                Engine = new VoicevoxEngineApi(ip, port, name);
            }
            else if(type == EngineType.Engine.COEIROINK_v2)
            {
                Engine = new CoeiroinkEngineApi(ip, port, name);
            }
            else
            {
                throw new Exception($"name:{name}\ntype: {type} は不正なエンジンtypeです。");
            }

            engineType = type;
        }

        public static EngineController Create(string ip, int port, string name, string type)
        {
            EngineType.Engine enginetype = EngineType.GetTypeFromName(type);

            return new EngineController(ip, port, name, enginetype);
        }
    }
}
