using Discord.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public override Task<List<string>> GetSpeakers()
        {
            throw new NotImplementedException();
        }
        //
        //
        public override Task<List<string>> GetPagedSpeakers(int page)
        {
            throw new NotImplementedException();
        }
        //
        //
        public override Task<int> GetStyleId(string name, string style)
        {
            throw new NotImplementedException();
        }
        //
        //
        public override async Task<bool> SpeakerPageExist(int page)
        {
            if (page < 0)
            {
                return false;
            }
            while (m_Speakers == null)
            {
                await Task.Yield();
            }
            return m_Speakers.ToArray().Length > page * 16;
        }
        //
        //
        public override async Task<string> GetSpeakerUUID(string speakername)
        {
            while (m_Speakers == null)
            {
                await Task.Yield();
            }

            return m_Speakers.Where(_ => _.speakerName == speakername).First().speakerUuid;
        }


        //
        //
        public override Task<List<string>> GetStyles(string speakername)
        {
            throw new NotImplementedException();
        }
        //
        //
        public override Task<List<string>> GetPagedStyles(string speakername, int page)
        {
            throw new NotImplementedException();
        }
        //
        // 
        public override Task<bool> StylePageExist(string speakername, int page)
        {
            throw new NotImplementedException();
        }

    }
}