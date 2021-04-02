using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace FinBot.Services
{
    public class LoggingService
    {
        private readonly ILogger _logger;
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;

        public LoggingService(ILogger<LoggingService> logger, DiscordShardedClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            _logger = logger;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
            _discord.ShardReady += OnShardReady;
            _discord.MessageDeleted += AddToSnipe;
            _discord.MessageReceived += OnLogMessage;
            _discord.MessageDeleted += OnMessageDelete;
        }

        private async Task OnMessageDelete(Cacheable<IMessage, ulong> msg, ISocketMessageChannel arg2)
        {
            SocketUserMessage author = (SocketUserMessage)await msg.GetOrDownloadAsync();
            SocketGuildChannel sGC = (SocketGuildChannel)arg2;
            string messagecontent = msg.HasValue ? msg.Value.Content : "Unable to retrieve message";
            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);
            conn.Open();
            long Now = Global.ConvertToTimestamp(DateTimeOffset.Now.UtcDateTime);
            bool IsEmpty = true;
            MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM snipelogs WHERE guildId = '{sGC.Guild.Id}'", conn);
            MySqlDataReader reader = cmd1.ExecuteReader();

            if (messagecontent.Contains("'"))
            {
                messagecontent = Regex.Replace(messagecontent, "'", "\"");
            }

            while (reader.Read())
            {
                IsEmpty = false;

                MySqlCommand cmd = new MySqlCommand($"UPDATE snipelogs SET ts = {Now}, message = '{messagecontent}', author = {author.Author.Id} WHERE guildId = {sGC.Guild.Id}", conn);
                cmd.ExecuteNonQuery();
            }

            if (IsEmpty)
            {
                MySqlCommand cmd = new MySqlCommand($"INSERT INTO snipelogs(message, ts, guildId, author) VALUES('{messagecontent}', {Now}, {sGC.Guild.Id}, {author.Author.Id})", conn);
                cmd.ExecuteNonQuery();
            }

            conn.Close();
        }

        private Task OnLogMessage(SocketMessage arg)
        {
            if (arg.Channel.GetType() == typeof(SocketTextChannel))
            {
                SocketGuildChannel gC = (SocketGuildChannel)arg.Channel;
                string logMessage = $"User: [{arg.Author.Username}]<->[{arg.Author.Id}] Discord Server: [{gC.Guild.Name}/{arg.Channel}] -> [{arg.Content}]";
                _logger.LogDebug(logMessage);
            }

            else
            {
                SocketDMChannel gC = (SocketDMChannel)arg.Channel;
                string logMessage = $"User: [{arg.Author.Username}]<->[{arg.Author.Id}] DM channel: [{arg.Channel}] -> [{arg.Content}]";
                _logger.LogDebug(logMessage);
            }

            return Task.CompletedTask;
        }

        private async Task AddToSnipe(Cacheable<IMessage, ulong> arg1, ISocketMessageChannel arg2)
        {
            SocketGuildChannel sGC = (SocketGuildChannel)arg2;
            //await ReplyAsync(arg1.Value.ToString());
            //RestUserMessage msg = (RestUserMessage)await arg1.GetOrDownloadAsync();

            //await ReplyAsync(msg.Content);

            //SQLiteConnection conn = new SQLiteConnection($"data source = {Global.SnipeLogs}");
            //conn.Open();
            //using var cmd = new SQLiteCommand(conn);
            //using var cmd1 = new SQLiteCommand(conn);
            //long Now = Global.ConvertToTimestamp(DateTimeOffset.Now.UtcDateTime);
            //cmd1.CommandText = $"SELECT * FROM SnipeLogs WHERE guildId = '{sGC.Guild.Id}'";
            //using SQLiteDataReader reader = cmd1.ExecuteReader();
            //bool IsEmpty = true;
            //string finalmsg = "";

            //while (reader.Read())
            //{
            //    finalmsg = msg.Content;

            //    if (msg.Content.Contains("'"))
            //    {
            //        finalmsg = Regex.Replace(msg.Content, "'", "\"");
            //    }

            //    IsEmpty = false;
            //    cmd.CommandText = $"UPDATE SnipeLogs SET timestamp = {Now}, message = '{finalmsg}', author = '{msg.Author.Id}' WHERE guildId = '{sGC.Guild.Id}'";
            //    cmd.ExecuteNonQuery();
            //}

            //if (IsEmpty)
            //{
            //    cmd.CommandText = $"INSERT INTO SnipeLogs(message, timestamp, guildId, author) VALUES ('{finalmsg}', {Now}, '{sGC.Guild.Id}', '{msg.Author.Id}')";
            //    cmd.ExecuteNonQuery();
            //}

            //conn.Close();
        }

        private Task OnShardReady(DiscordSocketClient arg)
        {
            _logger.LogInformation($"Connected as -> {arg.CurrentUser.Username}");
            _logger.LogInformation($"We are on {arg.Guilds.Count} servers");
            return Task.CompletedTask;
        }

        public Task OnLogAsync(LogMessage msg)
        {
            string logText = $"{msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";

            switch (msg.Severity.ToString())
            {
                case "Critical":
                        _logger.LogCritical(logText);
                        break;

                case "Warning":
                        _logger.LogWarning(logText);
                        break;

                case "Info":
                        _logger.LogInformation(logText);
                        break;
                    
                case "Verbose":
                        _logger.LogInformation(logText);
                        break;
                    
                case "Debug":
                        _logger.LogDebug(logText);
                        break;
                    
                case "Error":
                        _logger.LogError(logText);
                        break;
            }
            return Task.CompletedTask;
        }
    }
}
