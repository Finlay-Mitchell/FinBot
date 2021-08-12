using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;
using FinBot.Handlers;
using Color = Discord.Color;
using Discord.Rest;
using WikiDotNet;
using System.Text;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Web;
using QuickChart;
using ICanHazDadJoke.NET;
using System.Data;
using MySql.Data.MySqlClient;
using MongoDB.Driver;
using UptimeSharp;
using UptimeSharp.Models;
using SearchResult = Google.Apis.YouTube.v3.Data.SearchResult;
using FinBot.Services;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.Drawing;
using QRCoder;

namespace FinBot.Modules
{
    public class Commands : ModuleBase<ShardedCommandContext>
    {
        [Command("reddit"), Summary("Shows a post from the selected subreddit"), Remarks("(PREFIX)reddit <subreddit>"), Alias("r", "subreddit")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Reddit([Remainder] string subreddit)
        {
            subreddit = subreddit.Replace(" ", "");
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync($"https://www.reddit.com/r/{subreddit}.json");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            RedditHandler data = JsonConvert.DeserializeObject<RedditHandler>(resp);
            Regex r = new Regex(@"https:\/\/i.redd.it\/(.*?)\.");
            IEnumerable<Child> childs = data.Data.Children.Where(x => r.IsMatch(x.Data.Url.ToString())); //For some reason, this can still sometimes throw an exception. Not overly concerned since it doesn't cause any issues.
            SocketTextChannel Chan = Context.Message.Channel as SocketTextChannel;
            IDisposable tp = Context.Channel.EnterTypingState();

            if (!childs.Any())
            {
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Title = "Subreddit not found!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but the [subreddit](https://www.reddit.com/r/{subreddit}) you tried to post from was not found or no images/gifs could be retrieved, please try again.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }
                .WithColor(221, 65, 36)
                .WithCurrentTimestamp()
                .Build());
                tp.Dispose();

                return;
            }

            Random rand = new Random();
            int count = childs.Count();
            Child post = childs.ToArray()[rand.Next() % childs.Count()];

            if (!(Chan.IsNsfw) && post.Data.over_18)
            {
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Title = "NSFW post!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but the post you tried to send has been flagged as NSFW. Please try this in a NSFW channel.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }
                .WithColor(221, 65, 36)
                .WithCurrentTimestamp()
                .Build());
                tp.Dispose();

                return;
            }

            EmbedBuilder b = new EmbedBuilder()
            {
                Color = new Color(0xFF4301),
                Title = subreddit,
                Description = $"{post.Data.title}\n",
                ImageUrl = post.Data.Url.ToString(),
                Footer = new EmbedFooterBuilder()
                {
                    Text = "u/" + post.Data.author
                }
            };
            b.AddField("Post info", $"Score: {post.Data.score}\nTotal awards received: {post.Data.total_awards_received}\nurl: [Visit post here](https://reddit.com/{post.Data.permalink})\nCreated at: {Global.UnixTimeStampToDateTime(post.Data.created)}");
            b.WithCurrentTimestamp();
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.ReplyAsync("", false, b.Build());
            tp.Dispose();
        }

        [Command("guess"), Summary("The bot will guess the contents of an image"), Remarks("(PREFIX)guess <image>)")]
        public async Task Guess(params string[] arg)
        {
            IDisposable tp = Context.Channel.EnterTypingState();

            if (arg.Length == 1)
            {
                if (Uri.TryCreate(arg.First(), UriKind.RelativeOrAbsolute, out _))
                {
                    HttpClient HTTPClient = new HttpClient();
                    string url = "https://www.google.com/searchbyimage?image_url=" + arg.First();
                    HTTPClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
                    HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync(url);
                    string resp = await HTTPResponse.Content.ReadAsStringAsync();
                    Regex r = new Regex("value=\"(.*?)\" aria-label=\"Search\""); //This is sometimes subject to change, so if all images aren't recognized, look here.

                    if (r.IsMatch(resp))
                    {
                        Match mtch = r.Match(resp);
                        string val = mtch.Groups[1].Value;
                        RestUserMessage msg = (RestUserMessage)await Context.Message.ReplyAsync($"Is that {val}?");
                        await msg.AddReactionsAsync(Global.reactions.ToArray());
                        tp.Dispose();
                    }

                    else
                    {
                        await Context.Message.ReplyAsync(@"Unable to guess.");
                        tp.Dispose();
                    }
                }

                else
                {
                    await Context.Message.ReplyAsync("invalid URL.");
                    tp.Dispose();
                }
            }

            else if (Context.Message.Attachments.Count == 1)
            {
                HttpClient HTTPClient = new HttpClient();
                string url = "https://www.google.com/searchbyimage?image_url=" + Context.Message.Attachments.First().ProxyUrl;
                HTTPClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
                HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync(url);
                string resp = await HTTPResponse.Content.ReadAsStringAsync();
                Regex r = new Regex("value=\"(.*?)\" aria-label=\"Search\""); //This is sometimes subject to change, so if all images aren't recognized, look here.

                if (r.IsMatch(resp))
                {
                    Match mtch = r.Match(resp);
                    string val = mtch.Groups[1].Value;
                    RestUserMessage msg = (RestUserMessage)await Context.Message.ReplyAsync($"Is that {val}?");
                    await msg.AddReactionsAsync(Global.reactions.ToArray());
                    tp.Dispose();
                }

                else
                {
                    await Context.Message.ReplyAsync(@"Unable to guess.");
                    tp.Dispose();
                }
            }

            else
            {
                if (arg.Length <= 1)
                {
                    await Context.Message.ReplyAsync("There's nothing to guess. Please provide an image");
                    tp.Dispose();
                }

                else
                {
                    await Context.Message.ReplyAsync("Please only provide one parameter.");
                    tp.Dispose();
                }
            }
        }

        [Command("ping"), Summary("gives you a ping to the Discord API"), Remarks("(PREFIX)ping")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Ping()
        {
            DateTime before = DateTime.Now;
            await Context.Channel.TriggerTypingAsync();
            RestUserMessage message = (RestUserMessage)await Context.Message.ReplyAsync("Pong!");
            DateTime after = DateTime.Now;
            ulong snowflake = (ulong)Math.Round((after - before).TotalSeconds * 1000);
            ulong Heartbeat = (ulong)Math.Round((double)Context.Client.Latency);
            ulong totalLatency = (ulong)Math.Round((message.CreatedAt - Context.Message.CreatedAt).TotalSeconds * 1000);
            EmbedBuilder eb = new EmbedBuilder
            {
                Title = $"Pong!"
            };
            eb.AddField("Ping to discord", $"{Math.Floor((double)snowflake / 2)}ms");
            eb.AddField("Heartbeat(Me -> Discord -> Me)", $"{Heartbeat}ms");
            eb.AddField("Total time(Your message -> my reply)", $"{totalLatency}ms");
            eb.WithCurrentTimestamp();
            eb.WithAuthor(Context.Message.Author);

            if (totalLatency <= 400)
            {
                eb.Color = Color.Green;
            }

            else if (totalLatency <= 550)
            {
                eb.Color = Color.Orange;
            }

            else
            {
                eb.Color = Color.Red;
            }

            await message.ModifyAsync(x =>
            {
                x.Content = "";
                x.Embed = eb.Build();
            });
        }

        [Command("say"), Summary("repeats the text you enter to it"), Remarks("(PREFIX)say <text>"), Alias("echo", "repeat", "reply", "shout")]
        public async Task Echo([Remainder] string echo)
        {
            await Context.Channel.TriggerTypingAsync();

            if (Context.Message.MentionedUsers.Any() || Context.Message.MentionedRoles.Any() || Context.Message.MentionedEveryone)
            {
                await Context.Message.Channel.SendMessageAsync("Sorry, but you can't mention people");
                return;
            }

            if (echo.Length != 0)
            {
                await Context.Channel.SendMessageAsync(await SayTextAsync(echo, Context));
                return;
            }

            else
            {
                await Context.Message.ReplyAsync($"What do you want me to say? please do {await Global.DeterminePrefix(Context)}say <msg>.");
                return;
            }
        }

        /// <summary>
        /// Checks and modifies the message parsed to become acceptable to be sent to that guild.
        /// </summary>
        /// <param name="text">The text to check and modify.</param>
        /// <param name="context">The context for the message.</param>
        /// <returns>A string for the suitable message.</returns>
        public async Task<string> SayTextAsync(string text, SocketCommandContext context)
        {
            string final = text.ToLower();
            final = Regex.Replace(final, $"{Global.DeterminePrefix(context).Result}|{Global.URIAndIpRegex}|{Global.clientPrefix}|@", "");

            if (string.IsNullOrEmpty(final) || string.IsNullOrWhiteSpace(final))
            {
                final = "Whoopsie daisy, my filter has filtered your text and it's returned an empty string, try again, with more sufficient text.";
            }

            try
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = context.Guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("blacklistedterms").ToJson();

                if (itemVal != null)
                {
                    List<string> stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                    string message = final;

                    foreach (KeyValuePair<string, string> x in Global.leetRules)
                    {
                        message = message.Replace(x.Key, x.Value);
                    }

                    if (re.IsMatch(message))
                    {
                        final = "Whoopsie daisy, my filter has filtered your text and it's returned an empty string, try again, with more sufficient text.";
                    }
                }

                return final;
            }

            catch { return final; }
        }

