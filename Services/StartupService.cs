using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace FinBot.Services
{
    public class StartupService
    {
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public StartupService(IServiceProvider services)
        {
            _services = services;
            _discord = _services.GetRequiredService<DiscordShardedClient>();
            _commands = _services.GetRequiredService<CommandService>();
        }

        /// <summary>
        /// Starts the bot up.
        /// </summary>
        public async Task StartAsync()
        {
            if (string.IsNullOrWhiteSpace(Global.Token))
            {
                throw new Exception("Token missing from config.json! Please enter your token there (root directory)");
            }

            Global.clientCommands = false; // By default, this is set to false for extra security - meaning that calling a command via the bot is not possible.
            //Global.LoadPrefixes(); // Loads prefixes from the guildprefixes.load file to the Global.demandPrefixes variable.

            try
            {
                await _discord.LoginAsync(TokenType.Bot, Global.Token);
            }

            catch
            {
                throw new Exception("Invalid token");
            }

            await _discord.StartAsync();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
    }
}
