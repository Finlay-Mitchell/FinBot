using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

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
            _discord.MessageReceived += OnLogMessage;
            _discord.MessageDeleted += OnMessageDelete;
            _discord.MessageReceived += AddToDB;
        }

        public async Task AddToDB(SocketMessage arg)
        {

            if (arg.Author.IsBot || arg.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }

            SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
            long Now = Global.ConvertToTimestamp(arg.Timestamp.UtcDateTime);
            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);
            MySqlConnection queryConn = new MySqlConnection(Global.MySQL.connStr);

            try
            {
                conn.Open();
                long TimeStamp = 0;
                long XP = 0;
                long level = 0;
                bool ran = false;
                long xpToNextLevel = 0;
                long totalXP = 0;

                MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM Levels WHERE userId = {arg.Author.Id} AND guildId = {chan.Guild.Id}", conn);
                MySqlDataReader reader = cmd1.ExecuteReader();

                while (reader.Read())
                {
                    ran = true;
                    TimeStamp = Now - reader.GetInt64(2);

                    if (TimeStamp >= Global.MinMessageTimestamp)
                    {
                        XP = reader.GetInt64(4);
                        level = reader.GetInt64(3);
                        Random r = new Random();
                        XP += r.Next(15, 25);
                        totalXP = +XP;
                        xpToNextLevel = (long)(5 * Math.Pow(level, 2) + 50 * level + 100);


                        if (XP >= xpToNextLevel)
                        {
                            level += 1;
                            XP = XP - xpToNextLevel;
                            await arg.Channel.SendMessageAsync($"Congratulations, {arg.Author.Mention} for reaching level {level}!");
                        }

                        queryConn.Open();
                        MySqlCommand cmd2 = new MySqlCommand($"UPDATE Levels SET LastValidTimestamp = {Now}, level = {level}, XP = {XP}, totalXP = {totalXP} WHERE guildId = {chan.Guild.Id} AND userId = {arg.Author.Id}", queryConn);
                        cmd2.ExecuteNonQuery();
                        queryConn.Close();
                    }

                    else
                    {
                        return;
                    }
                }

                if (!ran)
                {
                    Random r = new Random();
                    totalXP =+ r.Next(15, 25);
                    queryConn.Open();
                    MySqlCommand cmd2 = new MySqlCommand($"INSERT INTO Levels(userId, guildId, LastValidTimestamp, level, XP, totalXP) VALUES({arg.Author.Id}, {chan.Guild.Id}, {Now}, 0, {XP}, {totalXP})", queryConn);
                    cmd2.ExecuteNonQuery();
                    queryConn.Close();

                }

                conn.Close();
            }

            catch (Exception ex)
            {
                if (ex.Message.GetType() != typeof(NullReferenceException))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithAuthor(arg.Author);
                    eb.WithTitle("Error sending deatils to database:");
                    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                    eb.WithCurrentTimestamp();
                    eb.WithColor(Color.Red);
                    eb.WithFooter("Please DM the bot ```support <issue>``` about this error and the developers will look at your ticket");
                    await arg.Channel.SendMessageAsync("", false, eb.Build());
                    return;
                }
            }
        }

        private async Task OnMessageDelete(Cacheable<IMessage, ulong> msg, ISocketMessageChannel arg2)
        {
            /*
             * Database consists of:
             * message TEXT
             * ts BIGINT(rename)
             * guildId BIGINT
             * author BIGINT
             * author BIGINT
             */
            
            int MySqlSuccess = -9;
            SocketUserMessage author = (SocketUserMessage)await msg.GetOrDownloadAsync();
            SocketGuildChannel sGC = (SocketGuildChannel)arg2;
            string messagecontent = msg.HasValue ? msg.Value.Content : "Unable to retrieve message";
            string fields = "";
            List<string> content = new List<string>();

            if (msg.HasValue)
            {
                if (author.Embeds.Count > 0)
                {
                    IEmbed message = author.Embeds.First();
                    var embed = message.ToEmbedBuilder();

                    if (embed.Fields.Count > 0)
                    {
                        foreach (var field in embed.Fields)
                        {
                            fields += $"**{field.Name}**\n{field.Value}\n";
                        }
                    }

                    content.Add(string.IsNullOrEmpty(embed.Title) ? "" : $"**{embed.Title}**");
                    content.Add(string.IsNullOrEmpty(embed.Description) ? "" : embed.Description);
                    fields = string.IsNullOrEmpty(fields) ? "" : $"\n{fields}";
                    content.Add(string.IsNullOrEmpty(author.Content) ? "" : author.Content);
                    content.Add(string.IsNullOrEmpty(embed.Footer.Text) ? "" : embed.Footer.Text);
                    messagecontent = $"{content[2]}\n\n{content[0]}\n\n{content[1]}\n{fields}\n{content[3]}";
                }
            }

            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);
            MySqlConnection QueryConn = new MySqlConnection(Global.MySQL.connStr);

            try
            {
                conn.Open();
                QueryConn.Open();
                long Now = Global.ConvertToTimestamp(DateTimeOffset.Now.UtcDateTime);
                bool IsEmpty = true;
                MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM SnipeLogs WHERE guildId = '{sGC.Guild.Id}'", conn);
                MySqlDataReader reader = (MySqlDataReader)await cmd1.ExecuteReaderAsync();

                if (messagecontent.Contains("'"))
                {
                    messagecontent = Regex.Replace(messagecontent, "'", "\"");
                }

                while (reader.Read())
                {                    
                    IsEmpty = false;
                    MySqlCommand cmd = new MySqlCommand($"UPDATE SnipeLogs SET MessageTimestamp = {Now}, message = '{messagecontent}', author = {author.Author.Id} WHERE guildId = {sGC.Guild.Id}", QueryConn);

                    MySqlSuccess = cmd.ExecuteNonQuery();
                    QueryConn.Close();
                }

                if (IsEmpty)
                {
                    MySqlCommand cmd = new MySqlCommand($"INSERT INTO SnipeLogs(message, MessageTimestamp, guildId, author) VALUES('{messagecontent}', {Now}, {sGC.Guild.Id}, {author.Author.Id})", QueryConn);
                    MySqlSuccess = await cmd.ExecuteNonQueryAsync();
                    QueryConn.Close();
                }

                conn.Close();
            }

            catch (Exception ex)
            {
                if (ex.Message.GetType() != typeof(NullReferenceException))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithAuthor(author.Author);
                    eb.WithTitle("Error logging deleted message:");
                    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                    eb.WithCurrentTimestamp();
                    eb.WithColor(Color.Red);
                    eb.WithFooter("Please DM the bot ```support <issue>``` about this error and the developers will look at your ticket");
                    await arg2.SendMessageAsync("", false, eb.Build());
                    return;
                }
            }

            if (MySqlSuccess != 1)
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor(author.Author);
                eb.WithTitle("Error logging deleted message:");
                eb.WithDescription($"The database returned a {MySqlSuccess} error code");
                eb.WithCurrentTimestamp();
                eb.WithColor(Color.Red);
                eb.WithFooter("Please DM the bot ```support <issue>``` about this error and the developers will look at your ticket");
                await arg2.SendMessageAsync("", false, eb.Build());
                return;
            }

            string logMessage = $"[DELETED]User: [{author.Author.Username}]<->[{author.Author.Id}] Discord Server: [{sGC.Guild.Name}/{arg2}] -> [{messagecontent}]";
            _logger.LogDebug(logMessage);
            
            return;
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
