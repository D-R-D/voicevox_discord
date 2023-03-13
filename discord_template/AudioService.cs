using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System.Diagnostics;

namespace voicevox_discord
{
    internal class AudioServiseData
    {
        public string name { get; private set; } = "四国めたん";
        public string engine_name { get; private set; } = "voicevox";
        public string style_name { get; private set; } = "ノーマル";
        public int id { get; private set; } = 2;

        public VoicevoxEngineApi? voicevoxEngineApi { get; private set; } = null;

        public IVoiceChannel? voiceChannel { get; set; } = null;
        public IAudioClient? audioclient { get; set; } = null;
        public AudioOutStream? audiooutstream { get; set; } = null;

        public AudioServiseData(VoicevoxEngineApi _voicevoxEngineApi)
        {
            SetEngine(_voicevoxEngineApi);
        }

        public void SetEngine(VoicevoxEngineApi _voicevoxEngineApi)
        {
            voicevoxEngineApi = _voicevoxEngineApi;
        }

        public void SetSpeakerInfo(string _engine_name, string _name, string _style_name, int _id)
        {
            engine_name = _engine_name;
            style_name = _style_name;
            name = _name;
            id = _id;
        }
    }

    internal class AudioService
    {   
        internal Dictionary<ulong, AudioServiseData> GuildAudioService = new Dictionary<ulong, AudioServiseData>();

        private IVoiceChannel? voiceChannel = null;
        private IAudioClient? audioclient = null;
        internal KeyValuePair<string, int> name_id_pair = new("voicevox:四国めたん:ノーマル", 2);
        private AudioOutStream? audiooutstream = null;
        private VoicevoxEngineApi? voicevoxEngineApi = null;


        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        public void initGuildService(ulong guildid, VoicevoxEngineApi _voicevoxEngineApi)
        {
            GuildAudioService.Add(guildid, new AudioServiseData(_voicevoxEngineApi));
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        public bool CheckGuildService(ulong guildid)
        {
            if (GuildAudioService.ContainsKey(guildid))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        /// <param name="_voicevoxEngineApi"></param>
        public void initengine(ulong guildid, VoicevoxEngineApi _voicevoxEngineApi)
        {
            if (!CheckGuildService(guildid))
            {
                initGuildService(guildid, _voicevoxEngineApi);
            }

            GuildAudioService[guildid].SetEngine(_voicevoxEngineApi);

            voicevoxEngineApi = _voicevoxEngineApi;
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="command"></param>
        /// <param name="firstval"></param>
        /// <returns></returns>
        public async Task JoinOperation(SocketSlashCommand command, string firstval)
        {
            if (!command.GuildId.HasValue)
            {
                await command.DeleteOriginalResponseAsync();
                return;
            }
            ulong guildid = command.GuildId.Value;
            AudioServiseData audioServiseData = GuildAudioService[guildid];

            if (firstval == "join")
            {
                try
                {
                    if (((IVoiceState)command.User).VoiceChannel == null)
                    {
                        await command.ModifyOriginalResponseAsync(m => { m.Content = "有効ボイスチャンネル ゼロ\nイグジット完了…\n…止まった"; });

                        return;
                    }

                    bool rejoin = false;
                    if (audioServiseData.audioclient != null)
                    {
                        if (audioServiseData.audioclient.ConnectionState == ConnectionState.Connected)
                        {
                            rejoin = true;

                            await audioServiseData.audioclient.StopAsync();
                            audioServiseData.voiceChannel = null;
                        }
                    }
                    audioServiseData.voiceChannel = ((IVoiceState)command.User).VoiceChannel;
                    await JoinChannel(rejoin, audioServiseData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await command.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
                }
            }

            if (firstval == "leave")
            {
                if (audioclient == null || audioclient!.ConnectionState != ConnectionState.Connected)
                {
                    await command.ModifyOriginalResponseAsync(m => { m.Content = "どこにも参加してないよ"; });
                }
                await command.ModifyOriginalResponseAsync(m => { m.Content = "退出します"; });
                await LeaveAsync();
                voiceChannel = null;

                return;
            }

            await command.DeleteOriginalResponseAsync();
        }

        public async Task JoinChannel(bool rejoin, AudioServiseData audioServiseData)
        {
            audioServiseData.audioclient = await audioServiseData.voiceChannel!.ConnectAsync().ConfigureAwait(false);
            audioServiseData.audiooutstream = audioServiseData.audioclient.CreatePCMStream(AudioApplication.Mixed);
            audioServiseData.audioclient.Disconnected += Audioclient_Disconnected;

            _ = Task.Run(async () =>
            {
                string text = $"{audioServiseData.name}です。{audioServiseData.style_name}な感じで行きますね。";

                if (rejoin)
                {
                    text = $"{audioServiseData.name}です。再起動してきました。";
                }


                try
                {
                    await PlayAudio(audioServiseData, text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }
        private async Task Audioclient_Disconnected(Exception arg)
        {
            audioclient!.Dispose();
            voiceChannel = null;

            await Task.CompletedTask;
        }

        public async Task LeaveAsync()
        {
            await audioclient!.StopAsync();

            voiceChannel = null;
            return;
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void SetSpeaker(string name, int id)
        {
            if (Tools.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            name_id_pair = new KeyValuePair<string, int>(name, id);

            Console.WriteLine(name + " : " + id);

            if (audioclient == null || audioclient!.ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await PlayAudio(, $"変わりまして{name_id_pair.Key.Split(":")[1]}です。{name_id_pair.Key.Split(":")[2]}な感じで行きますね。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }

        /// <summary>
        /// use
        /// </summary>
        /// <param name="command"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task TextReader(SocketSlashCommand command, string text)
        {


            if (audioclient == null || audioclient!.ConnectionState != ConnectionState.Connected)
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "チャンネルに接続できてないよ"; });

                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await PlayAudio(voicevoxEngineApi!, text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await command.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });

                    return;
                }
            });


            await command.DeleteOriginalResponseAsync();
        }


        private async Task PlayAudio(AudioServiseData audioServiseData, string text)
        {
            string audiofile = $"{Directory.GetCurrentDirectory()}/ffmpeg/audiofile/source.wav";
            using (Stream wavstream = audioServiseData.voicevoxEngineApi!.GetWavFromApi(name_id_pair.Value.ToString(), text))
            using (var wfr = new WaveFileReader(wavstream))
            {
                WaveFileWriter.CreateWaveFile(audiofile, wfr);
            }

            Process process = CreateStream();
            using (var output = process.StandardOutput.BaseStream)
            //using (var output = new MemoryStream())
            {
                //await Cli.Wrap($"{ffmpegdir}/ffmpeg.exe").WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1").WithStandardInputPipe(PipeSource.FromStream(wavstream)).WithStandardOutputPipe(PipeTarget.ToStream(output)).WithValidation(CommandResultValidation.None).ExecuteAsync();
                try { await output.CopyToAsync(audiooutstream!); }
                finally { await audiooutstream!.FlushAsync(); }
                //var s = new RawSourceWaveStream(output, new WaveFormat(48000, 2));
                //WaveFileWriter.CreateWaveFile("pcmtest.wav", s);
            }
            process.Kill();
        }

        private Process CreateStream()
        {
            string ffmpegdir = $"{Directory.GetCurrentDirectory()}/ffmpeg";

            return Process.Start(new ProcessStartInfo
            {
                FileName = $"{ffmpegdir}/ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{ffmpegdir}/audiofile/source.wav\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            })!;
        }
    }
}