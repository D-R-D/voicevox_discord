using CliWrap;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System.Diagnostics;
using System.Security.Permissions;
using System.Xml.Linq;

namespace voicevox_discord
{
    internal class AudioService
    {   
        //internal Dictionary<ulong, AudioServiceData> GuildAudioService = new Dictionary<ulong, AudioServiceData>();
        internal List<KeyValuePair<ulong, AudioServiceData>> GuildAudioService = new();

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        public void initGuildService(ulong guildid)
        {
            KeyValuePair<ulong, AudioServiceData> AudioService = new(guildid, new AudioServiceData());
            GuildAudioService.Add(AudioService);
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        private bool CheckGuildService(ulong guildid)
        {
            foreach(KeyValuePair<ulong, AudioServiceData> AudioService in GuildAudioService)
            {
                if(AudioService.Key == guildid)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        /// <param name="_voicevoxEngineApi"></param>
        private void setnewEngine(ulong guildid, string _engine_name)
        {
            if (!CheckGuildService(guildid))
            {
                initGuildService(guildid);
            }

            KeyValuePair<ulong, AudioServiceData> audioService = new();
            foreach (KeyValuePair<ulong, AudioServiceData> AudioService in GuildAudioService)
            {
                if (AudioService.Key == guildid)
                {
                    audioService = AudioService;
                }
            }

            audioService.Value.SetEngine(SpeakerInfo.GetEngineApiFromengine_name(_engine_name));
            audioService.Value.SetEngineName(_engine_name);
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="command"></param>
        /// <param name="firstval"></param>
        /// <returns></returns>
        public async Task JoinOperation(SocketSlashCommand command, string firstval)
        {
            ulong guildid = command.GuildId!.Value;
            if (!CheckGuildService(guildid))
            {
                initGuildService(guildid);
            }
            KeyValuePair<ulong, AudioServiceData> audioService = new();
            foreach (KeyValuePair<ulong, AudioServiceData> AudioService in GuildAudioService)
            {
                if (AudioService.Key == guildid)
                {
                    audioService = AudioService;
                }
            }
            AudioServiceData audioServiceData = audioService.Value;

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
                    if (audioServiceData.audioclient != null)
                    {
                        if (audioServiceData.audioclient.ConnectionState == ConnectionState.Connected)
                        {
                            rejoin = true;

                            await audioServiceData.audioclient.StopAsync();
                            audioServiceData.speaking = false;
                            audioServiceData.voiceChannel = null;
                        }
                    }
                    audioServiceData.voiceChannel = ((IVoiceState)command.User).VoiceChannel;
                    await JoinChannel(rejoin, audioServiceData, guildid);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await command.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
                }
            }

            if (firstval == "leave")
            {
                if (audioServiceData.audioclient == null || audioServiceData.audioclient!.ConnectionState != ConnectionState.Connected)
                {
                    await command.ModifyOriginalResponseAsync(m => { m.Content = "どこにも参加してないよ"; });
                }
                await command.ModifyOriginalResponseAsync(m => { m.Content = "退出します"; });
                await LeaveAsync(guildid);
                audioServiceData.voiceChannel = null;

                return;
            }

            await command.DeleteOriginalResponseAsync();
        }

        private async Task JoinChannel(bool rejoin, AudioServiceData audioServiseData, ulong guildid)
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
                    await PlayAudio(audioServiseData, guildid, text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task LeaveAsync(ulong guildid)
        {
            if (!CheckGuildService(guildid))
            {
                initGuildService(guildid);
            }
            KeyValuePair<ulong, AudioServiceData> audioService = new();
            foreach (KeyValuePair<ulong, AudioServiceData> AudioService in GuildAudioService)
            {
                if (AudioService.Key == guildid)
                {
                    audioService = AudioService;
                }
            }
            AudioServiceData audioServiseData = audioService.Value;
            await audioServiseData.audioclient!.StopAsync();

            audioServiseData.voiceChannel = null;
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task Audioclient_Disconnected(Exception arg)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        /// <param name="engine_name"></param>
        /// <param name="name"></param>
        /// <param name="style_name"></param>
        /// <param name="id"></param>
        public void SetSpeaker(ulong guildid, string name, string style_name, int id, string engine_name)
        {
            setnewEngine(guildid, engine_name);
            KeyValuePair<ulong, AudioServiceData> audioService = new();
            foreach (KeyValuePair<ulong, AudioServiceData> AudioService in GuildAudioService)
            {
                if (AudioService.Key == guildid)
                {
                    audioService = AudioService;
                }
            }
            AudioServiceData audioServiceData = audioService.Value;
            audioServiceData.SetSpeakerInfo(name, style_name, id);

            Console.WriteLine(name + " : " + id);

            if (audioServiceData.audioclient == null || audioServiceData.audioclient!.ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await PlayAudio(audioServiceData, guildid, $"変わりまして{audioServiceData.name}です。{audioServiceData.style_name}な感じで行きますね。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="audioServiseData"></param>
        public void Chat_ChangeInitialMessage(AudioServiceData audioServiseData)
        {
            audioServiseData.chatGpt!.SetInitialMessage($"以降の会話では{audioServiseData.name}として{audioServiseData.style_name}な感じにふるまってください。");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="OPENAI_APIKEY"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task Chat(SocketSlashCommand command, string OPENAI_APIKEY, string text)
        {
            if (!command.GuildId.HasValue)
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "悪いな、私はguild専用なんだ"; });

                return;
            }
            ulong guildid = command.GuildId.Value;
            if (!CheckGuildService(guildid))
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "ギルドIDが登録されてないね、/setspeakerとか/voicechannelをためしてみて！！"; });

                return;
            }
            KeyValuePair<ulong, AudioServiceData> audioService = new();
            foreach (KeyValuePair<ulong, AudioServiceData> AudioService in GuildAudioService)
            {
                if (AudioService.Key == guildid)
                {
                    audioService = AudioService;
                }
            }
            AudioServiceData audioServiceData = audioService.Value;

            if (audioServiceData.audioclient == null || audioServiceData.audioclient!.ConnectionState != ConnectionState.Connected)
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "チャンネルに接続できてないよ"; });

                return;
            }

