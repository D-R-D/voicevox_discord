using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System.Diagnostics;
using System.Text;
using System.Linq;
using NAudio.CoreAudioApi;

namespace voicevox_discord
{
    internal class AudioService
    {
        internal Dictionary<ulong, AudioServiceData> GuildAudioServiceDict = new Dictionary<ulong, AudioServiceData>();

        public AudioServiceData GetOrCreateAudioServiceData(ulong guildid)
        {
            if (!GuildAudioServiceDict.ContainsKey(guildid)) 
            {
                GuildAudioServiceDict.Add(guildid, AudioServiceData.Create(guildid));
                GuildAudioServiceDict[guildid].SetSavedSpeaker();
            }
            
            return GuildAudioServiceDict[guildid];
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
                            await LeaveAsync(guildid);
                        }
                    }

                    audioServiceData.voiceChannel = ((IVoiceState)command.User).VoiceChannel;
                    await JoinChannel(rejoin, audioServiceData, guildid);
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.ToString());

                    await command.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });
                    return;
                }

                await command.DeleteOriginalResponseAsync();
                return;
            }

            if (firstval == "leave") 
            {
                if (audioServiceData.audioclient == null || audioServiceData.audioclient!.ConnectionState != ConnectionState.Connected)
                {
                    await command.ModifyOriginalResponseAsync(m => { m.Content = "どこにも参加してないよ"; });
                    return;
                }
                await command.ModifyOriginalResponseAsync(m => { m.Content = "退出します"; });
                await LeaveAsync(guildid);
                return;
            }
        }
        
        private async Task JoinChannel(bool rejoin, AudioServiceData audioServiseData, ulong guildid) 
        {
            audioServiseData.audioclient = await audioServiseData.voiceChannel!.ConnectAsync(selfDeaf: true).ConfigureAwait(false);
            audioServiseData.audiooutstream = audioServiseData.audioclient.CreatePCMStream(AudioApplication.Mixed, packetLoss: 10);
            audioServiseData.IsSpeaking = false;

            _ = Task.Run(async () =>
            {
                string text = $"{audioServiseData.GuildSave.guildSpeaker.name}です。{audioServiseData.GuildSave.guildSpeaker.style}な感じで行きますね。";

                if (rejoin)
                {
                    text = $"{audioServiseData.GuildSave.guildSpeaker.name}です。再起動してきました。";
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
            AudioServiceData audioServiseData = GetOrCreateAudioServiceData(guildid);
            audioServiseData.voiceChannel = null;
            audioServiseData.IsSpeaking = false;
            await audioServiseData.audioclient!.StopAsync();

            await Task.CompletedTask;
        }

        /// <summary>
        /// setting
        /// </summary>
        /// <param name="guildid"></param>
        /// <param name="engineName"></param>
        /// <param name="speakerName"></param>
        /// <param name="styleName"></param>
        /// <param name="id"></param>
        public void SetSpeaker(ulong guildid, string speakerName, string styleName, int id, string engineName)
        {
            Task.Run(() =>
            {
                var target = GetOrCreateAudioServiceData(guildid);
                target.SetSpeaker(engineName, speakerName, styleName, id);

                if (target.audioclient == null || target.audioclient!.ConnectionState != ConnectionState.Connected)
                {
                    Console.WriteLine("client null");
                    target.SaveSpeaker();

                    return;
                }

                try 
                {
                    _ = PlayAudio(target, guildid, $"変わりまして{target.GuildSave.guildSpeaker.name}です。{target.GuildSave.guildSpeaker.style}な感じで行きますね。");
                    target.SaveSpeaker();
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
        /// <param name="command"></param>
        /// <param name="OPENAI_APIKEY"></param>
        /// <param name="text"></param>
        /// <param type="role"></param>
        /// <returns></returns>
        public async Task Chat(SocketSlashCommand command, string text)
        {
            ulong guildid = command.GuildId!.Value;
            AudioServiceData audioServiceData = GetOrCreateAudioServiceData(guildid);

            try 
            {
                string response = await audioServiceData.ChatGpt.RequestSender(text);
                if (response.Length > 2000)
                {
                    Optional<IEnumerable<FileAttachment>> optional = new();
                    using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(response)))
                    {
                        //ファイル添付に必用な処理
                        FileAttachment fa = new FileAttachment(stream, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
                        List<FileAttachment> flis = new List<FileAttachment> { fa };
                        optional = new Optional<IEnumerable<FileAttachment>>(flis);

                        response = $"文字数上限に達しました。脱法処理で表示します。\n[文字数: {response.Length}]";

                        await command.ModifyOriginalResponseAsync(m =>
                        {
                            m.Content = response;
                            m.Attachments = optional;
                        });
                    }
                }
                else
                {
                    await command.ModifyOriginalResponseAsync(m => { m.Content = $"{response}"; });
                }

                    if (audioServiceData.audioclient != null)
                if (audioServiceData.audioclient!.ConnectionState == ConnectionState.Connected)
                { 
                    await PlayAudio(audioServiceData, guildid, response);
                }
            }
            catch (Exception ex)
            {
                await command.ModifyOriginalResponseAsync(m => { m.Content += $"\n{ex.Message}\n何故だ！\n俺にも分からない！！\n答えろ！！\n教えてくれ！！\n答えろ！！！\nボスぅぅ！！！！"; });
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
            if (audioServiseData.IsSpeaking)
            {
                return;
            }
            audioServiseData.IsSpeaking = true;

            foreach (var dict in Settings.Shared.m_GuildDictionary[guildid])
            {
                text = text.Replace(dict.Key, dict.Value);
            }

            try
            {
                string audiofile = $"{Directory.GetCurrentDirectory()}/audiofile/{guildid}.wav";
                audioServiseData.EngineApi.WriteInfo();
                using (Stream wavstream = await audioServiseData.EngineApi!.GetWavFromApi(audioServiseData.GuildSave.guildSpeaker.uuid, audioServiseData.GuildSave.guildSpeaker.speakerId, text))
                using (var wfr = new WaveFileReader(wavstream))
                {
                    WaveFileWriter.CreateWaveFile(audiofile, wfr);
                }

                Process process = CreateStream(guildid);
                using (var output = process.StandardOutput.BaseStream)
                {
                    try
                    {
                        await audioServiseData.audioclient!.SetSpeakingAsync(true);
                        await output.CopyToAsync(audioServiseData.audiooutstream!);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        await audioServiseData.audiooutstream!.FlushAsync();
                        await audioServiseData.audioclient!.SetSpeakingAsync(false);
                    }
                }
                process.Kill();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            audioServiseData.IsSpeaking = false;
        }

        private Process CreateStream(ulong guildid)
        {
            string ffmpegdir = $"{Directory.GetCurrentDirectory()}/audiofile";

            return Process.Start(new ProcessStartInfo 
            {
                FileName = $"ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{ffmpegdir}/{guildid}.wav\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            })!;
        }
    }
}