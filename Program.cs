using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FinBot.Handlers;
using FinBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using FinBot.Handlers.AutoMod;
using System.Diagnostics;

namespace FinBot
{
    class Program
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

        public async Task RunBotAsync()
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"[{DateTime.Now.TimeOfDay}] - Welcome, {Environment.UserName}");
            Global.ReadConfig();
            IServiceCollection services = new ServiceCollection()
                .AddSingleton(new DiscordShardedClient(new DiscordSocketConfig
                {
                    GatewayIntents =
                        GatewayIntents.GuildMembers |
                        GatewayIntents.GuildMessages |
                        GatewayIntents.GuildIntegrations |
                        GatewayIntents.Guilds |
                        GatewayIntents.GuildBans |
                        GatewayIntents.GuildVoiceStates |
                        GatewayIntents.GuildEmojis |
                        GatewayIntents.GuildInvites |
                        GatewayIntents.GuildMessageReactions |
                        GatewayIntents.GuildMessageTyping |
                        GatewayIntents.GuildWebhooks |
                        GatewayIntents.DirectMessageReactions |
                        GatewayIntents.DirectMessages |
                        GatewayIntents.DirectMessageTyping |
                        GatewayIntents.GuildPresences,
                    LogLevel = LogSeverity.Error,
                    MessageCacheSize = 1000,
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = RunMode.Async,
                    LogLevel = LogSeverity.Verbose,
                    CaseSensitiveCommands = false,
                    ThrowOnError = false
                }))
                .AddSingleton<StartupService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<RedditHandler>()
                .AddSingleton<YouTubeModel>()
                .AddSingleton<ChatFilter>()
                .AddSingleton<HelpHandler>()
                .AddSingleton<LoggingService>();

            ConfigureServices(services);
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<LoggingService>();
            await serviceProvider.GetRequiredService<StartupService>().StartAsync();
            serviceProvider.GetRequiredService<CommandHandler>();
            await Task.Delay(-1);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddSerilog());
            Serilog.Events.LogEventLevel level = Serilog.Events.LogEventLevel.Error;

            switch (Global.LoggingLevel.ToLower())
            {
                case "error":
                    level = Serilog.Events.LogEventLevel.Error;
                    break;

                case "info":
                    level = Serilog.Events.LogEventLevel.Information;
                    break;

                case "debug":
                    level = Serilog.Events.LogEventLevel.Debug;
                    break;

                case "crit":
                    level = Serilog.Events.LogEventLevel.Fatal;
                    break;

                case "warn":
                    level = Serilog.Events.LogEventLevel.Warning;
                    break;

                case "trace":
                    level = Serilog.Events.LogEventLevel.Debug;
                    break;
                default:
                    throw new Exception("Logging level is invalid");
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Is(level)
                .CreateLogger();
        }
    }
}
