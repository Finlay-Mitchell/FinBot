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
using MongoDB.Driver;
using MongoDB.Bson;

namespace FinBot.Services
{
    public class LoggingService
    {
        private readonly ILogger _logger;
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

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
            _discord.MessageReceived += OnMessageReceived;
        }

        /// <summary>
        /// Appends data to the Levels database.
        /// </summary>
        /// <param name="type">The option for what kind of interaction is made with the database.</param>
        /// <param name="conn">The connection string to the database.</param>
        /// <param name="arg">The users message.</param>
        /// <param name="level">The level for the user.</param>
        /// <param name="XP">The XP for the user.</param>
        /// <param name="totalXP">The total XP for the user.</param>
        private async Task AddToDatabase(uint type, MySqlConnection conn, SocketMessage arg, long level, long XP, long totalXP)
        {
            try
            {
                SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
                long Now = Global.ConvertToTimestamp(arg.Timestamp.UtcDateTime);

                if (type == 0)
                {
                    MySqlCommand cmd = new MySqlCommand($"UPDATE Levels SET LastValidTimestamp = {Now}, level = {level}, XP = {XP}, totalXP = {totalXP} WHERE guildId = {chan.Guild.Id} AND userId = {arg.Author.Id}", conn);
                    cmd.ExecuteNonQuery();
                }

                else
                {
                    MySqlCommand cmd = new MySqlCommand($"INSERT INTO Levels(userId, guildId, LastValidTimestamp, level, XP, totalXP) VALUES({arg.Author.Id}, {chan.Guild.Id}, {Now}, 0, {XP}, {totalXP})", conn);
                    cmd.ExecuteNonQuery();
                }
            }

            catch(Exception ex)
            {
                await arg.Channel.SendMessageAsync(ex.Message);
                Global.ConsoleLog(ex.Message);
            }
        }

        /// <summary>
        /// Adds data to the snipelogs database.
        /// </summary>
        /// <param name="type">The option for what kind of interaction is made with the database.</param>
        /// <param name="conn">The connection string to the database.</param>
        /// <param name="content">The content to append to the database.</param>
        /// <param name="message">The message that we're appending.</param>
        /// <param name="socketGuildChannel">The channel where the message was deleted.</param>
        private async Task AddToSnipe(MySqlConnection conn, string content, SocketUserMessage message, SocketGuildChannel socketGuildChannel)
        {
            try
            {
                long Now = Global.ConvertToTimestamp(DateTimeOffset.Now.UtcDateTime);

                if (message == null)
                {
                    return;
                }

                MySqlCommand cmd = new MySqlCommand($"INSERT INTO SnipeLogs(message, MessageTimestamp, guildId, author) VALUES('{content}', {Now}, {socketGuildChannel.Guild.Id}, {message.Author.Id})", conn);
                cmd.ExecuteNonQuery();
            }

            catch (Exception ex)
            {
                await message.Channel.SendMessageAsync(ex.Message);
                Global.ConsoleLog(ex.Message);
            }
        }

        /// <summary>
        /// Gets the level-up notification channel.
        /// </summary>
        /// <param name="guild">The guild to get the channel for.</param>
        /// <returns>A string containing the channel id.</returns>
        public async Task<string> GetLevellingChannel(SocketGuild guild)
        {
            try
            {
                IMongoDatabase database = MongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("levellingchannel").ToString();

                if (itemVal != null)
                {
                    return itemVal;
                }

                else
                {
                    return "0";
                }
            }

            catch
            {
                return "0";
            }
        }

        /// <summary>
        /// Handles levelling and XP for users when they send a message.
        /// </summary>
        /// <param name="arg">The message which was sent.</param>
        public async Task OnMessageReceived(SocketMessage arg)
        {
            if (arg.Author.IsBot || arg.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }

            try
            {
                SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
                string toLevel = await Global.DetermineLevel(chan.Guild);

                if (toLevel.ToLower() == "false")
                {
                    return;
                }

                else if(toLevel.ToLower() == "off")
                {
                    return;
                }

                else
                {
                    ulong Levelchannel = Convert.ToUInt64(GetLevellingChannel(chan.Guild).Result);
                    long Now = Global.ConvertToTimestamp(arg.Timestamp.UtcDateTime);
                    MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);
                    MySqlConnection queryConn = new MySqlConnection(Global.MySQL.ConnStr);
                    conn.Open();
                    long TimeStamp = 0;
                    long XP = 0;
                    long level = 0;
                    bool ran = false;
                    long xpToNextLevel = 0;
                    long totalXP = 0;
                    MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM Levels WHERE userId = {arg.Author.Id} AND guildId = {chan.Guild.Id}", conn);
                    MySqlDataReader reader = cmd1.ExecuteReader();

                    try
                    {
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
                                totalXP += XP;
                                xpToNextLevel = (long)(5 * Math.Pow(level, 2) + 50 * level + 100);

                                if (XP >= xpToNextLevel)
                                {
                                    level += 1;
                                    XP -= xpToNextLevel;
                                    SocketTextChannel Channel = (SocketTextChannel)chan.Guild.GetChannel(Levelchannel);

                                    if (Channel != null)
                                    {
                                        await Channel.SendMessageAsync($"Congratulations, {arg.Author.Mention} for reaching level {level}!");
                                    }

                                    else { }
                                }

                                queryConn.Open();
                                await AddToDatabase(0, queryConn, arg, level, XP, totalXP);
                                queryConn.Close();
                            }

                            else
                            {
                                return;
                            }
                        }
                    }