            if(audioServiceData.chatGpt == null)
            {
                audioServiceData.initChatGpt(OPENAI_APIKEY);
            }
            audioServiceData.setInitialMessage();

            try
            {
                string response = await audioServiceData.chatGpt!.RequestSender(text);
                await command.ModifyOriginalResponseAsync(m => { m.Content = $"{response}"; });
                await PlayAudio(audioServiceData, guildid, response);
            }
            catch(Exception ex)
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content = $"{ex.Message}\n何故だ！\n俺にも分からない！！\n答えろ！！\n教えてくれ！！\n答えろ！！！\nボスぅぅ！！！！"; });
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// use
        /// </summary>
        /// <param name="command"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task TextReader(SocketSlashCommand command, string text)
        {
            if (!command.GuildId.HasValue)
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "悪いな、私はguild専用なんだ"; });

                return;
            }
            ulong guildid = command.GuildId.Value;
            if (!CheckGuildService(guildid))
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "ギルドIDが登録されてないね、/setspeakerとか/voicechannelをためしてみて！！"; });

                return;
            }
            KeyValuePair<ulong, AudioServiceData> audioService = new();
            foreach (KeyValuePair<ulong, AudioServiceData> AudioService in GuildAudioService)
            {
                if (AudioService.Key == guildid)
                {
                    audioService = AudioService;
                }
            }
            AudioServiceData audioServiseData = audioService.Value;

            if (audioServiseData.audioclient == null || audioServiseData.audioclient!.ConnectionState != ConnectionState.Connected)
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "チャンネルに接続できてないよ"; });

                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await PlayAudio(audioServiseData, guildid, text);
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


        private async Task PlayAudio(AudioServiceData audioServiseData, ulong guildid, string text)
        {
            if(audioServiseData.speaking == true)
            {
                return;
            }
            audioServiseData.speaking = true;

            string audiofile = $"{Directory.GetCurrentDirectory()}/ffmpeg/audiofile/{guildid}.wav";
            audioServiseData.voicevoxEngineApi!.Info();
            using (Stream wavstream = audioServiseData.voicevoxEngineApi!.GetWavFromApi(audioServiseData.id.ToString(), text))
            using (var wfr = new WaveFileReader(wavstream))
            {
                WaveFileWriter.CreateWaveFile(audiofile, wfr);
            }

            Process process = CreateStream(guildid);
            using (var output = process.StandardOutput.BaseStream)
            //using (var output = new MemoryStream())
            {
                //await Cli.Wrap($"{ffmpegdir}/ffmpeg.exe").WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1").WithStandardInputPipe(PipeSource.FromStream(wavstream)).WithStandardOutputPipe(PipeTarget.ToStream(output)).WithValidation(CommandResultValidation.None).ExecuteAsync();
                try { await output.CopyToAsync(audioServiseData.audiooutstream!); }
                finally { await audioServiseData.audiooutstream!.FlushAsync(); }
                //var s = new RawSourceWaveStream(output, new WaveFormat(48000, 2));
                //WaveFileWriter.CreateWaveFile("pcmtest.wav", s);
            }
            process.Kill();

            audioServiseData.speaking = false;
        }

        private Process CreateStream(ulong guildid)
        {
            string ffmpegdir = $"{Directory.GetCurrentDirectory()}/ffmpeg";

            return Process.Start(new ProcessStartInfo
            {
                FileName = $"{ffmpegdir}/ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{ffmpegdir}/audiofile/{guildid}.wav\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            })!;
        }
    }
}