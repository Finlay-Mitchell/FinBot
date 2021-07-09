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

        public async Task StartAsync()
        {
            if (string.IsNullOrWhiteSpace(Global.Token))
            {
                throw new Exception("Token missing from config.json! Please enter your token there (root directory)");
            }

            await _discord.LoginAsync(TokenType.Bot, Global.Token);
            await _discord.StartAsync();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            Global.clientCommands = false;
        }
    }
}
