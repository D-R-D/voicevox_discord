using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.MediaFoundation;
using NAudio.Wave;
using System.Diagnostics;
using System.IO;
using System.Threading;
using CliWrap;
using Discord.Audio.Streams;
using System;

namespace voicevox_discord
{
    internal class AudioService
    {
        private IVoiceChannel? voiceChannel = null;
        private IAudioClient? audioclient = null;
        internal KeyValuePair<string, int> name_id_pair = new("voicevox:四国めたん:ノーマル", 2);
        private AudioOutStream? audiooutstream = null;
        private VoicevoxEngineApi? voicevoxEngineApi = null;

        public void initengine(VoicevoxEngineApi _voicevoxEngineApi)
        {
            voicevoxEngineApi = _voicevoxEngineApi;
        }

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
                    await PlayAudio(voicevoxEngineApi!, $"変わりまして{name_id_pair.Key.Split(":")[1]}です。{name_id_pair.Key.Split(":")[2]}な感じで行きますね。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }
        public async Task<bool> CommandRunner(SocketSlashCommand command, string firstval)
        {
            if (firstval == "join")
            {
                try
                {
                    bool rejoin = false;
                    if (audioclient != null)
                    {
                        if (audioclient!.ConnectionState == ConnectionState.Connected)
                        {
                            rejoin = true;

                            await audioclient!.StopAsync();
                            voiceChannel = null;
                        }
                    }
                    voiceChannel = ((IVoiceState)command.User).VoiceChannel;
                    await JoinChannel(rejoin);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await command.ModifyOriginalResponseAsync(m => { m.Content = ex.Message; });

                    return false;
                }

                return true;
            }

            if (firstval == "leave")
            {
                if (audioclient == null || audioclient!.ConnectionState != ConnectionState.Connected)
                {
                    await command.ModifyOriginalResponseAsync(m => { m.Content = "どこにも参加してないよ"; });
                    return false;
                }
                await command.ModifyOriginalResponseAsync(m => { m.Content = "退出します"; });
                await LeaveAsync();
                voiceChannel = null;

                return false;
            }

            return false;
        }

        public async Task JoinChannel(bool rejoin)
        {
            audioclient = await voiceChannel!.ConnectAsync().ConfigureAwait(false);
            audiooutstream = audioclient.CreatePCMStream(AudioApplication.Mixed);
            audioclient.Disconnected += Audioclient_Disconnected;

            _ = Task.Run(async () =>
            {
                string text = $"{name_id_pair.Key.Split(":")[1]}です。{name_id_pair.Key.Split(":")[2]}な感じで行きますね。";

                if (rejoin)
                {
                    text = $"{name_id_pair.Key.Split(":")[1]}です。再起動してきました。";
                }


                try
                {
                    await PlayAudio(voicevoxEngineApi!, text);
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


        private async Task PlayAudio(VoicevoxEngineApi _voicevoxEngineApi, string text)
        {
            string audiofile = $"{Directory.GetCurrentDirectory()}/ffmpeg/audiofile/source.wav";
            using (Stream wavstream = _voicevoxEngineApi!.GetWavFromApi(name_id_pair.Value.ToString(), text))
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