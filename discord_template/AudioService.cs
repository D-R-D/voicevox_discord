using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System.Diagnostics;

namespace voicevox_discord
{
    internal class AudioService
    {
        internal Dictionary<ulong, AudioServiceData> GuildAudioServiceDict = new Dictionary<ulong, AudioServiceData>();

        private AudioServiceData GetOrCreateAudioServiceData(ulong guildid)
        {
            if (!GuildAudioServiceDict.ContainsKey(guildid)) {
                GuildAudioServiceDict.Add(guildid, new AudioServiceData());
            }
            return GuildAudioServiceDict[guildid];
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        /// <param name="_voicevoxEngineApi"></param>
        private void SetNewEngine(ulong guildid, string engineName)
        {
            var target = GetOrCreateAudioServiceData(guildid);

            target.SetEngine(SpeakerInfo.GetEngineApiFromEngineName(engineName));
            target.SetEngineName(engineName);
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
            AudioServiceData audioServiceData = GetOrCreateAudioServiceData(guildid);

            if (firstval == "join") {
                try {
                    if (((IVoiceState)command.User).VoiceChannel == null) {
                        await command.ModifyOriginalResponseAsync(m => { m.Content = "有効ボイスチャンネル ゼロ\nイグジット完了…\n…止まった"; });

                        return;
                    }

                    bool rejoin = false;
                    if (audioServiceData.audioclient != null) {
                        if (audioServiceData.audioclient.ConnectionState == ConnectionState.Connected) {
                            rejoin = true;

                            await audioServiceData.audioclient.StopAsync();
                            audioServiceData.speaking = false;
                            audioServiceData.voiceChannel = null;
                        }
                    }
                    audioServiceData.voiceChannel = ((IVoiceState)command.User).VoiceChannel;
                    await JoinChannel(rejoin, audioServiceData, guildid);
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    await command.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
                }
            }

            if (firstval == "leave") {
                if (audioServiceData.audioclient == null || audioServiceData.audioclient!.ConnectionState != ConnectionState.Connected) {
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

            _ = Task.Run(async () => {
                string text = $"{audioServiseData.name}です。{audioServiseData.style_name}な感じで行きますね。";

                if (rejoin) {
                    text = $"{audioServiseData.name}です。再起動してきました。";
                }

                try {
                    await PlayAudio(audioServiseData, guildid, text);
                } catch (Exception ex) {
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
            AudioServiceData audioServiseData  = GetOrCreateAudioServiceData(guildid);
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
            SetNewEngine(guildid, engine_name);
            AudioServiceData audioServiceData = GetOrCreateAudioServiceData(guildid);
            audioServiceData.SetSpeakerInfo(name, style_name, id);

            Console.WriteLine(name + " : " + id);

            if (audioServiceData.audioclient == null || audioServiceData.audioclient!.ConnectionState != ConnectionState.Connected) {
                return;
            }

            _ = Task.Run(async () => {
                try {
                    await PlayAudio(audioServiceData, guildid, $"変わりまして{audioServiceData.name}です。{audioServiceData.style_name}な感じで行きますね。");
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="OPENAI_APIKEY"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task Chat(SocketSlashCommand command, string text)
        {
            ulong guildid = command.GuildId!.Value;
            AudioServiceData audioServiceData  = GetOrCreateAudioServiceData(guildid);

            if (audioServiceData.audioclient == null || audioServiceData.audioclient!.ConnectionState != ConnectionState.Connected) {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "チャンネルに接続できてないよ"; });
                return;
            }

            try {
                string response = await audioServiceData.ChatGPT.RequestSender(text);
                await command.ModifyOriginalResponseAsync(m => { m.Content = $"{response}"; });
                await PlayAudio(audioServiceData, guildid, response);
            } catch (Exception ex) {
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
            ulong guildid = command.GuildId!.Value;
            AudioServiceData audioServiseData = GetOrCreateAudioServiceData(guildid);

            if (audioServiseData.audioclient == null || audioServiseData.audioclient!.ConnectionState != ConnectionState.Connected) {
                await command.ModifyOriginalResponseAsync(m => { m.Content = "チャンネルに接続できてないよ"; });
                return;
            }

            _ = Task.Run(async () => {
                try {
                    await PlayAudio(audioServiseData, guildid, text);
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    await command.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
                    return;
                }
            });

            await command.DeleteOriginalResponseAsync();
        }


        private async Task PlayAudio(AudioServiceData audioServiseData, ulong guildid, string text)
        {
            if (audioServiseData.speaking == true) {
                return;
            }
            audioServiseData.speaking = true;

            string audiofile = $"{Directory.GetCurrentDirectory()}/ffmpeg/audiofile/{guildid}.wav";
            audioServiseData.voicevoxEngineApi!.WriteInfo();
            using (Stream wavstream = audioServiseData.voicevoxEngineApi!.GetWavFromApi(audioServiseData.id.ToString(), text))
            using (var wfr = new WaveFileReader(wavstream)) {
                WaveFileWriter.CreateWaveFile(audiofile, wfr);
            }

            Process process = CreateStream(guildid);
            using (var output = process.StandardOutput.BaseStream)
            //using (var output = new MemoryStream())
            {
                //await Cli.Wrap($"{ffmpegdir}/ffmpeg.exe").WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1").WithStandardInputPipe(PipeSource.FromStream(wavstream)).WithStandardOutputPipe(PipeTarget.ToStream(output)).WithValidation(CommandResultValidation.None).ExecuteAsync();
                try { await output.CopyToAsync(audioServiseData.audiooutstream!); } finally { await audioServiseData.audiooutstream!.FlushAsync(); }
                //var s = new RawSourceWaveStream(output, new WaveFormat(48000, 2));
                //WaveFileWriter.CreateWaveFile("pcmtest.wav", s);
            }
            process.Kill();

            audioServiseData.speaking = false;
        }

        private Process CreateStream(ulong guildid)
        {
            string ffmpegdir = $"{Directory.GetCurrentDirectory()}/ffmpeg";

            return Process.Start(new ProcessStartInfo {
                FileName = $"{ffmpegdir}/ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{ffmpegdir}/audiofile/{guildid}.wav\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            })!;
        }
    }
}