                    catch(Exception ex)
                    {
                        Global.ConsoleLog(ex.Message);
                    }

                    finally
                    {
                        conn.Close();
                    }

                    if (!ran)
                    {
                        try
                        {
                            Random r = new Random();
                            totalXP = +r.Next(15, 25);
                            queryConn.Open();
                            await AddToDatabase(1, queryConn, arg, 0, XP, totalXP);
                            queryConn.Close();
                        }

                        finally
                        {
                            queryConn.Close();
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                //if (ex.Message.GetType() != typeof(NullReferenceException))
                //{
                //    EmbedBuilder eb = new EmbedBuilder();
                //    eb.WithAuthor(arg.Author);
                //    eb.WithTitle("Error sending deatils to database:");
                //    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                //    eb.WithCurrentTimestamp();
                //    eb.WithColor(Color.Red);
                //    eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
                //    await arg.Channel.SendMessageAsync("", false, eb.Build());
                //    return;
                //}
                Global.ConsoleLog(ex.Message);
            }
        }

        /// <summary>
        /// Handles building the snipe embed for a deleted message & logs it to the console.
        /// </summary>
        /// <param name="msg">The cached message that was deleted.</param>
        /// <param name="arg2">The cached channel where the message was sent.</param>
        private async Task OnMessageDelete(Cacheable<IMessage, ulong> msg, Cacheable<IMessageChannel, ulong> arg2)
        {
            try
            {
                SocketUserMessage author = (SocketUserMessage)await msg.GetOrDownloadAsync();
                SocketGuildChannel sGC = (SocketGuildChannel)_discord.GetChannel(arg2.Id);
                string messagecontent = msg.HasValue ? msg.Value.Content : "Unable to retrieve message";

                if (msg.HasValue)
                {
                    if (author.Embeds.Count > 0)
                    {
                        string fields = "";
                        List<string> content = new List<string>();
                        IEmbed message = author.Embeds.First();
                        EmbedBuilder embed = message.ToEmbedBuilder();

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
                        messagecontent = $"{content[2]}\n{content[0]}\n\n{content[1]}\n{fields}\n{content[3]}";
                    }
                }

                MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

                try
                {
                    long Now = Global.ConvertToTimestamp(DateTimeOffset.Now.UtcDateTime);

                    if (messagecontent.Contains("'"))
                    {
                        messagecontent = Regex.Replace(messagecontent, "'", "\"");
                    }

                    conn.Open();
                    await AddToSnipe(conn, messagecontent, author, sGC);
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
                        eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
                        await author.Channel.SendMessageAsync("", false, eb.Build());
                        return;
                    }

                    Global.ConsoleLog(ex.Message);
                }

                finally
                {
                    conn.Close();
                }

                if (author == null)
                {
                    return;
                }

                string logMessage = $"[DELETED]User: [{author.Author.Username}]<->[{author.Author.Id}] Discord Server: [{sGC.Guild.Name}/{arg2}] -> [{messagecontent}]";
                _logger.LogDebug(logMessage);
                return;
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        /// <summary>
        /// Logs the message and some basic information about it to the console.
        /// </summary>
        /// <param name="arg">The message to log.</param>
        private Task OnLogMessage(SocketMessage arg)
        {
            if (arg.Channel.GetType() == typeof(SocketTextChannel))
            {
                SocketGuildChannel gC = (SocketGuildChannel)arg.Channel;
                string logMessage = $"User: [{arg.Author.Username}]<->[{arg.Author.Id}] Discord Server: [{gC.Guild.Name}/{arg.Channel}] -> [{arg.Content}]";
                _logger.LogDebug(logMessage);
            }

            else if (arg.Channel.GetType() == typeof(SocketDMChannel))
            {
                string logMessage = $"User: [{arg.Author.Username}]<->[{arg.Author.Id}] DM channel: [{arg.Channel}] -> [{arg.Content}]";
                _logger.LogDebug(logMessage);
            }

            else
            {
                string logMessage = $"User: [{arg.Author.Username}]<->[{arg.Author.Id}] Channel type {arg.Channel.GetType()}: [{arg.Channel}] -> [{arg.Content}]";
                _logger.LogDebug(logMessage);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Writes to the console when a shard becomes available.
        /// </summary>
        /// <param name="arg">The shard that opened.</param>
        private async Task<Task> OnShardReady(DiscordSocketClient arg)
        {
            _logger.LogInformation($"Connected as -> {arg.CurrentUser.Username}");
            _logger.LogInformation($"We are on {arg.Guilds.Count} servers");
            _logger.LogInformation($"Shard {arg.ShardId} ready! - {_discord.Shards.Count()} / {await _discord.GetRecommendedShardCountAsync()} recommended shards.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles the logging severity of the message.
        /// </summary>
        /// <param name="msg">The message to log.</param>
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
