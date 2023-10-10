using System.Runtime.CompilerServices;

namespace voicevox_discord.engineController
{
    public static class EngineType
    {
        public enum Engine : int
        {
            VOICEVOX = 0,
            COEIROINK_v1,
            COEIROINK_v2,
        }

        public static readonly string[] EngineNames =
        {
            "voicevox",
            "coeiroink-v1",
            "coeiroink-v2"
        };

        public static Engine GetTypeFromName(string EngineName)
        {
            for(int engineIndex = 0; engineIndex < EngineNames.Length; engineIndex++)
            {
                if (EngineNames[engineIndex] == EngineName)
                {
                    return (Engine)engineIndex;
                }
            }

            throw new ArgumentException("UNKNOWN EngineName.");
        }

        public static string GetNameFromType(Engine EngineType)
        {
            foreach(var engineType in Enum.GetValues(typeof(Engine)))
            {
                if (engineType.Equals(engineType))
                {
                    return EngineNames[(int)engineType];
                }
            }

            throw new ArgumentException("UNKNOWN EngineType.");
        }
    }
}
