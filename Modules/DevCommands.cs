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

namespace FinBot.Modules
{
    public class DevCommands : ModuleBase<ShardedCommandContext>
    {
        private DiscordShardedClient _client;

        public DevCommands(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();
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
    }
}
