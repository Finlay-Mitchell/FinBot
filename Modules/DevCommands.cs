using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using FinBot;
using System.Threading.Tasks;
using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Discord.Rest;
using MySql.Data.MySqlClient;
using FinBot.Services;
using System.Net.Http;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using FinBot.Handlers;
using System.Timers;
using Discord.Audio;

using VideoLibrary;
using MediaToolkit.Model;
using MediaToolkit;

namespace FinBot.Modules
{
    public class DevCommands : ModuleBase<ShardedCommandContext>
    {
        private DiscordShardedClient _client;
        Timer T;

        public DevCommands(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();

            //T = new Timer() { AutoReset = true, Interval = new TimeSpan(0, 0, 0, 30).TotalMilliseconds, Enabled = true };
            //T.Elapsed += HandleTopicAsync;

        }

        private async void HandleTopicAsync(object sender, ElapsedEventArgs e)
        {
            await _client.GetGuild(725886999646437407).GetTextChannel(725896089542197278).SendMessageAsync($"Here's yer fucking latency, bitch {_client.Latency}");
            Global.ConsoleLog($"Here's your fucking latency, bitchass {_client.Latency}");
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
                    eb.WithColor(Color.Purple);
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

                    var audioClient = await channel.ConnectAsync();

                    SaveMP3($"{Environment.CurrentDirectory}", "https://www.youtube.com/watch?v=-ZReLaWESAE", "test");

                    await SendAsync(audioClient, $"{Environment.CurrentDirectory}/test.mp3");
                }

                catch (Exception ex)
                {
                    await ReplyAsync("You fucking failure at life, look at what you've done! No reason your parents hate you. Here's your fucking error, not just talking about your existance\n" + ex.Message);
                }
            }
        }

        private void SaveMP3(string SaveToFolder, string VideoURL, string MP3Name)
        {
            var source = @SaveToFolder;
            var youtube = YouTube.Default;
            var vid = youtube.GetVideo(VideoURL);
            File.WriteAllBytes(source + vid.FullName, vid.GetBytes());

            var inputFile = new MediaFile { Filename = source + vid.FullName };
            var outputFile = new MediaFile { Filename = $"{MP3Name}.mp3" };

            using (var engine = new Engine())
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

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }
    }
}