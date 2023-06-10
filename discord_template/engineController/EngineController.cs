using voicevox_discord.engineController;

namespace voicevox_discord.engines
{
    public class EngineController
    {
        public SynthesisEngine Engine { get; private set; }
        public string engineType { get; private set; }

        private EngineController(string ip, int port, string name, string type)
        {
            if (Tools.IsNullOrEmpty(ip)) { throw new ArgumentNullException("ipaddress"); }
            if (Tools.IsNotPortNumber(port)) { throw new Exception($"port がポート番号でない、もしくはNullです。"); }
            if (Tools.IsNullOrEmpty(name)) { throw new ArgumentNullException("name"); }

            switch (type)
            {
                case "coeiroink":
                    Engine = new CoeiroinkEngineApi(ip, port, name);
                    break;

                case "voicevox":
                    Engine = new VoicevoxEngineApi(ip, port, name);
                    break;

                default:
                    throw new Exception($"name:{name}\ntype: {type} は不正なエンジンtypeです。");
            }

            engineType = type;
        }

        public static EngineController Create(string ip, int port, string name, string type)
        {
            return new EngineController(ip, port, name, type);
        }
    }
}
