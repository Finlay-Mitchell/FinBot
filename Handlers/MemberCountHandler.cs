using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Timers;

namespace FinBot.Handlers
{
    class MemberCountHandler
    {
        private CommandService _commands;
        private DiscordShardedClient _client;
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        MongoClient MongoClient = new MongoClient(Global.mongoconnstr);

        public MemberCountHandler(IServiceProvider services)
        {
            Timer t = new Timer() { AutoReset = true, Interval = new TimeSpan(0, 0, 7, 5).TotalMilliseconds, Enabled = true };
            _services = services;
            _client = services.GetRequiredService<DiscordShardedClient>();
            _commands = services.GetRequiredService<CommandService>();
            _logger = services.GetRequiredService<ILogger<CommandHandler>>();


            t.Enabled = true;
            t.Elapsed += HandleUserCount;
            t.Start();
        }


        private async void HandleUserCount(object sender, ElapsedEventArgs e)
        {
            try
            {
                SocketGuild guild = _client.GetGuild(0000);
                SocketVoiceChannel chn = guild.GetVoiceChannel(0000);

                if (chn == null)
                {
                    return;
                }

                string msg = $"Total Users: {guild.Users.Count}";

                if (chn.Name != msg)
                {
                    await chn.ModifyAsync(x => x.Name = msg);
                }
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                return;
            }
        }
    }
}