        [Command("userinfo"), Summary("shows information on a user"), Remarks("(PREFIX)userinfo || (PREFIX)userinfo <user>."), Alias("whois", "user", "info", "user-info")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task UserInfo([Remainder] SocketUser user = null)
        {
            try
            {
                if (user == null)
                {
                    user = Context.Message.Author;
                }

                SocketGuildUser SGU = (SocketGuildUser)user; //Convert to SocketGuildUser to get more user options, such as their nickname(if one is set).
                string nickState = "";
                string ClientError = "None(offline)";

                if (SGU.Nickname != null)
                {
                    nickState = SGU.Nickname;
                }

                if (user.IsBot && user.Status.ToString().ToLower() != "offline")
                {
                    ClientError = "Server hosted";
                }

                await Context.Channel.TriggerTypingAsync();
                EmbedBuilder eb = new EmbedBuilder()
                {
                    ImageUrl = user.GetAvatarUrl() ?? user.GetAvatarUrl()
                };
                eb.AddField("Username", user);
                _ = nickState == "" ? null : eb.AddField("Nickname", nickState);
                eb.AddField("ID:", $"{user.Id}");
                eb.AddField("Status", user.Status);
                eb.AddField("Active clients", string.IsNullOrEmpty(string.Join(separator: ", ", values: user.ActiveClients.ToList().Select(r => r.ToString()))) || user.IsBot ? ClientError : string.Join(separator: ", ", values: user.ActiveClients.ToList().Select(r => r.ToString())));
                _ = eb.AddField("Created at UTC", user.CreatedAt.UtcDateTime.ToString("r"));
                eb.AddField("Joined at UTC?", SGU.JoinedAt.HasValue ? SGU.JoinedAt.Value.UtcDateTime.ToString("r") : "No value :/");
                _ = eb.AddField($"Roles: [{SGU.Roles.Count}]", $"<@&{string.Join(separator: ">, <@&", values: SGU.Roles.Select(r => r.Id))}>");
                _ = eb.AddField($"Permissions: [{SGU.GuildPermissions.ToList().Count}]", $"{string.Join(separator: ", ", values: SGU.GuildPermissions.ToList().Select(r => r.ToString()))}");
                eb.WithAuthor(Context.Message.Author);
                eb.WithColor(Color.DarkPurple);
                eb.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, eb.Build());
            }

            catch
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor(Context.Message.Author);
                eb.WithColor(Color.Orange);
                eb.WithTitle($"User not found");
                eb.WithDescription($"Sorry, but I couldn't find that user.");
                eb.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, eb.Build());
            }
        }

        [Command("serverinfo"), Summary("Shows information on the server"), Remarks("(PREFIX)serverinfo"), Alias("server", "server-info")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task ServerInfo()
        {
            await Context.Channel.TriggerTypingAsync();
            string boosttier = "";

            boosttier = (Context.Guild.PremiumTier.ToString()) switch
            {
                "Tier1" => "Tier 1",
                "Tier2" => "Tier 2",
                "Tier3" => "Tier 3",
                _ => "None",
            };

            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = "Serverinfo",
                Description = $"Some information on the server",
                ImageUrl = Context.Guild.IconUrl,
            };
            eb.AddField("server owner", Context.Guild.Owner, true);
            eb.AddField("Server name", Context.Guild, true);
            eb.AddField("Member count", Context.Guild.MemberCount, true);
            eb.AddField("Created at", Context.Guild.CreatedAt, true);
            eb.AddField("server location", Context.Guild.VoiceRegionId, true);
            eb.AddField("boosters", Context.Guild.PremiumSubscriptionCount, true);
            eb.AddField("Boost level", boosttier, true);
            eb.AddField("Number of roles", Context.Guild.Roles.Count, true);
            eb.AddField("Number of channels", $"Text channels: {Context.Guild.TextChannels.Count}\nVoice channels: {Context.Guild.VoiceChannels.Count}\nCategories: {Context.Guild.CategoryChannels.Count}", true);
            _ = eb.AddField($"VIP perks [{Context.Guild.Features.Count}]", string.IsNullOrEmpty(string.Join(separator: ", ", values: Context.Guild.Features.ToList().Select(r => r.ToString())).ToLower()) ? "None" : 
                string.Join(separator: ", ", values: Context.Guild.Features.ToList().Select(r => r.ToString())).ToLower().Replace("_", " "), true);
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.Blue);
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("botInfo"), Summary("shows some basic bot information"), Remarks("(PREFIX)botinfo")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task BotInfo()
        {
            await Context.Channel.TriggerTypingAsync();
            EmbedBuilder eb = new EmbedBuilder();
            eb.AddField("Developers:", "Finlay Mitchell, Thomas Waffles");
            eb.AddField("Version: ", Global.Version);
            eb.AddField("Languages", "C# - Discord.net API\nPython - Discord.py\nJavascript - Discord.js");
            eb.WithAuthor(Context.Message.Author);
            eb.WithColor(Color.Gold);
            eb.WithTitle("Bot info");
            eb.AddField("Uptime", $"{DateTime.Now - Process.GetCurrentProcess().StartTime:dd\\.hh\\:mm\\:ss}");
            eb.AddField("How many servers am I in?", Context.Client.Guilds.Count());
            eb.AddField("Invite to your server", "[Invite link](http://bot.finlaymitchell.ml)");
            eb.AddField("Join the support server", "[here](http://server.finlaymitchell.ml)");
            eb.AddField("My website", "[Visit the webpage here](http://finbot.finlaymitchell.ml)");
            eb.WithDescription($"Here's some info on me");
            eb.WithCurrentTimestamp();
            eb.WithDescription("To support the developers, [please feel free to donate](http://donate.finlaymitchell.ml)!");
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("8ball"), Summary("with 8ball magic, ask it a question and it will respond to you"), Remarks("(PREFIX)8ball <question>"), Alias("eightball", "ask")]
        public async Task EightBall([Remainder] string input)
        {
            IDisposable tp = Context.Channel.EnterTypingState();

            if (string.IsNullOrEmpty(input))
            {
                await Context.Message.ReplyAsync("Please enter a parameter");
                tp.Dispose();
            }

            else
            {
                string[] answers = { "As I see it, yes.", "Ask again later.", "It is certain.", "It is decidedly so.", "Don't count on it.", "Better not tell you now.", "Concentrate and ask again.", 
                    "Cannot predict now.", "Most likely.", "My reply is no.", "Yes.", "You may rely on it.", "Yes - definitely.", "Very doubtful.", "Without a doubt.", " My sources say no.", 
                    "Outlook not so good.", "Outlook good.", "Reply hazy, try again.", "Signs point to yes."};
                Random rand = new Random();
                int index = rand.Next(answers.Length);
                await Context.Message.ReplyAsync(answers[index]);
                tp.Dispose();
            }
        }

        [Command("topic"), Summary("sends a random conversation starter"), Remarks("(PREFIX)topic")]
        public async Task Topic(params string[] args)
        {
            await Context.Channel.TriggerTypingAsync();
            string[] topic = File.ReadAllLines(Global.TopicsPath);
            Random rand = new Random();
            int index = rand.Next(topic.Length);
            await Context.Message.ReplyAsync(topic[index]);
        }

        [Command("roll"), Summary("rolls a random number between 0 and your number"), Remarks("(PREFIX)roll <number>")]
        public async Task Roll(int num)
        {
            await Context.Channel.TriggerTypingAsync();
            Random r = new Random();
            int ans = r.Next(0, num);
            await Context.Message.ReplyAsync($"Number: {ans}");
        }

        [Command("av"), Summary("show users avatar"), Remarks("(PREFIX)av <user>"), Alias("Avatar", "profile")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task AV([Remainder] SocketUser user = null)
        {
            try
            {
                if (user == null)
                {
                    user = Context.Message.Author;
                }

                await Context.Channel.TriggerTypingAsync();
                EmbedBuilder eb = new EmbedBuilder()
                {
                    ImageUrl = user.GetAvatarUrl(size: 1024) ?? user.GetDefaultAvatarUrl()
                };
                eb.WithAuthor(Context.Message.Author)
                .WithColor(Color.DarkTeal)
                .WithDescription($"Heres the profile picture for {user}")
                .WithFooter($"{Context.Message.Author}")
                .WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, eb.Build());
            }

            catch
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor(Context.Message.Author);
                eb.WithColor(Color.Orange);
                eb.WithTitle($"User not found");
                eb.WithDescription($"Sorry, but I couldn't find that user.");
                eb.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, eb.Build());
            }
        }

        [Command("remind", RunMode = RunMode.Async), Summary("Reminds you with a custom message (In Seconds)"), Remarks("(PREFIX)remind <seconds> <message>"), Alias("Timer")]
        public async Task Remind(string duration, [Remainder] string remindMsg = "No content set")
        {
            if (remindMsg.Contains("@everyone") || remindMsg.Contains("@here") || Context.Message.MentionedUsers.Any() || Context.Message.MentionedRoles.Any())
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync($"Sorry but can't mention users");
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await ReminderService.SetReminder(Context.Guild, Context.User, (SocketTextChannel)Context.Channel, DateTime.Now, duration, remindMsg, Context);
            }
        }

        [Command("embed"), Summary("Displays your message in an embed message"), Remarks("(PREFIX)embed <title>, <description>"), Alias("embedmessage")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task CmdEmbedMessage([Remainder] string text = "")
        {
            string content = await CheckEmbedContent(text, Context);
            await Context.Channel.TriggerTypingAsync();

            if (Context.Message.MentionedUsers.Any() || Context.Message.MentionedRoles.Any() || Context.Message.MentionedEveryone)
            {
                await Context.Message.Channel.SendMessageAsync("Sorry, but you can't mention people");
                return;
            }

            if (content.Contains(","))
            {
                string[] result = content.Split(',');
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage(result[0], result[1]).Build());
            }

            else
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage(content).Build());
            }
        }

        /// <summary>
        /// Checks the embed content and makes it suitable for the guild.
        /// </summary>
        /// <param name="text">The text to parse in.</param>
        /// <param name="context">The context for the command.</param>
        /// <returns>A string of the valid text to send.</returns>
        public async Task<string> CheckEmbedContent(string text, SocketCommandContext context)
        {
            string final = text.ToLower();
            final = Regex.Replace(final, $"{Global.DeterminePrefix(context).Result}|{Global.URIAndIpRegex}|{Global.clientPrefix}", "");

            if (string.IsNullOrEmpty(final) || string.IsNullOrWhiteSpace(final))
            {
                final = "Invalid.";
            }

            try
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = context.Guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("blacklistedterms").ToJson();

                if (itemVal != null)
                {
                    List<string> stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                    string message = final;

                    foreach (KeyValuePair<string, string> x in Global.leetRules)
                    {
                        message = message.Replace(x.Key, x.Value);
                    }

                    if (re.IsMatch(message))
                    {
                        final = "Invalid.";
                    }
                }

                return final;
            }

            catch { return final; }
        }

        [Command("translate"), Summary("Translates inputted text to English"), Remarks("(PREFIX)translate <text>"), Alias("t", "trans")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Translate([Remainder] string translate)
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            translate = Regex.Replace(translate, @"^\s*""?|""?\s*$", "");
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={HttpUtility.UrlEncode(translate)}";
            WebClient webClient = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            string result = webClient.DownloadString(url);

            try
            {
                result = result[4..result.IndexOf("\"", 4, StringComparison.Ordinal)];

                if (result == translate)
                {
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "Error in translation",
                        Description = $"There was an error translating {translate}.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());
                    tp.Dispose();

                    return;
                }

                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.Blue,
                    Description = $"{result}",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
                tp.Dispose();
            }

            catch
            {
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "Error in translation",
                    Description = $"There was an error translating {translate}.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
                tp.Dispose();
            }
        }

        [Command("wiki", RunMode = RunMode.Async), Alias("wikipedia"), Summary("Searches Wikipedia"), Remarks("(PREFIX)wiki <phrase>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Wikipedia([Remainder] string search = "")
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("A search needs to be entered! E.G: `wiki testing`");
                return;
            }

            await WikiSearch(search, Context.Channel, Context.Message);
        }

        /// <summary>
        /// Searches Wikipedia and gathers results.
        /// </summary>
        /// <param name="search">The term to search Wikipedia for.</param>
        /// <param name="channel">The text channel where the command was called from.</param>
        /// <param name="msg">The message which executed the command.</param>
        /// <param name="maxSearch">The maximum number of search results.</param>
        /// <returns></returns>
        private async Task WikiSearch(string search, ISocketMessageChannel channel, SocketUserMessage msg, int maxSearch = 10)
        {
            await channel.TriggerTypingAsync();
            Color wikipediaSearchColor = new Color(237, 237, 237);
            EmbedBuilder embed = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            embed.WithTitle($"Wikipedia Search '{search}'");
            embed.WithColor(wikipediaSearchColor);
            embed.WithFooter($"Search by {Context.User}", Context.User.GetAvatarUrl());
            embed.WithCurrentTimestamp();
            embed.WithDescription("Searching Wikipedia...");
            RestUserMessage message = (RestUserMessage)await msg.ReplyAsync("", false, embed.Build());
            WikiSearchResponse response = WikiSearcher.Search(search, new WikiSearchSettings
            {
                ResultLimit = maxSearch
            });

            foreach (WikiSearchResult result in response.Query.SearchResults)
            {
                string link = $"**[{result.Title}]({result.ConstantUrl("en")})** (Words: {result.WordCount})\n{result.Preview}\n\n";

                if (sb.Length > 2047)
                {
                    continue;
                }

                if (sb.Length + link.Length > 2047)
                {
                    continue;
                }

                sb.Append(link);
            }

            embed.WithDescription(sb.ToString());
            embed.WithCurrentTimestamp();
            await Global.ModifyMessage(message, embed);
        }

        [Command("Roleinfo"), Summary("Gets information on the parsed role"), Remarks("(PREFIX)roleinfo <role name> or <@role>"), Alias("role", "role-info", "ri")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task RoleInfo([Remainder] SocketRole role = null)
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            
            if (Context.Guild.Roles.Any(x => x.Name == role?.ToString()))
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.AddField("Role name", role.Mention, true);
                eb.AddField("Role Id", role.Id, true);
                eb.AddField("Users with role", role.Members.Count(), true);
                eb.AddField("Mentionable", role.IsMentionable ? "Yes" : "No", true);
                eb.AddField("Displayed separately", role.IsHoisted ? "Yes" : "No", true);
                eb.AddField("Colour", role.Color, true);
                _ = eb.AddField($"Permissions[{role.Permissions.ToList().Count}]", string.IsNullOrEmpty(string.Join(separator: ", ", values: role.Permissions.ToList().Select(r => r.ToString()))) ? "None" :
                    string.Join(separator: ", ", values: role.Permissions.ToList().Select(r => r.ToString())), true);
                eb.WithFooter($"Role created at {role.CreatedAt}");
                eb.WithColor(role.Color);
                await Context.Message.ReplyAsync("", false, eb.Build());
                tp.Dispose();
            }

            else
            {
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please mention a role",
                    Description = $"Please parse in a role parameter!",
                    Color = Color.Orange,
                };
                await Context.Message.ReplyAsync("", false, eb.Build());
                tp.Dispose();
            }
        }

        [Command("roles"), Summary("Gets a list of all the roles in the server"), Remarks("(PREFIX)roles"), Alias("getroles", "allroles", "getallroless")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetRoles()
        {
            EmbedBuilder eb = new EmbedBuilder();
            _ = eb.AddField($"Roles [{Context.Guild.Roles.Count}]:", string.IsNullOrEmpty(string.Join(separator: ", ", values: Context.Guild.Roles.ToList().Select(r => r.ToString()))) ? "There are no roles" : 
                $"{string.Join(separator: ", ", values: Context.Guild.Roles.ToList().Select(r => r.Mention))}");
            eb.WithAuthor(Context.Message.Author);
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.DarkPurple);
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("rps"), Summary("Play rock paper sciessors with the bot"), Remarks("(PREFIX)rps <rock/paper/scissors>"), Alias("rockpaperscissors", "rock-paper-scissors")]
        public async Task RPS(string choice)
        {
            await Context.Channel.TriggerTypingAsync();

            if (choice.ToLower() != "rock" && choice.ToLower() != "paper" && choice.ToLower() != "scissors")
            {
                await Context.Message.ReplyAsync("Please enter a valid option. Either \"rock\", \"paper\" or \"scissors\"");
            }

            else
            {
                string[] options = { "Rock", "Paper", "Scissors" };
                Random rand = new Random();
                int index = rand.Next(options.Length);
                choice = choice.ToLower();

                if (choice == "rock" && options[index] == "Paper")
                {
                    await Context.Message.ReplyAsync("I win!");
                }

                else if (choice == "rock" && options[index] == "Scissors")
                {
                    await Context.Message.ReplyAsync("You win!");
                }

                else if (choice == "paper" && options[index] == "Rock")
                {
                    await Context.Message.ReplyAsync("You win!");
                }

                else if (choice == "Paper" && options[index] == "Scissors")
                {
                    await Context.Message.ReplyAsync("I win!");
                }

                else if (choice == "scissors" && options[index] == "Rock")
                {
                    await Context.Message.ReplyAsync("I win!");
                }

                else if (choice == "scissors" && options[index] == "Paper")
                {
                    await Context.Message.ReplyAsync("You win!");
                }

                else
                {
                    await Context.Message.ReplyAsync("Draw");
                }
            }
        }

        [Command("flip"), Summary("Challenge me to a coin flip!"), Remarks("(PREFIX)flip <heads/tails>"), Alias("coinflip", "coin", "coin-flip", "flipcoin", "flip-coin")]
        public async Task Flip(string choice = "")
        {
            await Context.Channel.TriggerTypingAsync();

            if (choice.ToLower() != "heads" && choice.ToLower() != "tails" && choice.ToLower() != "")
            {
                await Context.Message.ReplyAsync("Please enter a valid argument. Either heads or tails or leave it blank");
            }

            else
            {
                choice = choice.ToLower();
                string[] options = { "heads", "tails" };
                Random rand = new Random();
                int index = rand.Next(options.Length);

                if (string.IsNullOrWhiteSpace(choice))
                {
                    await Context.Message.ReplyAsync(options[index]);
                }

                else
                {
                    if (options[index] == "heads" && choice == "tails")
                    {
                        await Context.Message.ReplyAsync("I win!");
                    }

                    else if (options[index] == "heads" && choice == "heads")
                    {
                        await Context.Message.ReplyAsync("You win!");
                    }

                    if (options[index] == "tails" && choice == "heads")
                    {
                        await Context.Message.ReplyAsync("I win!");
                    }
                }
            }
        }

        [Command("ysearch", RunMode = RunMode.Async), Summary("Search YouTube for a specific keyword"), Remarks("(PREFIX)ysearch <query>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task SearchYouTube([Remainder] string args = "")
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            string searchFor = string.Empty;
            EmbedBuilder embed = new EmbedBuilder();
            string embedThumb = Context.User.GetAvatarUrl();
            StringBuilder sb = new StringBuilder();
            List<SearchResult> results = null;
            embed.ThumbnailUrl = embedThumb;

            if (string.IsNullOrEmpty(args))
            {
                embed.Title = $"No search term provided!";
                embed.WithColor(new Color(255, 0, 0));
                sb.AppendLine("Please provide a term to search for!");
                embed.Description = sb.ToString();
                await Context.Message.ReplyAsync("", false, embed.Build());
                tp.Dispose();

                return;
            }

            else
            {
                searchFor = args;
                embed.WithColor(new Color(0, 255, 0));
                YouTubeSearcher YouTubesearcher = new YouTubeSearcher();
                results = await YouTubesearcher.SearchChannelsAsync(searchFor);
            }

            if (results != null)
            {
                string videoUrlPrefix = $"https://www.youtube.com/watch?v=";
                embed.Title = $"YouTube Search For (**{searchFor}**)";
                SearchResult thumbFromVideo = results.Where(r => r.Id.Kind == "youtube#video").Take(1).FirstOrDefault();

                if (thumbFromVideo != null)
                {
                    embed.ThumbnailUrl = thumbFromVideo.Snippet.Thumbnails.Default__.Url;
                }

                foreach (SearchResult result in results.Where(r => r.Id.Kind == "youtube#video").Take(3))
                {
                    string fullVideoUrl = string.Empty;
                    string videoId = string.Empty;
                    string description = string.Empty;

                    if (string.IsNullOrEmpty(result.Snippet.Description))
                    {
                        description = "No description available.";
                    }

                    else
                    {
                        description = result.Snippet.Description;
                    }

                    if (result.Id.VideoId != null)
                    {
                        fullVideoUrl = $"{videoUrlPrefix}{result.Id.VideoId}";
                    }

                    sb.AppendLine($":video_camera: **__{result.Snippet.ChannelTitle}__** -> [**{result.Snippet.Title}**]({fullVideoUrl})\n\n *{description}*\n");
                }

                embed.Description = sb.ToString();
                await Context.Message.ReplyAsync("", false, embed.Build());
                tp.Dispose();
            }
        }

        [Command("TranslateTo"), Summary("Translates the input text to the language you specify"), Remarks("(PREFIX)TranslateTo <language code> <text>"), Alias("trto", "tto")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task TranslateTo(string toLanguage, [Remainder] string translate)
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            translate = Regex.Replace(translate, @"^\s*""?|""?\s*$", "");
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(translate)}";
            WebClient webClient = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            string result = webClient.DownloadString(url);

            try
            {
                result = result[4..result.IndexOf("\"", 4, StringComparison.Ordinal)];

                if (result == translate)
                {
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "Error in translation",
                        Description = $"There was an error translating {translate}. Did you use the right [language code](https://sites.google.com/site/opti365/translate_codes)?",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());
                    tp.Dispose();

                    return;
                }

                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.Blue,
                    Description = $"{result}",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
                tp.Dispose();
            }

            catch
            {
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "Error in translation",
                    Description = $"There was an error translating {translate}. Did you use the right [language code](https://sites.google.com/site/opti365/translate_codes)?",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
                tp.Dispose();
            }
        }

        [Command("fact"), Summary("Gives you a random fact"), Remarks("(PREFIX)fact")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Fact(params string[] args)
        {
            await Context.Channel.TriggerTypingAsync();
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync("https://uselessfacts.jsph.pl/random.json");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            resp = Regex.Replace(resp, @"[\]\[]", "");
            APIJsonItems APIData = JsonConvert.DeserializeObject<APIJsonItems>(resp);
            string URL = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={HttpUtility.UrlEncode(APIData.Text)}";
            WebClient webClient = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            string result = webClient.DownloadString(URL);
            result = result[4..result.IndexOf("\"", 4, StringComparison.Ordinal)];
            await Context.Message.ReplyAsync(result);
        }

        [Command("catfact"), Summary("Gets a random cat fact"), Remarks("(PREFIX)catfact")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task CatFact(params string[] args)
        {
            await Context.Channel.TriggerTypingAsync();
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync("https://meowfacts.herokuapp.com/");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            resp = Regex.Replace(resp, @"[\]\[]", "");
            APIJsonItems APIData = JsonConvert.DeserializeObject<APIJsonItems>(resp);
            await Context.Message.ReplyAsync(APIData.Data);
        }

        [Command("trivia"), Summary("Gives you a random trivia question and censored out answer"), Remarks("(PREFIX)trivia")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Trivia(params string[] args)
        {
            await Context.Channel.TriggerTypingAsync();
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync("http://jservice.io/api/random");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            resp = Regex.Replace(resp, @"[\]\[]", "");
            APIJsonItems APIData = JsonConvert.DeserializeObject<APIJsonItems>(resp);
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("Trivia question");
            eb.AddField($"__{APIData.Question}?__", $"\n_ _\n_ _\n||{APIData.Answer}||", false); //The empty unerline just means we can create `illegal` spaces between the question and answer.
            eb.WithColor(Color.DarkPurple);
            eb.WithAuthor(Context.Message.Author);
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        /// <summary>
        /// Holds the data which features such as fact, trivia, catfact etc use in their API.
        /// </summary>
        public class APIJsonItems
        {
            public string Answer { get; set; }
            public string Question { get; set; }
            public string Data { get; set; }
            public string Text { get; set; }
        }

        [Command("leaderboard"), Summary("Gets the top 10 members in the leaderboard for the guild"), Remarks("(PREFIX)leaderboard")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetLeaderboard()
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                string toLevel = await Global.DetermineLevel(Context.Guild);

                if (toLevel.ToLower() == "false")
                {
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        },
                        Title = $"Please enable levelling",
                        Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                        Color = Color.Orange,
                    };

                    await Context.Message.ReplyAsync("", false, eb.Build());
                    tp.Dispose();

                    return;
                }

                else if (toLevel.ToLower() == "off")
                {
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        },
                        Title = $"Please enable levelling",
                        Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                        Color = Color.Orange,
                    };

                    await Context.Message.ReplyAsync("", false, eb.Build());
                    tp.Dispose();

                    return;
                }

                conn.Open();
                List<Dictionary<string, dynamic>> scores = new List<Dictionary<string, dynamic>>();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Levels WHERE guildId = {Context.Guild.Id} ORDER BY totalXP DESC LIMIT 10", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                int count = 0;
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Leaderboard for {Context.Guild}",
                    Color = Color.Green,
                };
                string format = "```";
                string username = "";

                while (reader.Read())
                {
                    count++;

                    if (count <= 10)
                    {
                        Dictionary<string, dynamic> arr = new Dictionary<string, dynamic>();
                        SocketGuildUser user = (SocketGuildUser)Context.Message.Author;

                        if (Context.Guild.GetUser((ulong)reader.GetInt64(0)) == null)
                        {
                            username = $"<@{reader.GetInt64(0)}>";
                        }

                        else
                        {
                            user = Context.Guild.GetUser((ulong)reader.GetInt64(0));

                            if (user.Nickname != null)
                            {
                                username = user.Nickname;
                            }

                            else
                            {
                                username = user.Username;
                            }
                        }

                        arr.Add("name", username);
                        arr.Add("score", reader.GetInt64(5));
                        scores.Add(arr);
                        int spaceCount = 32 - username.Length;
                        string spaces = "";

                        for (int i = 0; i < spaceCount; i++)
                        {
                            spaces += " ";
                        }

                        format += $"{count}.{username}{spaces} | Score: {reader.GetInt64(4)}\n";
                    }

                    else
                    {
                        break;
                    }
                }

                conn.Close();

                try
                {
                    Dictionary<string, List<Dictionary<string, dynamic>>> final_object = new Dictionary<string, List<Dictionary<string, dynamic>>> { { "scores", scores } };
                    HttpClient HTTPClient = new HttpClient();
                    string content = JsonConvert.SerializeObject(final_object);
                    byte[] buffer = Encoding.UTF8.GetBytes(content);
                    ByteArrayContent byteContent = new ByteArrayContent(buffer);
                    byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri("https://api.thom.club/format_leaderboard"),
                        Content = new StringContent(JsonConvert.SerializeObject(final_object), Encoding.UTF8, "application/json"),
                    };
                    HttpResponseMessage HTTPResponse = await HTTPClient.SendAsync(request);
                    string resp = await HTTPResponse.Content.ReadAsStringAsync();
                    Dictionary<string, string> APIData = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp);
                    b.WithCurrentTimestamp();
                    b.Description = APIData["description"];
                    await Context.Message.ReplyAsync("", false, b.Build());
                }

                catch
                {
                    b.WithCurrentTimestamp();
                    b.Description = format + "```";
                    await Context.Message.ReplyAsync("", false, b.Build());
                }

                finally
                {
                    tp.Dispose();
                }
            }

            catch (Exception ex)
            {
                if (ex.Message.GetType() != typeof(NullReferenceException))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithAuthor(Context.User);
                    eb.WithTitle("Error getting info from database:");
                    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                    eb.WithCurrentTimestamp();
                    eb.WithColor(Color.Red);
                    eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
                    await Context.Channel.SendMessageAsync("", false, eb.Build());
                    tp.Dispose();
                    
                    return;
                }
            }

        }

        [Command("Rank"), Summary("Gets the rank for you or another user in a server"), Remarks("(PREFIX)rank (optional)<user>"), Alias("level")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Rank(params string[] arg)
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            string toLevel = await Global.DetermineLevel(Context.Guild);

            if (toLevel.ToLower() == "false")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                    Color = Color.Orange,
                };

                await Context.Message.ReplyAsync("", false, b.Build());
                tp.Dispose();

                return;
            }

            else if (toLevel.ToLower() == "off")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                    Color = Color.Orange,
                };

                await Context.Message.ReplyAsync("", false, b.Build());
                tp.Dispose();

                return;
            }

            if (arg.Length == 0)
            {
                await Context.Message.ReplyAsync("", false, await GetRankAsync(Context.Message.Author, Context.Guild.Id));
                tp.Dispose();
            }

            else
            {
                if (Context.Message.MentionedUsers.Any())
                {
                    await Context.Message.ReplyAsync("", false, await GetRankAsync(Context.Message.MentionedUsers.First(), Context.Guild.Id));
                    tp.Dispose();
                }

                else if (arg[0].Length == 17 || arg[0].Length == 18)
                {
                    SocketUser user = Context.Guild.GetUser(Convert.ToUInt64(arg[0]));
                    await Context.Message.ReplyAsync("", false, await GetRankAsync(user, Context.Guild.Id));
                    tp.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets the users rank from the database.
        /// </summary>
        /// <param name="user">The user to get the rank for.</param>
        /// <param name="guild">The guild to get the users rank for.</param>
        /// <returns>An embed containing the users progress, level and XP.</returns>
        public Task<Embed> GetRankAsync(SocketUser user, ulong guild)
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Levels WHERE guildId = {guild} AND userId = {user.Id}", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Score for {user.Username}",
                    Color = Color.Green,
                };

                while (reader.Read())
                {
                    b.Description = $"Current progress - {reader.GetInt64(5)}/{(long)(5 * Math.Pow(reader.GetInt64(3), 2) + 50 * reader.GetInt64(3) + 100)}\nCurrent progress to next " +
                    $"level - {Math.Round((double)reader.GetInt64(4) / (long)(5 * Math.Pow(reader.GetInt64(3), 2) + 50 * reader.GetInt64(3) + 100) * 100, 2)}%\nLevel - {reader.GetInt64(3)}";
                    b.WithCurrentTimestamp();
                    return Task.FromResult(b.Build());
                }

                conn.Close();
            }

            catch (Exception ex)
            {
                if (ex.Message.GetType() != typeof(NullReferenceException))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithAuthor(user);
                    eb.WithTitle("Error getting info from database:");
                    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                    eb.WithCurrentTimestamp();
                    eb.WithColor(Color.Red);
                    eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
                    return Task.FromResult(eb.Build());
                }
            }

            finally
            {
                conn.Close();
            }

            return null;
        }

        /// <summary>
        /// Gets the number of elements in a query result.
        /// </summary>
        /// <param name="guildId">The Id of the guild to get the channel of.</param>
        /// <returns>A string of a number for how many results there are.</returns>
        private string GetQueryCount(ulong guildId, ulong chanId)
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM snipeLogs WHERE guildId = {guildId} AND chanId = {chanId}", conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                int count = 0;

                while(reader.Read())
                {
                    count++;
                }

                conn.Close();

                return count.ToString();
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                return "err";
            }

            finally
            {
                conn.Close();
            }
        }

        [Command("snipe"), Summary("Gets the most recent deleted message from your guild"), Remarks("(PREFIX)snipe || (PREFIX)snipe (optional)<index>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Snipe(int num = 0)
        {
            await Context.Channel.TriggerTypingAsync();

            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = "Sniped message";
            eb.Description = "Gathering message...";
            IUserMessage msg = await Context.Message.ReplyAsync("", false, eb.Build());
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

            if(num != 0)
            {
                num = num - 1;
            }

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM snipelogs WHERE guildId = {Context.Guild.Id} AND chanId = {Context.Channel.Id} ORDER BY MessageTimestamp DESC LIMIT {num}, 1", conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                EmbedBuilder b = new EmbedBuilder();

                while (reader.Read())
                {
                    b.Title = "Sniped message";
                    b.Description = (string)reader[0];
                    b.WithFooter($"{Global.UnixTimeStampToDateTime(reader.GetDouble(1))}");
                    SocketGuildUser user = (SocketGuildUser)Context.Message.Author;
                    string username = "";
                    ulong uId = Convert.ToUInt64(reader[4]);

                    if (Context.Guild.GetUser(uId) == null || Context.Guild.GetUser(uId).GetType() == typeof(SocketUnknownUser))
                    {
                        username = $"<@{uId}>";
                    }

                    else
                    {
                        user = Context.Guild.GetUser(uId);

                        if (user.Nickname != null)
                        {
                            username = user.Nickname;
                        }

                        else
                        {
                            username = user.Username;
                        }
                    }

                    EmbedAuthorBuilder Author = new EmbedAuthorBuilder()
                    {
                        Name = username,
                        IconUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(),
                    };
                    b.WithAuthor(Author);
                    b.Color = Color.Red;
                }

                conn.Close();
                await Global.ModifyMessage(msg, b);
            }

            catch (Exception ex)
            {
                if(ex.Message.Contains("50035"))
                {
                    string ElemCount = GetQueryCount(Context.Guild.Id, Context.Channel.Id);
                
                    if (ElemCount == "err")
                    {
                        await Global.ModifyMessage(msg, Global.EmbedMessage("Error", $"There are not that many deleted messages in the database for this channel.", false, Color.Red));
                        return;
                    }

                    await Global.ModifyMessage(msg, Global.EmbedMessage("Error", $"There are only {ElemCount} deleted messages in the database for this channel.", false, Color.Red));
                    return;
                }

                if (ex.Message.GetType() != typeof(NullReferenceException))
                {
                    EmbedBuilder eeb = new EmbedBuilder();
                    eeb.WithAuthor(Context.User);
                    eeb.WithTitle("Error getting info from database:");
                    eeb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                    eeb.WithCurrentTimestamp();
                    eeb.WithColor(Color.Red);
                    eeb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
                    await Global.ModifyMessage(msg, eeb);
                }
            }
        }

        [Command("stats"), Summary("Gets the server stats in a fancy graph"), Remarks("(PREFIX)stats <pie, bar, line, doughnut, polararea>")]
        public async Task Stats(params string[] graph)
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            string toLevel = await Global.DetermineLevel(Context.Guild);

            if (toLevel.ToLower() == "false")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                    Color = Color.Orange,
                };

                await Context.Message.ReplyAsync("", false, b.Build());
                tp.Dispose();

                return;
            }

            else if (toLevel.ToLower() == "off")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                    Color = Color.Orange,
                };

                await Context.Message.ReplyAsync("", false, b.Build());
                tp.Dispose();

                return;
            }

            if (graph.Length == 0)
            {
                EmbedBuilder noOp = new EmbedBuilder();
                noOp.WithTitle("Error");
                noOp.WithDescription("Please enter an option!");
                noOp.WithColor(Color.Red);
                noOp.WithAuthor(Context.Message.Author);
                await Context.Message.ReplyAsync("", false, noOp.Build());
                tp.Dispose();

                return;
            }

            else if (graph.Length == 1)
            {
                MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Levels WHERE guildId = {Context.Guild.Id} ORDER BY totalXP DESC LIMIT 10", conn);
                    using MySqlDataReader reader = cmd.ExecuteReader();
                    int count = 0;
                    string Username = "labels: [";
                    string Data = "data: [";
                    SocketGuildUser user = (SocketGuildUser)Context.Message.Author;

                    while (reader.Read())
                    {
                        count++;

                        if (count <= 10)
                        {

                            if (Context.Guild.GetUser((ulong)reader.GetInt64(0)) == null)
                            {
                                Username += $"'<@{reader.GetInt64(0)}>', ";
                            }

                            else
                            {
                                user = Context.Guild.GetUser((ulong)reader.GetInt64(0));

                                if (user.Nickname != null)
                                {
                                    Username += $"'{user.Nickname}', ";
                                }

                                else
                                {
                                    Username += $"'{user.Username}', ";
                                }
                            }

                            Data += $"{reader.GetInt64(5)}, ";
                        }

                        else
                        {
                            break;
                        }
                    }

                    conn.Close();
                    Chart qc = new Chart
                    {
                        Width = 500,
                        Height = 300
                    };
                    Username = Username.Remove(Username.LastIndexOf(','));
                    Username += "]";
                    Data = Data.Remove(Data.LastIndexOf(','));
                    Data += "]";

                    switch (graph[0].ToLower())
                    {
                        case "pie":
                            qc.Config = $"{{type: 'pie', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            tp.Dispose();
                            break;

                        case "bar":
                            qc.Config = $"{{type: 'bar', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            tp.Dispose();
                            break;

                        case "line":
                            qc.Config = $"{{type: 'line', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            tp.Dispose();
                            break;

                        case "doughnut":
                            qc.Config = $"{{type: 'doughnut', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            tp.Dispose();
                            break;

                        case "polararea":
                            qc.Config = $"{{type: 'polarArea', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            tp.Dispose();
                            break;
                        default:
                            await ReplyAsync("Please only enter \"pie\", \"bar\", \"line\", \"doughnut\" or \"polararea\"");
                            tp.Dispose();
                            break;
                    }
                }

                catch (Exception ex)
                {
                    if (ex.Message.GetType() != typeof(NullReferenceException))
                    {
                        EmbedBuilder eb = new EmbedBuilder();
                        eb.WithAuthor(Context.User);
                        eb.WithTitle("Error getting info from database:");
                        eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                        eb.WithCurrentTimestamp();
                        eb.WithColor(Color.Red);
                        eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
                        await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
                        tp.Dispose();
                    }
                }
            }
        }

        [Command("dadjoke"), Summary("Gets a random dad joke"), Remarks("(PREFIX)dadjoke"), Alias("badjoke")]
        public async Task DadJoke(params string[] args)
        {
            await Context.Channel.TriggerTypingAsync();
            DadJokeClient client = new DadJokeClient("ICanHazDadJoke.NET Readme", "https://github.com/mattleibow/ICanHazDadJoke.NET");
            string dadJoke = await client.GetRandomJokeStringAsync();
            await Context.Message.ReplyAsync(dadJoke);
        }

        private void AddToPolls(uint type, MySqlConnection conn, ulong msgId, ulong guildId, ulong authorId, string state, ulong chanId)
        {
            try
            {
                if (type == 0)
                {
                    MySqlCommand cmd = new MySqlCommand($"UPDATE Polls SET message = {msgId}, guildId = {guildId}, author = {authorId}, state = '{state}', chanId = {chanId} WHERE guildId = {guildId} AND author = {authorId}", conn);
                    cmd.ExecuteNonQuery();
                }

                if (type == 1)
                {
                    MySqlCommand cmd = new MySqlCommand($"INSERT INTO Polls(message, guildId, author, state, chanId) VALUES ({msgId}, {guildId}, {authorId}, '{state}', {chanId})", conn);
                    cmd.ExecuteNonQuery();
                }

                if (type == 2)
                {
                    MySqlCommand cmd = new MySqlCommand($"UPDATE Polls SET state = 'Inactive' WHERE guildId = {guildId} AND author = {authorId}", conn);
                    cmd.ExecuteNonQuery();
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        [Command("poll")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.AddReactions)]
        public async Task Poll([Remainder] string question)
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);
            MySqlConnection queryConn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Polls WHERE guildId = {Context.Guild.Id} AND author = {Context.Message.Author.Id}", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                bool hasRan = false;

                try
                {
                    while (reader.Read())
                    {
                        hasRan = true;

                        if (reader.GetString(3) == "Active")
                        {
                            EmbedBuilder eb = new EmbedBuilder
                            {
                                Title = "Poll already active",
                                Description = $"Your poll with ID {reader.GetInt64(0)} is already active, please close this poll by doing {await Global.DeterminePrefix(Context)}endpoll"
                            };
                            eb.WithAuthor(Context.Message.Author);
                            eb.WithCurrentTimestamp();
                            eb.Color = Color.Red;
                            await Context.Message.ReplyAsync("", false, eb.Build());
                            tp.Dispose();

                            return;
                        }

                        else
                        {
                            EmbedBuilder eb = new EmbedBuilder
                            {
                                Title = $"{question}"
                            };
                            eb.WithAuthor(Context.Message.Author);
                            eb.WithFooter($"Poll active at {Context.Message.Timestamp}");
                            RestUserMessage msg = await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
                            tp.Dispose();
                            await msg.AddReactionsAsync(Global.reactions.ToArray());
                            queryConn.Open();
                            AddToPolls(0, queryConn, msg.Id, Context.Guild.Id, Context.Message.Author.Id, "Active", Context.Channel.Id);
                            queryConn.Close();
                        }
                    }

                    conn.Close();

                    if (!hasRan)
                    {
                        EmbedBuilder eb = new EmbedBuilder()
                        {
                            Title = $"{question}"
                        };
                        eb.WithAuthor(Context.Message.Author);
                        eb.WithFooter($"Poll active at {Context.Message.Timestamp}");
                        RestUserMessage msg = await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
                        tp.Dispose();
                        await msg.AddReactionsAsync(Global.reactions.ToArray());
                        queryConn.Open();
                        AddToPolls(1, queryConn, msg.Id, Context.Guild.Id, Context.Message.Author.Id, "Active", Context.Channel.Id);
                        queryConn.Close();
                    }
                }

                catch { }

                finally
                {
                    queryConn.Close();
                }
            }

            catch (Exception ex)
            {
                if (ex.Message.GetType() != typeof(NullReferenceException))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithAuthor(Context.User);
                    eb.WithTitle("Error getting info from database:");
                    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                    eb.WithCurrentTimestamp();
                    eb.WithColor(Color.Red);
                    eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
                    await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
                    tp.Dispose();
                }
            }

            finally
            {
                conn.Close();
            }
        }

        [Command("endpoll")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Endpoll()
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);
            MySqlConnection queryConn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Polls WHERE guildId = {Context.Guild.Id} AND author = {Context.Message.Author.Id}", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                bool hasRan = false;

                try
                {
                    while (reader.Read())
                    {
                        hasRan = true;

                        if (reader.GetString(3) == "Active")
                        {
                            try
                            {
                                ulong mId = (ulong)reader.GetInt64(0);
                                ulong chanId = (ulong)reader.GetInt64(4);
                                ITextChannel channel = (ITextChannel)Context.Guild.GetChannel(chanId);
                                IMessage msg = await channel.GetMessageAsync(mId);
                                EmbedBuilder eb = new EmbedBuilder();
                                eb.WithTitle("Getting poll results...");
                                eb.Color = Color.Orange;
                                RestUserMessage message = (RestUserMessage)await Context.Message.ReplyAsync("", false, eb.Build());
                                msg.Reactions.TryGetValue(Global.reactions[0], out ReactionMetadata YesReactions);
                                msg.Reactions.TryGetValue(Global.reactions[1], out ReactionMetadata NoReactions);
                                eb.Title = $"{msg.Embeds.First().Title}";
                                eb.WithAuthor(Context.Message.Author);
                                eb.WithFooter($"Poll ended at {Context.Message.Timestamp}");
                                eb.AddField("✅", $"{YesReactions.ReactionCount - 1}", true);
                                eb.AddField("❌", $"{NoReactions.ReactionCount - 1}", true);
                                await Global.ModifyMessage(message, eb);
                                queryConn.Open();
                                AddToPolls(2, queryConn, msg.Id, Context.Guild.Id, Context.Message.Author.Id, "Inactive", Context.Channel.Id);
                                queryConn.Close();
                                tp.Dispose();
                            }

                            catch (Exception ex)
                            {
                                await Context.Message.ReplyAsync($"Error: {ex}");
                                queryConn.Open();
                                AddToPolls(2, queryConn, Context.Message.Id, Context.Guild.Id, Context.Message.Author.Id, "Inactive", Context.Channel.Id);
                                queryConn.Close();
                                tp.Dispose();
                            }

                            finally
                            {
                                queryConn.Close();
                                tp.Dispose();
                            }
                        }

                        else
                        {
                            EmbedBuilder eb = new EmbedBuilder
                            {
                                Title = "Poll not active",
                                Description = $"You currently do not have any active polls. You can initiate one by using the {await Global.DeterminePrefix(Context)}poll command"
                            };
                            eb.WithAuthor(Context.Message.Author);
                            eb.WithCurrentTimestamp();
                            eb.Color = Color.Red;
                            await Context.Message.ReplyAsync("", false, eb.Build());
                            tp.Dispose();
                        }
                    }

                    conn.Close();

                    if (!hasRan)
                    {
                        EmbedBuilder eb = new EmbedBuilder
                        {
                            Title = "Poll not active",
                            Description = $"You currently do not have any active polls. You can initiate one by using the {await Global.DeterminePrefix(Context)}poll command"
                        };
                        eb.WithAuthor(Context.Message.Author);
                        eb.WithCurrentTimestamp();
                        eb.Color = Color.Red;
                        await Context.Message.ReplyAsync("", false, eb.Build());
                        tp.Dispose();
                    }
                }

                catch { }

                finally
                {
                    conn.Close();
                }
            }

            catch (Exception ex)
            {
                if (ex.Message.GetType() != typeof(NullReferenceException))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithAuthor(Context.User);
                    eb.WithTitle("Error getting info from database:");
                    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                    eb.WithCurrentTimestamp();
                    eb.WithColor(Color.Red);
                    eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
                    await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
                    tp.Dispose();
                }
            }

            finally
            {
                conn.Close();
            }
        }

        [Command("uptime"), Summary("Gets the percentage of uptime for each of the bot modules"), Remarks("(PREFIX)uptime"), Alias("status")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Uptime(params string[] arg)
        {
            await Context.Channel.TriggerTypingAsync();
            UptimeClient _client = new UptimeClient(Global.StatusPageAPIKey);
            List<Monitor> monitors = await _client.GetMonitors();
            EmbedBuilder eb = new EmbedBuilder();
            monitors.ForEach(item => eb.AddField(item.Name, $"Status: {item.Status}\nUptime: {item.Uptime}%"));
            eb.Title = "Bot status";
            eb.Author = new EmbedAuthorBuilder()
            {
                Name = Context.Message.Author.ToString(),
                IconUrl = Context.Message.Author.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                Url = Context.Message.GetJumpUrl()
            };
            eb.Description = "[View status page](https://status.finlaymitchell.ml)";
            eb.WithCurrentTimestamp();
            eb.WithFooter("Via UptimeRobot");
            eb.Color = Color.Green;
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("botinvite"), Summary("Gets an invite for the bot"), Remarks("(PREFIX)botinvite"), Alias("bot_invite", "invitebot", "invite_bot")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Invite(params string[] arg)
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = "Invite me here!",
                Url = "https://bot.finlaymitchell.ml"
            };

            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("support"), Summary("Gets an invite to the FinBot support server"), Remarks("(PREFIX)support"), Alias("support_invite", "supportinvite", "invite_support")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Support(params string[] arg)
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = "Join my support server!",
                Url = "https://support.finlaymitchell.ml"
            };

            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("website"), Summary("Gets the bots custom-built website"), Remarks("(PREFIX)website")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Website(params string[] arg)
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = "View my website here!",
                Url = "https://finbot.finlaymitchell.ml"
            };

            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("changelog"), Summary("Gets information on the most recent update to the bots code."), Remarks("(PREFIX)changelog"), Alias("changes", "updates", "updatelog")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Changelog()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/Finlay-Mitchell/FinBot/commits");
            string json = await response.Content.ReadAsStringAsync();
            dynamic commits = JArray.Parse(json);
            string commit = commits[0].commit.message;
            string[] title = commit.Split(new string[] { "\n\n" }, 2 , StringSplitOptions.None);
            string authorId = commits[0].committer.id;
            string authorName = commits[0].commit.author.name;
            string commitDate = commits[0].commit.author.date;
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle(title[0]);
            _ = title.Length == 2 ? eb.AddField("Most recent update:", title[1]) : eb.AddField("Most recent update:", "No description could be retrieved");
            eb.Color = Color.Magenta;
            eb.Footer = new EmbedFooterBuilder()
            {
                IconUrl = $"https://avatars.githubusercontent.com/u/{authorId}?v=4",
                Text = $"Author: {authorName}\nCommit date: {commitDate}"
            };
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("invite"), Summary("Gets an invite to the current guild in both the form of a URL and a QR code"), Remarks("(PREFIX)invite"), Alias("guildinvite", "inviteguild", "getinvite", "guild_invite")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.CreateInstantInvite | ChannelPermission.AttachFiles | ChannelPermission.ManageMessages)]
        public async Task invite(params string[] args)
        {
            IReadOnlyCollection<RestInviteMetadata> invites = await Context.Guild.GetInvitesAsync();
            string url = "";

            foreach (RestInviteMetadata invite in invites)
            {
                if(Context.Guild.VanityURLCode != null)
                {
                    RestInviteMetadata vanityURL = await Context.Guild.GetVanityInviteAsync();
                    url = vanityURL.Url;
                    break;
                }

                if (invite.IsTemporary)
                {
                    continue;
                }

                else
                {
                    url = invite.Url;
                    break;
                }
            }

            if(url == "")
            {
                SocketTextChannel channel = Context.Channel as SocketTextChannel;
                IInviteMetadata invite = await channel.CreateInviteAsync(null, null, false);
                url = invite.Url;
            }

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            System.Drawing.Color colour = ColorTranslator.FromHtml("#5539cc");
            Bitmap qrCodeImage = null;

            try
            {
                WebClient WC = new WebClient();
                byte[] iconBytes = WC.DownloadData(Context.Guild.IconUrl);
                Bitmap bmp;
                using (MemoryStream iconMS = new MemoryStream(iconBytes))
                {
                    bmp = new Bitmap(iconMS);
                }
                qrCodeImage = qrCode.GetGraphic(20, colour, System.Drawing.Color.White, bmp);
            }

            catch
            {
                qrCodeImage = qrCode.GetGraphic(20, "#5539cc", "#ffffff");
            }

            System.Drawing.Image img = qrCodeImage;
            MemoryStream ms = new MemoryStream(Global.ImageToByteArray(img)); // We do this as a memorystream because this can be parsed straight to Discord without having to save our image to our computer/server.
            string url_safe = GenerateRandom(); // This means there's a different attachment name every time that we use this command, stopping Discord from caching the message and showing us the wrong one.
            EmbedBuilder eb = new EmbedBuilder();
            IUserMessage msg = await Context.Channel.SendFileAsync(ms, $"guild_invite-{url_safe}.png"); // We do this because sending it to Discord and quickly removing it allows us to get a proxy url, which we do in the next line. 
            eb.ImageUrl = msg.Attachments.First().ProxyUrl;
            await msg.DeleteAsync(); //Since we've already got the URL, we can just delete the image and put it into a nicer looking embed.
            eb.Title = "Scan this invite QR code for the guild";
            eb.Description = $"or simply copy the URL here: {url}";
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        /// <summary>
        /// Generates a "random" URL-safe string.
        /// </summary>
        /// <param name="size">The size of the string to generate.</param>
        /// <returns>A "random" string with the size of the parsed size with a default size of 16.</returns>
        public static string GenerateRandom(uint size = 16)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] stringChars = new char[size];
            Random random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return stringChars.ToString();
        }

        [Command("weather"), Summary("Gets the weather for a given location"), Remarks("(PREFIX)weather <location>")]
        public async Task Weather([Remainder] string city)
        {
            if(city == null)
            {
                await Context.Message.ReplyAsync("Please enter a valid city name.");
            }

            WeatherData Weather = new WeatherData(city);
            Weather.CheckWeather();

            if (Weather.XmlIsNull)
            {
                await ReplyAsync("", false, Global.EmbedMessage("Error", $"Sorry, but weather data on the location \"{city.First().ToString().ToUpper() + city.Substring(1)}\" couldn't be found. Please try a valid location", false, Color.Red).Build());
                return;
            }

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle($"Weather for {city.First().ToString().ToUpper() + city.Substring(1)}");
            eb.WithDescription($"Weather: {Weather.WeatherValue}\nTemperature: {Weather.Temp}°C ({Weather.CelciusToFarenheit(Weather.Temp)}°F)\nFeels like: {Weather.FeelsLike}°C ({Weather.CelciusToFarenheit(Weather.FeelsLike)}°F)" +
                $"\nMax temperature: {Weather.TempMax} ({Weather.CelciusToFarenheit(Weather.TempMax)}°F)\nMin temperature: {Weather.TempMin}°C ({Weather.CelciusToFarenheit(Weather.TempMin)}°F)\nWind speed: {Weather.Windspeed} " +
                $"({Weather.WindspeedValue}m/s)\n------------------------\nPressure: {Weather.Pressure} hPa\nHumidity: {Weather.Humidity}%");
            eb.Author = new EmbedAuthorBuilder()
            {
                IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                Name = $"{Context.User}"
            };
            eb.Color = Weather.DetermineWeatherColour(Weather.WeatherId);
            eb.WithFooter($"Last updated: {Weather.LastUpdated}");
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        //[Command("ftest")]
        public async Task testcmd(params string[] args)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithCurrentTimestamp();
            eb.Color = Color.DarkOrange;
            eb.Footer = new EmbedFooterBuilder()
            {
                IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                Text = $"{Context.User}"
            };

            switch (args[0].ToLower())
            {
                case "circuit":
                    Regex imageSearcher = new Regex("https://www.formula1.com/content/dam/fom-website/2018-redesign-assets/Circuit%20maps%2016x9/(.*?).png.transform/9col/image.png");
                    string url = "";
                    HttpClient HTTPClient = new HttpClient();
                    Regex TrackStats = new Regex("class=\"f1-bold--stat\">(.*?)</p>", RegexOptions.IgnorePatternWhitespace);
                    Regex MultiTrackStats = new Regex("class=\"f1-bold--stat\">(.*?)<span class=\"misc--label d-block d-md-inline\">(.*?)</span>");
                    Regex MultiTrackStatsFirst = new Regex("class=\"f1-bold--stat\">(.*?)<span class=\"misc--label\">");

                    switch (args[1].ToLower())
                    {
                        case "silverstone": case "gb": case "england": case "great britain": case "united kingdom": case "uk":
                            url = $"https://www.formula1.com/en/racing/2021/Great_Britain/Circuit.html";
                            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync(url);
                            string result = await HTTPResponse.Content.ReadAsStringAsync();

                            eb.Description = "**[Silverstone Circuit](https://en.wikipedia.org/wiki/Silverstone_Circuit)**";
                            eb.ImageUrl = imageSearcher.Match(result).Groups[0].Value;
                            eb.AddField("Track statistics:", $"Fastest lap time: {MultiTrackStats.Matches(result)[0].Groups[1]} - {MultiTrackStats.Matches(result)[0].Groups[2]}\n" +
                                $"Number of laps: {TrackStats.Matches(result)[1].Groups[1].Value}\nCircuit length: {MultiTrackStatsFirst.Matches(result)[0].Groups[1].Value}km ({Math.Round(Convert.ToDouble(MultiTrackStatsFirst.Matches(result)[0].Groups[1].Value) / 1.6, 3)}mi)\n" +
                                $"Race distance length: {MultiTrackStatsFirst.Matches(result)[1].Groups[1].Value}km ({Math.Round(Convert.ToDouble(MultiTrackStatsFirst.Matches(result)[1].Groups[1].Value) / 1.6, 3)}mi)\nFirst Grand Prix held: {TrackStats.Matches(result)[0].Groups[1].Value}");
                            eb.AddField("Country:", "England");
                            await ReplyAsync("", false, eb.Build());
                            break;

                        default:
                            await ReplyAsync("error");
                            break;
                    }

                    break;

                case "driver":

                    break;
            }
        }

        /*
         * 
         * BOILERPLACE CODE FOR PYTHON MODULE 
         * 
         */

        [Command("chatbot"), Summary("ALlows you to interact with the AI chatbot"), Remarks("(PREFIX)chatbot")]
        public Task Chatbot(params string[] arg)
        {
            return Task.CompletedTask;
        }
    }
}