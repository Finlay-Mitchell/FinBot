using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;
using FinBot.Handlers;
using FinBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using FinBot.Handlers.AutoMod;
using FinBot.Interactivity;
using System.IO;
using System.Text;
using Serilog.Sinks.File.Archive;
using System.IO.Compression;

namespace FinBot
{
    class Program
    {
        /// <summary>
        /// Entry point for the program.
        /// </summary>
        /// <param name="args">Startup arguments.</param>
        static void Main(string[] args)
        {
            Console.Title = "Finbot"; //Set the console title, because why not?

            while (true)
            {
                try
                {
                    //Start the bot.
                    new Program().RunBotAsync().GetAwaiter().GetResult();
                }

                catch (Exception ex)
                {
                    //Log the exception to the console and retry after 5 seconds.
                    Global.ConsoleLog($"Exception: \"{ex}\"\n\n Retrying...", ConsoleColor.Red, ConsoleColor.Black);
                    Thread.Sleep(5000);
                }
            }
        }

        /// <summary>
        /// Handles the startup and configuration of the bot.
        /// </summary>
        public async Task RunBotAsync()
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"[{DateTime.Now.TimeOfDay}] - Welcome, {Environment.UserName}");
            Global.ReadConfig(); //Reads json.config file, this setting lots of our global members which host information that could be sensitive/subject to change such as API keys and the Discord bot token.
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
                        GatewayIntents.GuildPresences |
                        GatewayIntents.GuildScheduledEvents |
                        GatewayIntents.DirectMessageReactions |
                        GatewayIntents.DirectMessages |
                        GatewayIntents.DirectMessageTyping,
                    LogLevel = LogSeverity.Error,
                    MessageCacheSize = 1000,
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    DefaultRunMode = Discord.Commands.RunMode.Async,
                    LogLevel = LogSeverity.Verbose,
                    CaseSensitiveCommands = false,
                    ThrowOnError = false,
                }))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordShardedClient>(), new InteractionServiceConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    ThrowOnError = false,
                }))
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
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
                .AddSingleton<StartupService>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<RedditHandler>()
                .AddSingleton<YouTubeModel>()
                .AddSingleton<ChatFilter>()
                .AddSingleton<HelpHandler>()
                .AddSingleton<MuteService>()
                .AddSingleton<ReminderService>()
                .AddSingleton<UserHandler>()
                .AddSingleton<MemberCountHandler>()
                .AddSingleton<StatusHandler>()
                .AddSingleton<ReminderService>()
                .AddSingleton<MuteService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<ShutdownService>()
                .AddSingleton<TwitchHandler>()
                .AddSingleton<TwitchService>()
                .AddSingleton<AFKHandler>()
                .AddSingleton<SuggestionHandler>()
                .AddSingleton<MessageCountHandler>()
                .AddSingleton<AutoSlowmode>()
                .AddSingleton<GuildDeleteHandler>()
                .AddSingleton<LoggingService>();
            ConfigureServices(services);
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetRequiredService<LoggingService>();
            await serviceProvider.GetRequiredService<StartupService>().StartAsync();
            serviceProvider.GetRequiredService<CommandHandler>();

            DiscordShardedClient client = serviceProvider.GetRequiredService<DiscordShardedClient>();
            InteractionService commands = serviceProvider.GetRequiredService<InteractionService>();

            client.ShardReady += async (DiscordSocketClient arg) =>
            {
                if (true)
                {
                    Global.ConsoleLog($"Registered commands to guild - {811919861537964032}");
                    await commands.RegisterCommandsToGuildAsync(811919861537964032, true);
                }

                else
                {
                    await commands.RegisterCommandsGloballyAsync(true); //Registers commands to all guilds the bot is in - can take up to a few hours for commands to be fully integrated.
                }
            };

            await serviceProvider.GetRequiredService<CommandHandler>().InitializeAsync(); // Initialises interaction services.
            await Task.Delay(Timeout.Infinite); //This blocks the program until it is closed.
        }

        /// <summary>
        /// Configures the services for the bot.
        /// </summary>
        /// <param name="services">The services.</param>
        private static void ConfigureServices(IServiceCollection services)
        {
            //Read level type from config.json
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
                .WriteTo.Async(a => a.File(@$"Data/Logs/logs-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: Global.RetainedLogFileCount, rollOnFileSizeLimit: true, 
                hooks: new ArchiveHooks(CompressionLevel.Optimal, @$"Data/Logs/Archived"), buffered: true))
                .MinimumLevel.Is(level)
                .CreateLogger();
        }
    }
}
