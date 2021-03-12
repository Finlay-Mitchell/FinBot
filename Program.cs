using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FinBot.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FinBot
{
    class Program : ModuleBase<SocketCommandContext>
    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    new Program().RunBotAsync().GetAwaiter().GetResult();
                }

                catch (Exception ex)
                {
                    Global.ConsoleLog($"Exception: \"{ex}\"\n\n Retrying...", ConsoleColor.Red, ConsoleColor.Black);
                    Thread.Sleep(5000);
                }
            }
        }

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private CommandHandler _commandhandler;

        public async Task RunBotAsync()
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] - ");
            Console.WriteLine($"Welcome, {Environment.UserName}");
            Global.ReadConfig();

            _commands = new CommandService();
            _client = new DiscordSocketClient();
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<InfractionMessageHandler>()
                .BuildServiceProvider();
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 99999,
            });
            _client.Log += Log;
            await _client.LoginAsync(TokenType.Bot, Global.Token);
            await _client.StartAsync();
            Global.Client = _client;
            _commandhandler = new CommandHandler(_client);
            Console.WriteLine($"[{ DateTime.Now.TimeOfDay}] - Command Handler ready");
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            if (msg.Message == null)
            {
                return Task.CompletedTask;
            }

            if (!msg.Message.StartsWith("Received Dispatch"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[Svt: {msg.Severity} Src: {msg.Source} Ex: {msg.Exception}] - " + msg.Message);
            }

            return Task.CompletedTask;
        }
    }
}