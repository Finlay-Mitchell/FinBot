using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using FinBot.Handlers.Automod;
using System.Data.SQLite;
using FinBot.Handlers.AutoMod;

namespace FinBot.Handlers
{
    class CommandHandler : ModuleBase<SocketCommandContext>
    {
        public static DiscordSocketClient _client;
        public static CommandService _service;
        public static IConfigurationRoot _config = null;

        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _service = new CommandService();
            _service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
            _client.MessageReceived += LogMessage;
            _client.Log += Client_Log;
            _client.GuildMembersDownloaded += GMD;
            _client.Ready += Init;
            _client.MessageReceived += HandleCommandAsync;
            _client.MessageDeleted += AddToSnipe;
            Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] - " + "Services loaded");
        }

        private async Task AddToSnipe(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            SocketGuildChannel sGC = (SocketGuildChannel)arg2;
            SocketUserMessage msg = (SocketUserMessage)await arg1.GetOrDownloadAsync();
            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.SnipeLogs}");
            conn.Open();
            using var cmd = new SQLiteCommand(conn);
            using var cmd1 = new SQLiteCommand(conn);
            long Now = Global.ConvertToTimestamp(DateTimeOffset.Now.UtcDateTime);
            cmd1.CommandText = $"SELECT * FROM SnipeLogs WHERE guildId = '{sGC.Guild.Id}'";
            using SQLiteDataReader reader = cmd1.ExecuteReader();
            bool IsEmpty = true;
            string finalmsg = "";

            while (reader.Read())
            {
                finalmsg = msg.Content;

                if (msg.Content.Contains("'"))
                {
                    finalmsg = Regex.Replace(msg.Content, "'", "\"");
                }

                IsEmpty = false;
                cmd.CommandText = $"UPDATE SnipeLogs SET timestamp = {Now}, message = '{finalmsg}', author = '{msg.Author.Id}' WHERE guildId = '{sGC.Guild.Id}'";
                cmd.ExecuteNonQuery();
            }

            if (IsEmpty)
            {
                cmd.CommandText = $"INSERT INTO SnipeLogs(message, timestamp, guildId, author) VALUES ('{finalmsg}', {Now}, '{sGC.Guild.Id}', '{msg.Author.Id}')";
                cmd.ExecuteNonQuery();
            }

            conn.Close();
        }

        private Task LogMessage(SocketMessage arg)
        {
            Global.ConsoleLog("Message from: " + arg.Author + ": \"" + arg.Content + "\"", ConsoleColor.Magenta);
            return Task.CompletedTask;
        }

        public Task<int> GMD(SocketGuild arg)
        {
            Console.WriteLine("Guild Members downloaded " + arg);
            return Task.FromResult(0);
        }

        private Task Client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg))
            {
                return;
            }

            SocketCommandContext context = new SocketCommandContext(_client, msg);

            if (msg.Author.IsBot)
            {
                return;
            }

            int argPos = 0;

            if (msg.HasStringPrefix(Global.Prefix, ref argPos))
            {
                if (msg.Channel.GetType() == typeof(SocketDMChannel) && msg.Author.Id != 305797476290527235)
                {
                    try
                    {
                        await msg.Channel.SendMessageAsync("sorry but DM's do not accept bot commands.");
                    }

                    catch (Exception)
                    {

                    }
                }

                else
                {
                    IResult result = await _service.ExecuteAsync(context, argPos, null, MultiMatchHandling.Best);

                    if (!result.IsSuccess)
                    {
                        EmbedBuilder b = new EmbedBuilder
                        {
                            Color = Color.Red,
                            Description = $"The following info is the Command error info, `{msg.Author.Username}#{msg.Author.Discriminator}` tried to use the `{msg}` Command in {msg.Channel}: \n \n **COMMAND ERROR**: ```{result.Error.Value}``` \n \n **COMMAND ERROR REASON**: ```{result.ErrorReason}```",
                            Author = new EmbedAuthorBuilder()
                        };
                        b.Author.Name = msg.Author.Username + "#" + msg.Author.Discriminator;
                        b.Author.IconUrl = msg.Author.GetAvatarUrl();
                        b.Footer = new EmbedFooterBuilder
                        {
                            Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " ZULU"
                        };
                        b.Title = "Bot Command Error!";
                        await _client.GetGuild(799431538814222406).GetTextChannel(810885351223984168).SendMessageAsync("", false, b.Build());
                    }

                    if (!result.IsSuccess && result.Error == CommandError.BadArgCount)
                    {
                        string msgwp = Regex.Replace(msg.ToString(), $"{Global.Prefix}", "");
                        await msg.Channel.SendMessageAsync("", false, new EmbedBuilder()
                        {
                            Color = Color.LightOrange,
                            Title = "Bad arg count",
                            Description = $"Sorry, {msg.Author.Mention} but the command {msg.ToString().Split(' ').First()} does not take those parameters. Use the help command {Global.Prefix}help {msgwp.Split(' ').First()}",
                            Author = new EmbedAuthorBuilder()
                            {
                                Name = msg.Author.ToString(),
                                IconUrl = msg.Author.GetAvatarUrl(),
                                Url = msg.GetJumpUrl()
                            }
                        }
                        .WithFooter($"{result.Error.Value}")
                        .WithCurrentTimestamp()
                        .Build());
                    }
                }
            }
        }

        public async Task Init()
        {
            try
            {
                Console.WriteLine("Starting handler loading...");
                await StartHandlers();
                Global.ConsoleLog("Finnished Init!", ConsoleColor.Black, ConsoleColor.DarkGreen);
                Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] - " + "Command Handler ready");
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public bool FirstPass = false;

        public Task StartHandlers()
        {
            if (!FirstPass)
            {
                HelpHandler helpHandler = new HelpHandler(_service);
                ChatFilter chatFilter = new ChatFilter(_client);
                LevellingHandler levellingHandler = new LevellingHandler(_client);
                FirstPass = true;
            }

            return Task.CompletedTask;
        }
    }
}
