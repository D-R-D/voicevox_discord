using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord.engineController
{
    public abstract class SynthesisEngine
    {
        //
        // インスタンスを作成する
        public abstract SynthesisEngine Create(string ipAddress, int port, string name);
        //
        // 話者を読み込む
        public abstract void LoadSpeakers();
        //
        // エンジン情報を標準出力する
        public abstract void WriteInfo();
        //
        // Wavファイルを取得してStreamで返す
        public abstract Task<Stream> GetWavFromApi(string uuid,int id, string text);


        //
        // 
        public abstract Task<List<string>> GetSpeakers();
        //
        // 指定されたpage * 16要素より後の16要素を返す
        public abstract Task<List<string>> GetPagedSpeakers(int page);
        //
        // 
        public abstract Task<bool> SpeakerPageExist(int page);
        //
        //
        public abstract Task<string> GetSpeakerUUID(string speakername);


        //
        //
        public abstract Task<List<string>> GetStyles(string speakername);
        //
        // 
        public abstract Task<List<string>> GetPagedStyles(string speakername, int page);
        //
        //
        public abstract Task<bool> StylePageExist(string speakername, int page);
        //
        // 話者スタイルIDを返す
        public abstract Task<int> GetStyleId(string name, string style);
    }


#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
    public class Speaker
    {
        public string name { get; set; }
        public string speaker_uuid { get; set; }
        public IList<Style> styles { get; set; }
        public string version { get; set; }
    }
    public class Style
    {
        public string name { get; set; }
        public int id { get; set; }
    }

    public class CoeiroinkSpeaker
    {
        public string speakerName { get; set; }
        public string speakerUuid { get; set; }
        public IList<CoeiroinkStyle> styles { get; set; }
        public string version { get; set; }
        public string base64Portrait { get; set; }
    }
    public class CoeiroinkStyle
    {
        public string styleName { get; set; }
        public int styleId { get; set; }
        public string base64Icon { get; set; }
        public string base64Portrait { get; set; }
    }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
}
