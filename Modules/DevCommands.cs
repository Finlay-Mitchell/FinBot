using Discord;
using Discord.Commands;
using DiscColour = Discord.Color;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Timers;
using Discord.Audio;
using VideoLibrary;
using MediaToolkit.Model;
using MediaToolkit;
using System.Drawing;
using System.Drawing.Imaging;

using Colour = System.Drawing.Color;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FinBot.Modules
{
    public class DevCommands : ModuleBase<ShardedCommandContext>
    {
        private DiscordShardedClient _client;
        private IAudioClient _audioClient;
        Timer T;
        private IAudioClient _userVoiceClient;
        private IAudioChannel _userVoiceChannel;

        bool ready = false;

        public DevCommands(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();

            if (ready)
            {
                _audioClient.SpeakingUpdated += StartAYSTTimer;
                T = new Timer() { AutoReset = true, Interval = new TimeSpan(0, 0, 0, 10).TotalMilliseconds, Enabled = true };
                T.Elapsed += SendAYST;

            }
        }

        [Command("testing")]
        public async Task testing()
        {
            string rand = "";
            using (var rng = new RNGCryptoServiceProvider())
            {
                int bit_count = (16 * 6);
                int byte_count = ((bit_count + 7) / 8); // rounded up
                byte[] bytes = new byte[byte_count];
                rng.GetBytes(bytes);
                rand = Convert.ToBase64String(bytes);
            }

            rand = Regex.Replace(rand, "[\"/]", "");
            rand = Regex.Replace(rand, @"[\\]", "");
            await ReplyAsync(rand);
            //2000, 510
            //Bitmap bitmap = new Bitmap(2000, 1000, PixelFormat.Format32bppPArgb);
            Bitmap bitmap = new Bitmap(1050, 550);
            Graphics graphics = Graphics.FromImage(bitmap);
            //Pen pen = new Pen(Colour.FromKnownColor(KnownColor.Blue), 2);
            Pen pen = new Pen(Colour.FromArgb(255, 0, 0, 0), 5);
            //1000, 500
            graphics.DrawRectangle(pen, 10, 10, 750, 500);


            //Pen pen1 = new Pen(Colour.FromKnownColor(KnownColor.Red), 2);
            //graphics.DrawEllipse(pen1, 10, 10, 900, 700);
            string file = $"image_{rand}.png";
            bitmap.Save(file);
            await Context.Channel.SendFileAsync(file);
            File.Delete(file);
        }

        private async void SendAYST(object sender, ElapsedEventArgs e)
        {
            _userVoiceClient = await _userVoiceChannel.ConnectAsync();
            SaveMP3($"{Environment.CurrentDirectory}", "https://www.youtube.com/watch?v=-ZReLaWESAE", "AYST");
            await SendAsync(_userVoiceClient, $"{Environment.CurrentDirectory}/AYST.mp3", Context.Message);
        }

        private Task StartAYSTTimer(ulong userId, bool isSpeaking)
        {
            T.Stop();
            T.Start();
            return Task.CompletedTask;
        }

        [Command("restart")]
        public async Task Reset([Remainder] string reason = "No reason provided.")
        {
            if (Global.IsDev(Context.User))
            {
                Process currentProcess = Process.GetCurrentProcess();
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync($"Restarting bot with reason \"{reason}\"\nKilled Process {Process.GetProcessById(Global.processes.ProcessID).ProcessName}: {Global.processes.ProcessID}\n" +
                    $"Killed process: {currentProcess.ProcessName}: {currentProcess.Id}");
                Process.Start($"{AppDomain.CurrentDomain.FriendlyName}.exe");
                Process.GetProcessById(Global.processes.ProcessID).Kill();
                Environment.Exit(1);
            }
        }

        [Command("terminate")]
        public async Task term()
        {
            if (Global.IsDev(Context.User))
            {
                await Context.Message.ReplyAsync($"Shutting down services.\nShutting down {Process.GetProcessById(Global.processes.ProcessID).ProcessName}: PID[{Global.processes.ProcessID}]...\n" +
                    $"Shutting down {Process.GetCurrentProcess().ProcessName}: PID[{Process.GetCurrentProcess().Id}]...");
                Process.GetProcessById(Global.processes.ProcessID).Kill();
                Environment.Exit(1);
            }
        }

        [Command("updateSupport")]
        public async Task UpdateSupport(ulong guildId, ulong msgId, [Remainder] string msg)
        {
            if (Global.IsDev(Context.User))
            {
                try
                {
                    SocketDMChannel chn = (SocketDMChannel)await _client.GetDMChannelAsync(guildId);
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Support ticket update");
                    eb.WithFooter($"Support ticket update for {msgId}");
                    eb.WithCurrentTimestamp();
                    eb.WithDescription(msg);
                    eb.WithColor(DiscColour.Purple);
                    await chn.SendMessageAsync("", false, eb.Build());
                    await Context.Message.ReplyAsync("Sent support message response successfully");
                }

                catch (Exception ex)
                {
                    await Context.Message.ReplyAsync($"I encountered an error trying to respond to that support message. Here are the details:\n{ex.Message}\n{ex.Source}");
                }
            }
        }

        [Command("tld")] //boilerplate code for python TLD module
        public Task tld(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("exec")] //more boilerplate
        public Task exec(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("ayst")]
        public async Task ARST(IVoiceChannel channel = null)
        {
            if (Global.IsDev(Context.User))
            {
                try
                {
                    channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;

                    if (channel == null)
                    {
                        await Context.Message.ReplyAsync("User must be in a voice channel, or you must parse in a voice channel");
                        return;
                    }

                    IAudioClient audioClient = await channel.ConnectAsync();
                    SaveMP3($"{Environment.CurrentDirectory}", "https://www.youtube.com/watch?v=-ZReLaWESAE", "AYST");
                    await SendAsync(audioClient, $"{Environment.CurrentDirectory}/AYST.mp3", Context.Message);
                }

                catch (Exception ex)
                {
                    await ReplyAsync("Uh oh, stinkie! error poop:\n" + ex.Message);
                }
            }
        }

        private void SaveMP3(string SaveToFolder, string VideoURL, string MP3Name)
        {
            string source = @SaveToFolder;
            YouTube youtube = YouTube.Default;
            YouTubeVideo vid = youtube.GetVideo(VideoURL);
            File.WriteAllBytes(source + vid.FullName, vid.GetBytes());
            MediaFile inputFile = new MediaFile { Filename = source + vid.FullName };
            MediaFile outputFile = new MediaFile { Filename = $"{MP3Name}.mp3" };

            using (Engine engine = new Engine())
            {
                engine.GetMetadata(inputFile);

                engine.Convert(inputFile, outputFile);
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private async Task SendAsync(IAudioClient client, string path, IUserMessage message)
        {
            using (Process ffmpeg = CreateStream(path))
            using (Stream output = ffmpeg.StandardOutput.BaseStream)
            
            using (AudioOutStream discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try 
                {
                    await output.CopyToAsync(discord); 
                }

                finally
                {
                    await discord.FlushAsync(); 
                }
            }

            try
            {
                File.Delete(path);
            }

            catch(Exception ex)
            {
                await message.ReplyAsync($"You numpty, you've only gone and found yourself an error!\n\n {ex.Message}");
            }
        }

        [Command("Speaking")]
        public async Task speaking()
        {
            if (Global.IsDev(Context.User))
            {
                IVoiceChannel channel = (Context.User as IGuildUser)?.VoiceChannel;
                IAudioClient audioClient = await channel.ConnectAsync();
                await Context.Message.ReplyAsync($"{audioClient.ConnectionState}");
                await audioClient.SetSpeakingAsync(true);
            }
        }

        [Command("setAudioClient")]
        public async Task setAudioClient()
        {
            if (Global.IsDev(Context.User))
            {
                _audioClient = await (Context.User as IGuildUser)?.VoiceChannel.ConnectAsync();
                ready = true;
                await ReplyAsync("Success");
            }
        }
    }
}