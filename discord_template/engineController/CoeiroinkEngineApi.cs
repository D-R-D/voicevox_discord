using voicevox_discord.engineController;

namespace voicevox_discord.engines
{
    internal class CoeiroinkEngineApi : SynthesisEngine
    {
        private readonly string m_EngineIPAddress;
        private readonly int m_EnginePort;
        private readonly string m_EngineName;

        private IList<CoeiroinkSpeaker>? m_Speakers;

        public CoeiroinkEngineApi(string ipAddress, int port, string name)
        {
            m_EngineIPAddress = ipAddress;
            m_EnginePort = port;
            m_EngineName = name;
        }

        //
        // エンジンのインスタンスを作成する
        public override SynthesisEngine Create(string ipAddress, int port, string name)
        {
            throw new NotImplementedException();
        }

        //
        //
        public override void WriteInfo()
        {
            Console.WriteLine($"[{m_EngineName}.Info]@{m_EngineIPAddress}:{m_EnginePort}");
        }

        //
        //
        public override void LoadSpeakers()
        {
            throw new NotImplementedException();
        }

        //
        //Wavファイルを取得してStreamで返す
        public override Task<Stream> GetWavFromApi(string uuid, int id, string text)
        {
            throw new NotImplementedException();
        }


        //
        // 
        public override async Task<List<string>> GetSpeakers()
        {
            while(m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.Select(_ => _.speakerName).ToList()!;
        }
        //
        //
        public override async Task<List<string>> GetPagedSpeakers(int page)
        {
            while(m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.Select(_ => _.speakerName).ToArray().Skip(page * 16).Take(16).ToList()!;
        }
        //
        //
        public override async Task<bool> SpeakerPageExist(int page)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            if (page < 0)
            {
                return false;
            }
            return m_Speakers.Count() > page * 16;
        }
        //
        //
        public override async Task<string> GetSpeakerUUID(string speakername)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.First(_ => _.speakerName == speakername).speakerUuid!;
        }


        //
        //
        public override async Task<List<string>> GetStyles(string speakername)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.First(_ => _.speakerName == speakername).styles!.Select(_ => _.styleName).ToList()!;
        }
        //
        //
        public override async Task<List<string>> GetPagedStyles(string speakername, int page)
        {
            while(m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.First(_ => _.speakerName == speakername).styles!.Select(_ => _.styleName).Skip(page * 16).Take(16).ToList()!;
        }
        //
        // 
        public override async Task<bool> StylePageExist(string speakername, int page)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.First(_ => _.speakerName == speakername).styles!.Count() > page * 16;
        }
        //
        //
        public override async Task<int> GetStyleId(string name, string style)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }

            return (int)m_Speakers.First(_ => _.speakerName == name).styles!.First(_ => _.styleName == style).styleId!;
        }

    }
}