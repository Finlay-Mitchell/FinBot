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

            modCommands = new ModCommands(service);
            _client.MessageReceived += CheckForCensoredWords;
            _client.MessageReceived += CheckForPingSpam;
            _client.MessageReceived += CheckForLinks;
        }

        public async Task CheckForCensoredWords(SocketMessage msg)
        {
            try
            {
                if (msg.Author.IsBot)
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
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
                    string message = msg.Content;

                    foreach (KeyValuePair<string, string> x in Global.leetRules)
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
                            Description = $"{msg.Author} has been warned at {DateTime.Now}\n Reason: Bad word usage.",
                            ThumbnailUrl = Global.KickMessageURL,
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
                        eb.AddField("Moderator", $"LexiBot automod.", true);
                        eb.AddField("Reason", $"\"Bad word usage.\"", true);
                        //eb.AddField("Message with filter", message.Replace("\n", ""), true);
                        eb.AddField("Message", msg.ToString(), true);
                        eb.WithCurrentTimestamp();
                        await logchannel.SendMessageAsync("", false, eb.Build());
                    }

                }

                else
                {
                    //await msg.Channel.SendMessageAsync(itemVal.ToString());
                }
            }

            catch (Exception ex)
            {
                //await msg.Channel.SendMessageAsync($"broke: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        private async Task CheckForLinks(SocketMessage arg)
        {
            SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
            SocketGuildUser user = (SocketGuildUser)arg.Author;

            if (user.GuildPermissions.ManageMessages)
            {
                return;
            }

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

            Regex r = new Regex(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

            if (r.IsMatch(arg.ToString()))
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
                    Description = $"{arg.Author} has been warned at {DateTime.Now}\n Reason: Sent a link.",
                    ThumbnailUrl = Global.KickMessageURL,
                    Color = Color.Orange
                };
                eb.WithCurrentTimestamp();
                await arg.Channel.TriggerTypingAsync();
                await arg.Channel.SendMessageAsync("", false, eb.Build());
                string modlogchannel = await Global.GetModLogChannel(chan.Guild);

                if(modlogchannel == "0")
                {
                    return;
                }

                SocketTextChannel logchannel = chan.Guild.GetTextChannel(Convert.ToUInt64(modlogchannel));
                eb.AddField("User", $"{user.Username}", true);
                eb.AddField("Moderator", $"LexiBot automod.", true);
                eb.AddField("Reason", $"\"Sent a link.\"", true);
                eb.AddField("Message", arg.ToString(), true);
                eb.WithCurrentTimestamp();
                await logchannel.SendMessageAsync("", false, eb.Build());


                return;
            }

            return;
        }

        private async Task CheckForPingSpam(SocketMessage arg)
        {
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
                    Description = $"{arg.Author} has been warned at {DateTime.Now}\n Reason: mass ping.",
                    ThumbnailUrl = Global.KickMessageURL,
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
                eb.AddField("Moderator", $"LexiBot automod.", true);
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