using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using FinBot.Modules;
using System.Text.RegularExpressions;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace FinBot.Handlers.AutoMod
{
    public class ChatFilter : ModuleBase<ShardedCommandContext>
    {
        DiscordShardedClient _client;
        public static ModCommands modCommands;

        public ChatFilter(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();
            modCommands = new ModCommands(service); // New instance of the ModCommands class.

            _client.MessageReceived += CheckForCensoredWords;
            _client.MessageReceived += CheckForPingSpam;
            _client.MessageReceived += CheckForLinks;
        }

        /// <summary>
        /// Checks for censored phrases in the parsed message.
        /// </summary>
        /// <param name="msg">The message the user sent.</param>
        public async Task CheckForCensoredWords(SocketMessage msg)
        {
            try
            {
                if (msg.Author.IsBot || msg.Channel.GetType() == typeof(SocketDMChannel) || Global.DevUIDs.Contains(msg.Author.Id))
                {
                    return;
                }

                SocketGuildUser user = (SocketGuildUser)msg.Author;

                if (user.GuildPermissions.ManageMessages)
                {
                    return;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                SocketGuildChannel chan = msg.Channel as SocketGuildChannel;
                ulong _id = chan.Guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("blacklistedterms").ToJson();

                if (itemVal != null)
                {
                    List<string> stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace); //Generates the regular expression to search the message for guild blacklisted terms.
                    string message = msg.Content;

                    foreach (KeyValuePair<string, string> x in Global.leetRules) //Assigns leet rules to the message to avoid people avoiding word filtration.
                    {
                        message = message.Replace(x.Key, x.Value);
                    }

                    message = message.ToLower();

                    if (re.IsMatch(message))
                    {
                        await msg.DeleteAsync();
                        modCommands.AddModlogs(msg.Author.Id, ModCommands.Action.Warned, _client.CurrentUser.Id, "Bad word usage", chan.Guild.Id);
                        EmbedBuilder eb = new EmbedBuilder()
                        {
                            Title = $"***{msg.Author.Username} has been warned***",
                            Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = msg.Author.GetAvatarUrl(),
                                Text = $"{msg.Author.Username}#{msg.Author.Discriminator}"
                            },
                            Description = $"{msg.Author} has been warned at {DateTime.Now}\nReason: Bad word usage.",
                            Color = Color.Orange
                        };
                        eb.WithCurrentTimestamp();
                        await msg.Channel.TriggerTypingAsync();
                        await msg.Channel.SendMessageAsync("", false, eb.Build());
                        string modlogchannel = await Global.GetModLogChannel(chan.Guild);

                        if (modlogchannel == "0")
                        {
                            return;
                        }

                        SocketTextChannel logchannel = chan.Guild.GetTextChannel(Convert.ToUInt64(modlogchannel));
                        eb.AddField("User", $"{user.Username}", true);
                        eb.AddField("Moderator", $"FinBot automod.", true);
                        eb.AddField("Reason", $"\"Bad word usage.\"", true);
                        //eb.AddField("Message with filter", message.Replace("\n", ""), true); //See how the message was caught - fix in future.
                        eb.AddField("Message", msg.ToString(), true);
                        eb.WithCurrentTimestamp();
                        await logchannel.SendMessageAsync("", false, eb.Build());
                    }
                }

                else
                {
                    return;
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog("Chat Filter[blacklisted terms] - " + ex.Message);
            }
        }

        /// <summary>
        /// Checks for URI's in sent message.
        /// </summary>
        /// <param name="arg">User sent message.</param>
        /// <returns></returns>
        private async Task CheckForLinks(SocketMessage arg)
        {
            try
            {
                if (arg.Author.IsBot || arg.Channel.GetType() == typeof(SocketDMChannel) || Global.DevUIDs.Contains(arg.Author.Id))
                {
                    return;
                }

                SocketGuildUser user = (SocketGuildUser)arg.Author;

                if (user.GuildPermissions.ManageMessages)
                {
                    return;
                }

                SocketGuildChannel chan = arg.Channel as SocketGuildChannel;

                try
                {
                    MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                    IMongoDatabase database = mongoClient.GetDatabase("finlay");
                    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                    ulong _id = chan.Guild.Id;
                    BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                    string itemVal = item?.GetValue("disablelinks").ToJson();

                    if (itemVal == null || itemVal == "false")
                    {
                        return;
                    }
                }

                catch { return; }

                if (Global.URIAndIpRegex.IsMatch(arg.ToString()))
                {
                    await arg.DeleteAsync();
                    modCommands.AddModlogs(arg.Author.Id, ModCommands.Action.Warned, _client.CurrentUser.Id, "Sent a link", chan.Guild.Id);
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Title = $"***{arg.Author.Username} has been warned***",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = arg.Author.GetAvatarUrl(),
                            Text = $"{arg.Author.Username}#{arg.Author.Discriminator}"
                        },
                        Description = $"{arg.Author} has been warned at {DateTime.Now}\nReason: Sent a link.",
                        Color = Color.Orange
                    };
                    eb.WithCurrentTimestamp();
                    await arg.Channel.TriggerTypingAsync();
                    await arg.Channel.SendMessageAsync("", false, eb.Build());
                    string modlogchannel = await Global.GetModLogChannel(chan.Guild);

                    if (modlogchannel == "0")
                    {
                        return;
                    }

                    SocketTextChannel logchannel = chan.Guild.GetTextChannel(Convert.ToUInt64(modlogchannel));
                    eb.AddField("User", $"{user.Username}", true);
                    eb.AddField("Moderator", $"FinBot automod.", true);
                    eb.AddField("Reason", $"\"Sent a link.\"", true);
                    eb.AddField("Message", arg.ToString(), true);
                    eb.WithCurrentTimestamp();
                    await logchannel.SendMessageAsync("", false, eb.Build());

                    return;
                }

                return;
            }

            catch(Exception ex)
            {
                Global.ConsoleLog("Chat Filter[links] - " + ex.Message);
            }
        }

        /// <summary>
        /// Checks whether the message contains mass ping, roles & members.
        /// </summary>
        /// <param name="arg">The message.</param>
        private async Task CheckForPingSpam(SocketMessage arg)
        {
            if (arg.Author.IsBot || arg.Channel.GetType() == typeof(SocketDMChannel) || Global.DevUIDs.Contains(arg.Author.Id))
            {
                return;
            }

            SocketGuildUser user = (SocketGuildUser)arg.Author;

            if (!user.GuildPermissions.ManageMessages && arg.MentionedUsers.Count >= Global.MaxUserPingCount || arg.MentionedRoles.Count >= Global.MaxRolePingCount)
            {
                await arg.DeleteAsync();
                SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
                modCommands.AddModlogs(arg.Author.Id, ModCommands.Action.Warned, _client.CurrentUser.Id, "mass ping", chan.Guild.Id);
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Title = $"***{arg.Author.Username} has been warned***",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = arg.Author.GetAvatarUrl(),
                        Text = $"{arg.Author.Username}#{arg.Author.Discriminator}"
                    },
                    Description = $"{arg.Author} has been warned at {DateTime.Now}\nReason: mass ping.",
                    Color = Color.Orange
                };
                eb.WithCurrentTimestamp();
                await arg.Channel.TriggerTypingAsync();
                await arg.Channel.SendMessageAsync("", false, eb.Build());
                string modlogchannel = await Global.GetModLogChannel(chan.Guild);

                if (modlogchannel == "0")
                {
                    return;
                }

                SocketTextChannel logchannel = chan.Guild.GetTextChannel(Convert.ToUInt64(modlogchannel));
                eb.AddField("User", $"{user.Username}", true);
                eb.AddField("Moderator", $"FinBot automod.", true);
                eb.AddField("Reason", $"\"Mass ping.\"", true);
                eb.AddField("Message", arg.ToString(), true);
                eb.WithCurrentTimestamp();
                await logchannel.SendMessageAsync("", false, eb.Build());

                return;
            }

            return;
        }
    }
}