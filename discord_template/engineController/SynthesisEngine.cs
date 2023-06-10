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
        // 全話者名を返す
        public abstract Task<List<string>> GetSpeakers();
        //
        // 話者名16要素で1ページとするリストを返す
        public abstract Task<List<string>> GetPagedSpeakers(int page);
        //
        // 指定されたページの存在確認
        public abstract Task<bool> SpeakerPageExist(int page);
        //
        // 指定されたUUIDに対応する話者名を返す
        public abstract Task<string> GetSpeakerUUID(string speakername);


        //
        // 全スタイル名のリストを返す
        public abstract Task<List<string>> GetStyles(string speakername);
        //
        // スタイル名を16要素で1ページとするリストにして返す
        public abstract Task<List<string>> GetPagedStyles(string speakername, int page);
        //
        // 指定されたページの存在確認
        public abstract Task<bool> StylePageExist(string speakername, int page);
        //
        // 話者スタイルIDを返す
        public abstract Task<int> GetStyleId(string name, string style);
    }
}
