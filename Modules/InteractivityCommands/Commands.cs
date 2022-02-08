using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using FinBot.Handlers;
using FinBot.Services;
using Google.Apis.YouTube.v3.Data;
using ICanHazDadJoke.NET;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using UptimeSharp;
using UptimeSharp.Models;
using WikiDotNet;
using Color = Discord.Color;

namespace FinBot.Modules
{
    public class InteractiveCommands : InteractionModuleBase<ShardedInteractionContext> 
    {
        public InteractionService _commands { get; set; }
        private CommandHandler _handler;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public InteractiveCommands(IServiceProvider services)
        {
            _handler = services.GetRequiredService<CommandHandler>();
        }

        [SlashCommand("ping", "Gets latency information on the bot.")]
        public async Task slashPing()
        {
            DateTime before = DateTime.Now;
            await Context.Channel.TriggerTypingAsync();
            await Context.Interaction.RespondAsync("Pong!");
            DateTime after = DateTime.Now;
            ulong snowflake = (ulong)Math.Round((after - before).TotalSeconds * 1000);
            ulong Heartbeat = (ulong)Math.Round((double)Context.Client.Latency);
            ulong totalLatency = (ulong)Math.Round((Context.Interaction.GetOriginalResponseAsync().Result.CreatedAt - Context.Interaction.CreatedAt).TotalSeconds * 1000);
            EmbedBuilder eb = new EmbedBuilder
            {
                Title = $"Pong!"
            };
            eb.AddField("Ping to discord", $"{Math.Floor((double)snowflake / 2)}ms");
            eb.AddField("Heartbeat(Me -> Discord -> Me)", $"{Heartbeat}ms");
            eb.AddField("Total time(Your message -> my reply)", $"{totalLatency}ms");
            eb.WithCurrentTimestamp();
            eb.WithAuthor(Context.Interaction.User);

            eb.Color = (totalLatency) switch
            {
                ulong expression when totalLatency <= 400 => Color.Green,
                ulong expression when totalLatency <= 550 && totalLatency > 400 => Color.Orange,
                _ => Color.Red
            };

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "";
                x.Embed = eb.Build();
            });
        }

        [SlashCommand("reddit", "Gets a post from the parsed subreddit.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Reddit([Summary(name: "subreddit", description: "The subreddit to get a post from (Spaces are ignored and words are joined together).")] string subreddit)
        {
            subreddit = subreddit.Replace(" ", "");
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync($"https://www.reddit.com/r/{subreddit}.json");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            RedditHandler data = JsonConvert.DeserializeObject<RedditHandler>(resp);
            Regex r = new Regex(@"https:\/\/i.redd.it\/(.*?)\.");
            IEnumerable<Child> childs = data.Data.Children.Where(x => r.IsMatch(x.Data.Url.ToString())); //For some reason, this can still sometimes throw an exception. Not overly concerned since it doesn't cause any issues.
            IDisposable tp = Context.Channel.EnterTypingState();

            if (!childs.Any())
            {
                await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
                {
                    Title = "Subreddit not found!",
                    Description = $"Sorry, {Context.Interaction.User.Mention} but the [subreddit](https://www.reddit.com/r/{subreddit}) you tried to post from was not found or no images/gifs could be retrieved, please try again.",
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
            Child post = childs.ToArray()[rand.Next() % childs.Count()];
            SocketTextChannel Chan = Context.Interaction.Channel as SocketTextChannel;

            if (!(Chan.IsNsfw) && post.Data.over_18)
            {
                await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
                {
                    Title = "NSFW post!",
                    Description = $"Sorry, {Context.Interaction.User.Mention} but the post you tried to send has been flagged as NSFW. Please try this in a NSFW channel.",
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
            await Context.Interaction.RespondAsync("", embed: b.Build());
            tp.Dispose();
        }


        [SlashCommand("say", "Repeats the parsed text.")]
        public async Task Echo([Summary(name: "Text", description: "The text to repeat.")] string echo)
        {
            await Context.Channel.TriggerTypingAsync();
            await Context.Interaction.RespondAsync("Ignore me");
            await Context.Interaction.GetOriginalResponseAsync().Result.DeleteAsync();
            await Context.Interaction.Channel.SendMessageAsync(await SayTextAsync(echo, Context));
        }

        /// <summary>
        /// Checks and modifies the message parsed to become acceptable to be sent to that guild.
        /// </summary>
        /// <param name="text">The text to check and modify.</param>
        /// <param name="context">The context for the message.</param>
        /// <returns>A string for the suitable message.</returns>
        public async Task<string> SayTextAsync(string text, ShardedInteractionContext context)
        {
            string final = text.ToLower();
            final = Regex.Replace(final, $"{Global.URIAndIpRegex}|@", "");

            if (string.IsNullOrEmpty(final) || string.IsNullOrWhiteSpace(final))
            {
                final = "Whoopsie daisy, my filter has filtered your text and it's returned an empty string, try again, with more sufficient text.";
            }

            try
            {
                IMongoCollection<BsonDocument> collection = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
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


        [SlashCommand("userinfo", "Gets information on user.")]
        [UserCommand("userinfo")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task UserInfo([Summary(name: "User", description: "The user to gather information on - optional.")] SocketUser user = null)
        {
            try
            {
                if (user == null)
                {
                    user = Context.Interaction.User;
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
                _ = nickState == string.Empty ? null : eb.AddField("Nickname", nickState);
                eb.AddField("ID:", $"{user.Id}");
                eb.AddField("Status", user.Status);
                eb.AddField("Active clients", string.IsNullOrEmpty(string.Join(separator: ", ", values: user.ActiveClients.ToList().Select(r => r.ToString()))) || user.IsBot ? ClientError : string.Join(separator: ", ", values: user.ActiveClients.ToList().Select(r => r.ToString())));
                _ = eb.AddField("Created at UTC", user.CreatedAt.UtcDateTime.ToString("r"));
                eb.AddField("Joined at UTC?", SGU.JoinedAt.HasValue ? SGU.JoinedAt.Value.UtcDateTime.ToString("r") : "No value :/");
                _ = eb.AddField($"Roles: [{SGU.Roles.Count}]", $"<@&{string.Join(separator: ">, <@&", values: SGU.Roles.Select(r => r.Id))}>");
                _ = eb.AddField($"Permissions: [{SGU.GuildPermissions.ToList().Count}]", $"{string.Join(separator: ", ", values: SGU.GuildPermissions.ToList().Select(r => r.ToString()))}");
                eb.WithAuthor(Context.Interaction.User);
                eb.WithColor(Color.DarkPurple);
                eb.WithCurrentTimestamp();
                await Context.Interaction.RespondAsync("", embed: eb.Build());
            }

            catch
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor(Context.Interaction.User);
                eb.WithColor(Color.Orange);
                eb.WithTitle($"User not found");
                eb.WithDescription($"Sorry, but I couldn't find that user.");
                eb.WithCurrentTimestamp();
                await Context.Interaction.RespondAsync("", embed: eb.Build());
            }
        }

        [SlashCommand("serverinfo", "Gets information on the server.")]
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
            eb.AddField("boosters", Context.Guild.PremiumSubscriptionCount, true);
            eb.AddField("Boost level", boosttier, true);
            eb.AddField("Number of roles", Context.Guild.Roles.Count, true);
            eb.AddField("Number of channels", $"Text channels: {Context.Guild.TextChannels.Count}\nVoice channels: {Context.Guild.VoiceChannels.Count}\nCategories: {Context.Guild.CategoryChannels.Count}", true);
            _ = eb.AddField($"VIP perks [{Context.Guild.Features.Experimental.Count}]", string.IsNullOrEmpty(string.Join(separator: ", ", values: Context.Guild.Features.Experimental.ToList().Select(r => r.ToString())).ToLower()) ? "None" :
                string.Join(separator: ", ", values: Context.Guild.Features.Experimental.ToList().Select(r => r.ToString())).ToLower().Replace("_", " "), true);
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.Blue);
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("botinfo", "Gets information on the bot.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task BotInfo()
        {
            await Context.Channel.TriggerTypingAsync();
            EmbedBuilder eb = new EmbedBuilder();
            eb.AddField("Developers:", "Finlay Mitchell, Thomas Waffles");
            eb.AddField("Version: ", Global.Version);
            eb.AddField("Languages", "C# - Discord.net API\nPython - Discord.py\nJavascript - Discord.js\n\nPowered by AWS!");
            eb.WithAuthor(Context.Interaction.User);
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
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("8ball", "Ask a question and it will respond to you.")]
        public async Task EightBall([Summary(name: "Question", description: "The question you want to ask.")] string input)
        {
            string[] answers = { "As I see it, yes.", "Ask again later.", "It is certain.", "It is decidedly so.", "Don't count on it.", "Better not tell you now.", "Concentrate and ask again.",
                    "Cannot predict now.", "Most likely.", "My reply is no.", "Yes.", "You may rely on it.", "Yes - definitely.", "Very doubtful.", "Without a doubt.", " My sources say no.",
                    "Outlook not so good.", "Outlook good.", "Reply hazy, try again.", "Signs point to yes."};
            Random rand = new Random();
            int index = rand.Next(answers.Length);
            await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
            {
                Title = input,
                Description = answers[index],
                Timestamp = DateTimeOffset.Now,
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Interaction.User.Username,
                    IconUrl = Context.Interaction.User.GetAvatarUrl() ?? Context.Interaction.User.GetDefaultAvatarUrl()
                },
                Color = Color.Green
            }.Build());
        }

        [SlashCommand("topic", "Gets a random conversation starter.")]
        public async Task Topic()
        {
            await Context.Channel.TriggerTypingAsync();
            string[] topic = File.ReadAllLines(Global.TopicsPath);
            Random rand = new Random();
            int index = rand.Next(topic.Length);
            await Context.Interaction.RespondAsync(topic[index]);
        }

        [SlashCommand("roll", "Rolls a random number between both inputs.")]
        public async Task Roll([Summary(name: "Number1", description: "The minimal number to choose from.")][MinValue(int.MinValue)][MaxValue(int.MaxValue)] int num1,
            [Summary(name: "Number2", description: "The maximum number to choose from.")][MinValue(int.MinValue)][MaxValue(int.MaxValue)] int num2)
        {
            await Context.Channel.TriggerTypingAsync();
            Random r = new Random();
            int ans = r.Next(num1, num2);
            await Context.Interaction.RespondAsync($"Number: {ans}");
        }

        [SlashCommand("av", "Gets users avatar image.")]
        [UserCommand("av")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task AV([Summary(name: "User", description: "The user to get the avatar for - optional.")] SocketUser user = null)
        {
            try
            {
                if (user == null)
                {
                    user = Context.Interaction.User;
                }

                await Context.Channel.TriggerTypingAsync();
                EmbedBuilder eb = new EmbedBuilder()
                {
                    ImageUrl = user.GetAvatarUrl(size: 1024) ?? user.GetDefaultAvatarUrl()
                };
                eb.WithAuthor(Context.Interaction.User)
                .WithColor(Color.DarkTeal)
                .WithDescription($"Heres the profile picture for {user}")
                .WithFooter($"{Context.Interaction.User}")
                .WithCurrentTimestamp();
                await Context.Interaction.RespondAsync("", embed: eb.Build());
            }

            catch
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.WithAuthor(Context.Interaction.User);
                eb.WithColor(Color.Orange);
                eb.WithTitle($"User not found");
                eb.WithDescription($"Sorry, but I couldn't find that user.");
                eb.WithCurrentTimestamp();
                await Context.Interaction.RespondAsync("", embed: eb.Build());
            }
        }

        [SlashCommand("remind", "Sets a reminder for the parsed duration of time.")]
        public async Task Remind([Summary(name: "Duration", description: "How long to set the reminder timer for.")] string duration, 
            [Summary(name: "ReminderMessage", description: "The message to remind you for - optional.")] string remindMsg = "\"No content set\"")
        {
            await Context.Channel.TriggerTypingAsync();
            await ReminderService.SetReminder(Context.Guild, Context.User, (SocketTextChannel)Context.Channel, DateTime.Now, duration, remindMsg, Context);
        }

        [SlashCommand("embed", "Places the parsed text into an embed.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task CmdEmbedMessage([Summary(name: "Title", description: "Sets the text to set the embed title to.")] string titleText, 
            [Summary(name: "Description", description: "Sets the text to set the embed description to.")] string descriptionText)
        {
            await Context.Channel.TriggerTypingAsync();
            await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage(await CheckEmbedContent(titleText, Context), await CheckEmbedContent(descriptionText, Context)).Build());
        }

        /// <summary>
        /// Checks the embed content and makes it suitable for the guild.
        /// </summary>
        /// <param name="text">The text to parse in.</param>
        /// <param name="context">The context for the command.</param>
        /// <returns>A string of the valid text to send.</returns>
        public async Task<string> CheckEmbedContent(string text, ShardedInteractionContext context)
        {
            string final = text.ToLower();
            final = Regex.Replace(final, $"{Global.URIAndIpRegex}|{Global.clientPrefix}", "");

            try
            {
                IMongoCollection<BsonDocument> collection = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
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

        [SlashCommand("translate", "Translates parsed text to English.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Translate([Summary(name: "Translate", description: "Text to translate to English.")] string translate)
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
                    await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "Error in translation",
                        Description = $"There was an error translating {translate}.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Interaction.User.ToString(),
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        }
                    }.Build());
                    tp.Dispose();

                    return;
                }

                await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
                {
                    Color = Color.Blue,
                    Description = $"{result}",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    }
                }.Build());
                tp.Dispose();
            }

            catch
            {
                await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "Error in translation",
                    Description = $"There was an error translating \"{translate}\".",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    }
                }.Build());
                tp.Dispose();
            }
        }

        [SlashCommand("translateto", "Translates the parsed text to the language your specified language.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task TranslateTo([Summary(name: "ToLanguage", description: "The language to translate the text too.")] string toLanguage, [Summary(name: "Text", description: "The text to translate.")] string translate)
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
                    await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "Error in translation",
                        Description = $"There was an error translating {translate}. Did you use the right [language code](https://sites.google.com/site/opti365/translate_codes)?",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.User.ToString(),
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        }
                    }.Build());
                    tp.Dispose();

                    return;
                }

                await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
                {
                    Color = Color.Blue,
                    Description = $"{result}",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    }
                }.Build());
                tp.Dispose();
            }

            catch
            {
                await Context.Interaction.RespondAsync("", embed: new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "Error in translation",
                    Description = $"There was an error translating {translate}. Did you use the right [language code](https://sites.google.com/site/opti365/translate_codes)?",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    }
                }.Build());
                tp.Dispose();
            }
        }

        [SlashCommand("wiki", "Gets articles from Wikipedia based off your parsed string.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Wikipedia([Summary(name: "Article", description: "The desired article to get results for.")] string search)
        {
            await WikiSearch(search, Context.Channel, Context);
        }

        /// <summary>
        /// Searches Wikipedia and gathers results.
        /// </summary>
        /// <param name="search">The term to search Wikipedia for.</param>
        /// <param name="channel">The text channel where the command was called from.</param>
        /// <param name="msg">The message which executed the command.</param>
        /// <param name="maxSearch">The maximum number of search results.</param>
        /// <returns></returns>
        private async Task WikiSearch(string search, ISocketMessageChannel channel, ShardedInteractionContext context, int maxSearch = 10)
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
            await context.Interaction.RespondAsync("", embed: embed.Build());
            WikiSearchResponse response = WikiSearcher.Search(search, new WikiSearchSettings
            {
                ResultLimit = maxSearch
            });
            string link;

            foreach (WikiSearchResult result in response.Query.SearchResults)
            {
                link = $"**[{result.Title}]({result.ConstantUrl("en")})** (Words: {result.WordCount})\n{result.Preview}\n\n";

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
            await context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "";
                x.Embed = embed.Build();
            });
        }

        [SlashCommand("roleinfo", "Gets information on parsed role.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task RoleInfo([Summary(name: "Role", description: "The role to get the information on.")] SocketRole role)
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
                await Context.Interaction.RespondAsync("", embed: eb.Build());
                tp.Dispose();
            }

            else
            {
                EmbedBuilder eb = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please mention a role",
                    Description = $"Please parse in a role parameter!",
                    Color = Color.Orange,
                };
                await Context.Interaction.RespondAsync("", embed: eb.Build());
                tp.Dispose();
            }
        }

        [SlashCommand("roles", "Gets a list of all the roles in the server.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetRoles()
        {
            EmbedBuilder eb = new EmbedBuilder();
            _ = eb.AddField($"Roles [{Context.Guild.Roles.Count}]:", string.IsNullOrEmpty(string.Join(separator: ", ", values: Context.Guild.Roles.ToList().Select(r => r.ToString()))) ? "There are no roles" :
                $"{string.Join(separator: ", ", values: Context.Guild.Roles.ToList().Select(r => r.Mention))}");
            eb.WithAuthor(Context.User);
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.DarkPurple);
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("rps", "Play rock paper scissors with the bot.")]
        public async Task RPS([Summary(name: "Option", description: "Either rock, paper or scissors.")] RPSOptions option)
        {
            await Context.Channel.TriggerTypingAsync();

            Array values = Enum.GetValues(typeof(RPSOptions));
            Random random = new Random();
            RPSOptions randomRPS = (RPSOptions)values.GetValue(random.Next(values.Length));

            if (option == RPSOptions.rock && randomRPS == RPSOptions.paper)
            {
                await Context.Interaction.RespondAsync("I win!");
            }

            else if (option == RPSOptions.rock && randomRPS == RPSOptions.scissors)
            {
                await Context.Interaction.RespondAsync("You win!");
            }

            else if(option == RPSOptions.paper && randomRPS == RPSOptions.rock)
            {
                await Context.Interaction.RespondAsync("You win!");
            }

            else if(option == RPSOptions.paper && randomRPS == RPSOptions.scissors)
            {
                await Context.Interaction.RespondAsync("I win!");
            }

            else if (option == RPSOptions.scissors && randomRPS == RPSOptions.rock)
            {
                await Context.Interaction.RespondAsync("I win!");
            }

            else if (option == RPSOptions.paper && randomRPS == RPSOptions.scissors)
            {
                await Context.Interaction.RespondAsync("You win!");
            }

            else
            {
                await Context.Interaction.RespondAsync("Draw");
            }
        }

        public enum RPSOptions
        {
            rock,
            paper,
            scissors
        }

        [SlashCommand("coinflip", "Play coin flip against the bot, or simply get a result of heads/tails.")]
        public async Task Flip([Summary(name: "Option", description: "Your bet of coin/whether you want to return a result - optional.")] coinFlipParseOptions flipOption = coinFlipParseOptions.random)
        {
            await Context.Channel.TriggerTypingAsync();
            Array values = Enum.GetValues(typeof(coinFlipOptions));
            Random random = new Random();
            coinFlipOptions randomCoinToss = (coinFlipOptions)values.GetValue(random.Next(values.Length));

            if(flipOption == coinFlipParseOptions.random)
            {
                await Context.Interaction.RespondAsync($"You flipped {randomCoinToss}");
                return;
            }

            if (flipOption != (coinFlipParseOptions)randomCoinToss)
            {
                await Context.Interaction.RespondAsync($"You lost! You landed {randomCoinToss}");
            }

            else
            {
                await Context.Interaction.RespondAsync("You won!");
            }
        }

        public enum coinFlipOptions
        {
            heads,
            tails
        }

        public enum coinFlipParseOptions
        {
            heads,
            tails,
            random
        }

        [SlashCommand("ysearch", "Searches YouTube with your parsed query.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task SearchYouTube([Summary(name: "Query", description: "The query to search.")] string args)
        {
            string searchFor = string.Empty;
            EmbedBuilder embed = new EmbedBuilder();
            string embedThumb = Context.User.GetAvatarUrl();
            StringBuilder sb = new StringBuilder();
            List<SearchResult> results = null;
            embed.ThumbnailUrl = embedThumb;
            searchFor = args;
            embed.WithColor(new Color(0, 255, 0));
            YouTubeSearcher YouTubesearcher = new YouTubeSearcher();
            results = await YouTubesearcher.SearchChannelsAsync(searchFor);

            if (results != null)
            {
                string videoUrlPrefix = $"https://www.youtube.com/watch?v=";
                embed.Title = $"YouTube Search For: **{searchFor}**";
                SearchResult thumbFromVideo = results.Where(r => r.Id.Kind == "youtube#video").Take(1).FirstOrDefault();

                if (thumbFromVideo != null)
                {
                    embed.ThumbnailUrl = thumbFromVideo.Snippet.Thumbnails.Default__.Url;
                }

                string fullVideoUrl;
                string videoId;
                string description;

                foreach (SearchResult result in results.Where(r => r.Id.Kind == "youtube#video").Take(3))
                {
                    fullVideoUrl = string.Empty;
                    videoId = string.Empty;
                    description = string.Empty;

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

                    sb.AppendLine($":video_camera: **__{WebUtility.HtmlDecode(result.Snippet.ChannelTitle)}__** -> [**{WebUtility.HtmlDecode(result.Snippet.Title)}**]({fullVideoUrl})\n\n *{WebUtility.HtmlDecode(description)}*\n");
                }

                embed.Description = sb.ToString();
                await Context.Interaction.RespondAsync("", embed: embed.Build());
            }
        }

        [SlashCommand("fact", "Generates a random fact.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Fact()
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
            await Context.Interaction.RespondAsync(result);
        }

        [SlashCommand("trivia", "Gives you a random trivia question and censored out answer.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Trivia()
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
            eb.WithAuthor(Context.User);
            await Context.Interaction.RespondAsync("", embed: eb.Build());
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

        /// <summary>
        /// Gets the number of elements in a query result.
        /// </summary>
        /// <param name="guildId">The Id of the guild to get the channel of.</param>
        /// <returns>A string of a number for how many results there are.</returns>
        private long GetQueryCount(ulong guildId, ulong chanId)
        {
            try
            {
                IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
                return messages.CountDocuments(new BsonDocument { { "deleted", true }, { "guildId", guildId.ToString() }, { "channelId", chanId.ToString() } });
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                return 0;
            }
        }

        [SlashCommand("snipe", "Gets a deleted message from the database.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Snipe([Summary(name: "Index", description: "Selects deleted message by index - optional.")][MinValue(0)][MaxValue(int.MaxValue)] int num = 0)
        {
            if (num != 0)
            {
                num = num - 1;
            }

            try
            {
                IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
                await Context.Channel.TriggerTypingAsync();
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "Sniped message";
                eb.Description = "Gathering message...";
                await Context.Interaction.RespondAsync("", embed: eb.Build());

                if (num >= GetQueryCount(Context.Guild.Id, Context.Interaction.Channel.Id))
                {
                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = "";
                        x.Embed = Global.EmbedMessage("Error", $"There are only {GetQueryCount(Context.Guild.Id, Context.Interaction.Channel.Id)} deleted messages in the database for this channel.", false, Color.Red).Build();
                    });
                    return;
                }

                IFindFluent<BsonDocument, BsonDocument> message = messages.Find(new BsonDocument { { "deleted", true }, { "guildId", Context.Guild.Id.ToString() }, { "channelId", Context.Channel.Id.ToString() } }).Sort(new BsonDocument { { "deletedTimestamp", -1 } }).Limit(1).Skip(num);

                foreach (BsonDocument document in message.ToList())
                {
                    string username = "";
                    SocketGuildUser user = (SocketGuildUser)Context.Interaction.User;
                    IMongoCollection<BsonDocument> users = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("users");
                    BsonDocument dbUser = await users.Find(new BsonDocument { { "_id", document.GetValue("discordId") } }).FirstOrDefaultAsync();

                    if (Context.Guild.GetUser(Convert.ToUInt64(document.GetValue("discordId"))) == null || Context.Guild.GetUser(Convert.ToUInt64(document.GetValue("discordId"))).GetType() == typeof(SocketUnknownUser))
                    {
                        username = dbUser.GetValue("discordTag").ToString();
                    }

                    else
                    {
                        user = Context.Guild.GetUser(Convert.ToUInt64(document.GetValue("discordId")));

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
                        IconUrl = dbUser.GetValue("avatarURL").ToString()
                    };
                    eb.WithAuthor(Author);
                    BsonDocument previousMessage = await messages.Find(new BsonDocument { { "deleted", false }, { "createdTimestamp", new BsonDocument { { "$gte", document.GetValue("createdTimestamp") } } },
                        { "guildId", Context.Guild.Id.ToString() }, { "channelId", Context.Channel.Id.ToString() } }).Sort(new BsonDocument { { "createdTimestamp", 1 } }).Limit(1).FirstOrDefaultAsync();
                    string URLs = "";
                    string attachments = document?.GetValue("attachments").ToJson();

                    if (attachments != null)
                    {
                        List<string> attachmentArray = JsonConvert.DeserializeObject<string[]>(attachments).ToList();

                        foreach (string attachmentURL in attachmentArray)
                        {
                            //WebClient wc = new WebClient();
                            //byte[] bytes = wc.DownloadData(attachmentURL);
                            //MemoryStream ms = new MemoryStream(bytes);
                            //Bitmap bmp = new Bitmap(ms);
                            //System.Drawing.Image img = bmp;
                            //ms = new MemoryStream(Global.ImageToByteArray(img));
                            //await Context.Message.Channel.SendFileAsync(ms, $"{username}-{attachmentURL}-{GenerateRandom()}");
                            URLs += $"\n{attachmentURL}";
                        }
                    }

                    string embedContent = "";
                    string embedTitle = "";
                    string description = "";
                    string embedFields = "";
                    string content = "";
                    string footer = "";
                    BsonDocument[] embeds = { };

                    try
                    {
                        BsonDocument[] edits = document.GetValue("edits").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();

                        await ReplyAsync(document.ToString().Substring(0, 1997) + "...");

                        if (edits.Count() > 0)
                        {
                            embeds = edits[edits.Count() - 1].GetValue("embeds").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();
                        }

                        else
                        {
                            embeds = document.GetValue("embeds").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();
                        }
                    }

                    catch(Exception ex)
                    {
                        Global.ConsoleLog($"{ex}");
                        embeds = document.GetValue("embeds").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();
                    }

                    if (embeds.Count() > 0)
                    {
                        foreach (BsonDocument embed in embeds)
                        {
                            description = string.IsNullOrEmpty(embed.GetValue("description").ToString()) ? "" : embed.GetValue("description").ToString();
                            content = "";
                            embed.TryGetValue("footer", out BsonValue footerVal);
                            footer = (footerVal == null) ? "" : embed.GetValue("footer").ToString();
                            BsonDocument[] fields = embed.GetValue("fields").AsBsonArray.Select(y => y.AsBsonDocument).ToArray();
                            BsonDocument[] titleFields = embed.GetValue("title").AsBsonArray.Select(z => z.AsBsonDocument).ToArray();

                            foreach (BsonDocument title in titleFields)
                            {
                                if (!string.IsNullOrEmpty(title.GetValue("url").ToString()))
                                {
                                    embedTitle = $"**[{title.GetValue("url")}]({title.GetValue("value")})**";
                                }

                                else
                                {
                                    embedTitle = $"**{title.GetValue("value")}**";
                                }
                            }

                            foreach (BsonDocument field in fields)
                            {
                                embedFields += $"**{field.GetValue("name")}**\n{field.GetValue("value")}\n";
                            }

                            embedContent = $"{embedTitle}\n\n{description}\n{embedFields}\n{footer}";
                            embed.TryGetValue("image", out BsonValue imageVal);

                            if (imageVal != null)
                            {
                                eb.ImageUrl = embed.GetValue("image").ToString();
                            }

                            embed.TryGetValue("colour", out BsonValue colourVal);
                            eb.Color = (colourVal == null || colourVal == string.Empty) ? Color.Default : new Color(Convert.ToUInt32(embed.GetValue("colour")));
                        }

                        if (embedContent.Length > 5800)
                        {
                            embedContent = embedContent.Substring(0, 2021) + "...";
                        }

                        eb.Description = embedContent;
                    }

                    else
                    {
                        if (document.GetValue("content").ToString().Length > 2021)
                        {
                            eb.Description = document.GetValue("content").ToString().Substring(0, 2021) + "...";
                        }

                        else
                        {
                            eb.Description = document.GetValue("content").ToString();
                        }
                    }

                    IReadOnlyCollection<IMessage> IPrevious = await Context.Channel.GetMessagesAsync(Convert.ToUInt64(previousMessage.GetValue("_id")), Direction.Before, 1).FirstOrDefaultAsync();
                    eb.AddField("_ _", $"[The previous message](https://discord.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{IPrevious.First().Id})", true);

                    if (!string.IsNullOrEmpty(document.GetValue("replyingTo").ToString()))
                    {
                        eb.AddField("_ _", $"[The message that was replied too](https://discord.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{document.GetValue("replyingTo")})", true);
                    }

                    eb.Footer = new EmbedFooterBuilder()
                    {
                        Text = $"Message sent at: {Global.UnixTimeStampToDateTime(Convert.ToUInt64(document.GetValue("createdTimestamp")))}\nMessage deleted at: {Global.UnixTimeStampToDateTime(Convert.ToUInt64(document.GetValue("deletedTimestamp")))}"
                    };
                    eb.WithCurrentTimestamp();

                    await Context.Interaction.ModifyOriginalResponseAsync(x =>
                    {
                        x.Content = $"{content}\n{URLs}";
                        x.Embed = eb.Build();
                    });
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.ToString());
            }
        }
        
        [MessageCommand("edits")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task edits(IMessage message)
        {
            try
            {
                string referencedMessage = message.Id.ToString();

                if (referencedMessage == null)
                {
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error!", "Please reply to a message for this command.", false, Color.Red).Build());
                    return;
                }

                if(message.EditedTimestamp == null)
                {
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error!", "This message has no edits.", false, Color.Red).Build());
                    return;
                }

                IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
                BsonDocument messageDocument = await messages.Find(new BsonDocument { { "_id", (decimal)/*Context.Interaction.GetOriginalResponseAsync().Result.Reference.MessageId*/message.Id } }).FirstOrDefaultAsync();
                BsonDocument[] editsDocument = messageDocument.GetValue("edits").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();

                if (editsDocument.Count() == 0)
                {
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error!", "This message has no edits.", false, Color.Red).Build());
                    return;
                }

                BsonDocument[] embeds = editsDocument[0].GetValue("embeds").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();
                IMessage msg = message;
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "Getting message edits...";
                eb.Description = "Getting edits for message";
                eb.Color = Color.Orange;
                await Context.Interaction.RespondAsync("", embed: eb.Build());
                eb.Author = new EmbedAuthorBuilder()
                {
                    Name = $"{msg.Author.Username}#{msg.Author.Discriminator}",
                    IconUrl = msg.Author.GetAvatarUrl() ?? msg.Author.GetDefaultAvatarUrl()
                };
                eb.WithCurrentTimestamp();
                eb.Color = Color.Green;
                uint count = 0;
                eb.Description = "";

                if (embeds.Count() > 0)
                {
                    string embedTitle = "";
                    string editsBuilder = "";
                    int fieldCount = 0;
                    eb.Title = "Edits for emebd(Beta)";
                    BsonDocument[] messageEmbeds = messageDocument.GetValue("embeds").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();
                    BsonDocument[] latestEmbedEdit = editsDocument[editsDocument.Count() - 1].GetValue("embeds").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();
                    BsonDocument[] tF = messageEmbeds[0].GetValue("title").AsBsonArray.Select(z => z.AsBsonDocument).ToArray();
                    BsonDocument[] tFLE = latestEmbedEdit[latestEmbedEdit.Count() - 1].GetValue("title").AsBsonArray.Select(z => z.AsBsonDocument).ToArray();

                    if (tFLE[0].GetValue("value") != "" && tFLE[0].GetValue("value") != tF[0].GetValue("value"))
                    {
                        foreach (BsonDocument title in tF)
                        {
                            if (!string.IsNullOrEmpty(title.GetValue("url").ToString()))
                            {
                                eb.Description += $"Original title: [{title.GetValue("url")}]({title.GetValue("value")})\n";
                            }

                            else
                            {
                                eb.Description += $"Original title: {title.GetValue("value")}\n";
                            }
                        }
                    }

                    eb.Description += (latestEmbedEdit[latestEmbedEdit.Count() - 1].GetValue("description") == string.Empty && latestEmbedEdit[latestEmbedEdit.Count() - 1].GetValue("description") != messageEmbeds[0].GetValue("description")) 
                        ? null : eb.Description += $"Original description: {messageEmbeds[0].GetValue("description")}"; //////////
                    
                    foreach (BsonDocument edit in editsDocument)
                    {
                        BsonDocument[] embed = edit.GetValue("embeds").AsBsonArray.Select(y => y.AsBsonDocument).ToArray();

                        foreach (BsonDocument e in embed)
                        {
                            count++;
                            BsonDocument[] titleFields = e.GetValue("title").AsBsonArray.Select(z => z.AsBsonDocument).ToArray();

                            foreach (BsonDocument title in titleFields)
                            {
                                if (!string.IsNullOrEmpty(title.GetValue("url").ToString()))
                                {
                                    embedTitle = $"[{title.GetValue("url")}]({title.GetValue("value")})";
                                }

                                else
                                {
                                    embedTitle = $"{title.GetValue("value")}";
                                }
                            }

                            if (messageEmbeds[0].GetValue("description").ToString() != e.GetValue("description").ToString() && e.GetValue("description") != "")
                            {
                                editsBuilder += $"Description: {e.GetValue("description")}\n";
                            }


                            if(titleFields[0].GetValue("value") != "" && titleFields[0].GetValue("value") != tF[0].GetValue("value"))
                            {
                                if (!string.IsNullOrEmpty(titleFields[0].GetValue("url").ToString()))
                                {
                                    editsBuilder = $"Title: [{titleFields[0].GetValue("url")}]({titleFields[0].GetValue("value")})\n";
                                }

                                else
                                {
                                    editsBuilder = $"Title: {titleFields[0].GetValue("value")}\n";
                                }
                            }

                            BsonDocument[] fields = e.GetValue("fields").AsBsonArray.Select(y => y.AsBsonDocument).ToArray();

                            foreach (BsonDocument field in fields)
                            {
                                if (field.GetValue("name") == string.Empty && field.GetValue("value") == string.Empty)
                                {
                                    continue;
                                }

                                editsBuilder += $"Field {fieldCount} name: {field.GetValue("name")} | value: {field.GetValue("value")}\n";
                                fieldCount += 1;
                            }

                            if(count >= 5)
                            {
                                continue;
                            }

                            if(editsBuilder.Length >= 1024)
                            {
                                editsBuilder = editsBuilder.Substring(0, 1021) + "...";
                            }

                            eb.AddField($"Edit {count} ({Global.UnixTimeStampToDateTime(Convert.ToUInt64(edit.GetValue("updatedTimestamp")))}):", editsBuilder);

                            editsBuilder = "";

                        }
                    }
                }

                else
                {
                    eb.Title = "Edits for message";
                    string editValue = "";

                    foreach (BsonDocument edit in editsDocument)
                    {
                        count++;

                        if(edit.GetValue("content").ToString().Length >= 1021)
                        {
                            editValue = edit.GetValue("content").ToString().Substring(0, 1021) + "...";
                        }

                        else
                        {
                            editValue = edit.GetValue("content").ToString();
                        }

                        if (count == 1)
                        {
                            eb.AddField($"Original message ({Global.UnixTimeStampToDateTime(Convert.ToUInt64(edit.GetValue("updatedTimestamp")))}):", editValue);
                        }

                        else
                        {
                            eb.AddField($"Edit {count - 1} ({Global.UnixTimeStampToDateTime(Convert.ToUInt64(edit.GetValue("updatedTimestamp")))}):", editValue);
                        }
                    }
                }

                while(eb.Length >= 5900)
                {
                    eb.Fields.Remove(eb.Fields[eb.Fields.Count() - 1]);
                }

                eb.AddField("_ _", $"[Jump to message](https://discord.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{/*Context.Interaction.GetOriginalResponseAsync().Result.Reference.MessageId*/message.Id})", true);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = eb.Build();
                });
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.ToString());
            }
        }

        /*
         * Unfortunately the implementation of slash commands within Discord means that I am unable to use them when replying to a message, which this system relies on so for now until I think of a workoround/Discord sorts this I won't be able to do anything.
         */
        //[SlashCommand("edits", "edits")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task edits()
        {
            try
            {
                string referencedMessage = Context.Interaction.GetOriginalResponseAsync().Result.Reference.MessageId.ToString();

                if (referencedMessage == null)
                {
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error!", "Please reply to a message for this command.", false, Color.Red).Build());
                    return;
                }

                IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
                BsonDocument messageDocument = await messages.Find(new BsonDocument { { "_id", (decimal)Context.Interaction.GetOriginalResponseAsync().Result.Reference.MessageId } }).FirstOrDefaultAsync();
                BsonDocument[] editsDocument = messageDocument.GetValue("edits").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();

                if (editsDocument.Count() == 0)
                {
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error!", "This message has no edits.", false, Color.Red).Build());
                    return;
                }

                BsonDocument[] embeds = editsDocument[0].GetValue("embeds").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();
                IMessage msg = await Context.Channel.GetMessageAsync((ulong)Context.Interaction.GetOriginalResponseAsync().Result.Reference.MessageId);
                EmbedBuilder eb = new EmbedBuilder();
                eb.Title = "Getting message edits...";
                eb.Description = "Getting edits for message";
                eb.Color = Color.Orange;
                await Context.Interaction.RespondAsync("", embed: eb.Build());
                eb.Author = new EmbedAuthorBuilder()
                {
                    Name = $"{msg.Author.Username}#{msg.Author.Discriminator}",
                    IconUrl = msg.Author.GetAvatarUrl() ?? msg.Author.GetDefaultAvatarUrl()
                };
                eb.WithCurrentTimestamp();
                eb.Color = Color.Green;
                uint count = 0;
                eb.Description = "";

                if (embeds.Count() > 0)
                {
                    string embedTitle = "";
                    string editsBuilder = "";
                    int fieldCount = 0;
                    eb.Title = "Edits for emebd(Beta)";

                    foreach (BsonDocument edit in editsDocument)
                    {
                        BsonDocument[] embed = edit.GetValue("embeds").AsBsonArray.Select(y => y.AsBsonDocument).ToArray();

                        foreach (BsonDocument e in embed)
                        {
                            count++;
                            BsonDocument[] titleFields = e.GetValue("title").AsBsonArray.Select(z => z.AsBsonDocument).ToArray();

                            foreach (BsonDocument title in titleFields)
                            {
                                if (!string.IsNullOrEmpty(title.GetValue("url").ToString()))
                                {
                                    embedTitle = $"[{title.GetValue("url")}]({title.GetValue("value")})";
                                }

                                else
                                {
                                    embedTitle = $"{title.GetValue("value")}";
                                }
                            }

                            if (!string.IsNullOrEmpty(embedTitle))
                            {
                                eb.Description += $"Original title: {embedTitle}";
                            }

                            if (!string.IsNullOrEmpty(e.GetValue("description").ToString()))
                            {
                                eb.Description += $"\nOriginal description: {e.GetValue("description")}";
                            }

                            BsonDocument[] fields = e.GetValue("fields").AsBsonArray.Select(y => y.AsBsonDocument).ToArray();

                            foreach (BsonDocument field in fields)
                            {
                                if (field.GetValue("name") == string.Empty && field.GetValue("value") == string.Empty)
                                {
                                    continue;
                                }

                                editsBuilder += $"Field {fieldCount} name: {field.GetValue("name")} | value: {field.GetValue("value")}\n";
                                fieldCount += 1;
                            }

                            eb.AddField($"Edit {count} ({Global.UnixTimeStampToDateTime(Convert.ToUInt64(edit.GetValue("updatedTimestamp")))}):", editsBuilder);
                        }
                    }
                }

                else
                {
                    eb.Title = "Edits for message";

                    foreach (BsonDocument edit in editsDocument)
                    {
                        count++;

                        if (count == 1)
                        {
                            eb.AddField($"Original message ({Global.UnixTimeStampToDateTime(Convert.ToUInt64(edit.GetValue("updatedTimestamp")))}):", $"{edit.GetValue("content")}");
                        }

                        else
                        {
                            eb.AddField($"Edit {count - 1} ({Global.UnixTimeStampToDateTime(Convert.ToUInt64(edit.GetValue("updatedTimestamp")))}):", $"{edit.GetValue("content")}");
                        }
                    }
                }

                eb.AddField("_ _", $"[Jump to message](https://discord.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{referencedMessage})", true);
                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embed = eb.Build();
                });
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.ToString());
            }
        }

        [SlashCommand("weather", "Gets the weather for the parsed location.")]
        public async Task Weather([Summary(name: "Location", description: "The location to get the weather information on.")] string city)
        {
            string cityModified = Regex.Replace(city, " ", "+");
            WeatherData Weather = new WeatherData(cityModified);
            Weather.CheckWeather();

            if (Weather.XmlIsNull)
            {
                await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error", $"Sorry, but weather data on the location \"{city.First().ToString().ToUpper() + city.Substring(1)}\" couldn't be found. Please try a valid location", false, Color.Red).Build());
                return;
            }

            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle($"Weather for {city.First().ToString().ToUpper() + city.Substring(1)}");
            eb.WithDescription($"Weather: {Weather.WeatherValue}\nTemperature: {Weather.Temp}°C ({Weather.CelciusToFarenheit(Weather.Temp)}°F)\nFeels like: {Weather.FeelsLike}°C ({Weather.CelciusToFarenheit(Weather.FeelsLike)}°F)" +
                $"\nMax temperature: {Weather.TempMax} ({Weather.CelciusToFarenheit(Weather.TempMax)}°F)\nMin temperature: {Weather.TempMin}°C ({Weather.CelciusToFarenheit(Weather.TempMin)}°F)\nWind speed: {Weather.Windspeed} " +
                $"({Weather.WindspeedValue}m/s)({Weather.WindspeedValue * (60 * 60) / 1000}kmh)({(float)(Math.Round(Weather.WindspeedValue * (60 * 60) / 1000) * 0.6214 * 100f) / 100f}mph)\n------------------------\nPressure: {Weather.Pressure} hPa\nHumidity: {Weather.Humidity}%");
            eb.Author = new EmbedAuthorBuilder()
            {
                IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                Name = $"{Context.User}"
            };
            eb.Color = Weather.DetermineWeatherColour(Weather.WeatherId);
            eb.WithFooter($"Last updated: {Weather.LastUpdated}");
            eb.WithThumbnailUrl("https://purepng.com/public/uploads/large/weather-forecast-symbol-v7o.png");
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("search", "Searches for an image with a title matching the parsed title.")]
        public async Task search([Summary(name: "Query", description: "The term to search for.")] string args)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithCurrentTimestamp();
            eb.ImageUrl = $"https://source.unsplash.com/1600x900/?{args}&content_filter=high";
            eb.Author = new EmbedAuthorBuilder()
            {
                IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                Name = $"{Context.User}"
            };
            eb.WithTitle(args);
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("suggest", "Generates a suggestion for the server.")]
        public async Task Suggest([Summary(name: "Suggestion", description: "What you would like to suggest.")] string suggestion)
        {
            string suggestionschannelid = await Global.DetermineSuggestionChannel(Context.Guild);

            if (suggestionschannelid == "0")
            {
                await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error", "There is no configured suggestions channel.", false, Color.Red).Build());
                return;
            }

            SocketTextChannel suggestionschannel = Context.Guild.GetTextChannel(Convert.ToUInt64(suggestionschannelid));
            RestUserMessage msg = await suggestionschannel.SendMessageAsync("Creating suggestion...");
            EmbedBuilder eb = new EmbedBuilder()
            {
                Color = Color.DarkerGrey,
                Author = new EmbedAuthorBuilder
                {
                    Name = "Not reviewed",
                    IconUrl = "https://cdn.discordapp.com/emojis/787036714337566730.png?v=1"
                }
            };
            eb.AddField("New user suggestion", suggestion);
            eb.AddField("Author", Context.User);
            await msg.ModifyAsync(x =>
            {
                x.Content = "";
                x.Embed = eb.AddField("Suggestion ID", msg.Id).Build();
            });
            await Context.Interaction.RespondAsync($"Created your suggestion in {suggestionschannel.Mention}");
            await msg.AddReactionsAsync(Global.reactions.ToArray());

            if (Context.Channel == suggestionschannel)
            {
                await Task.Delay(5000);
                await Context.Interaction.GetOriginalResponseAsync().Result.DeleteAsync();//DeleteOriginalResponseAsync();
            }
        }

        [SlashCommand("waddle", "Places a users avatar over a penguin.")]
        public async Task Waddle([Summary(name: "User", description: "The user to turn into a penguin - optional.")] SocketUser user = null)
        {
            await Context.Interaction.DeferAsync();

            if(user == null)
            {
                user = Context.User;
            }

            WebClient wc = new WebClient();
            byte[] bytes = wc.DownloadData("https://cdn.discordapp.com/attachments/592463507124125706/719941828476010606/wqqJzxAeASEAAAAASUVORK5CYII.png");
            MemoryStream ms = new MemoryStream(bytes);
            System.Drawing.Image img = System.Drawing.Image.FromStream(ms);
            bytes = wc.DownloadData(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
            ms = new MemoryStream(bytes);
            System.Drawing.Image img2 = System.Drawing.Image.FromStream(ms);
            int width = img.Width;
            int height = img.Height;

            using (img)
            {
                using (Bitmap bitmap = new Bitmap(img.Width, img.Height))
                {
                    using (Graphics canvas = Graphics.FromImage(bitmap))
                    {
                        canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        canvas.DrawImage(img,
                                         new Rectangle(0, //100
                                                       0, //-30
                                                       width,
                                                       height),
                                         new Rectangle(0,
                                                       0,
                                                       img.Width,
                                                       img.Height),
                                         GraphicsUnit.Pixel);

                        canvas.DrawImage(OvalImage(img2), (img.Width / 2) - (img2.Width / 2) + 5, (img.Height / 2) - img2.Height - 65, 120, 110);
                        canvas.Save();
                    }

                    try
                    {
                        img = bitmap;
                        ms = new MemoryStream(Global.ImageToByteArray(img));
                        await Context.Interaction.FollowupWithFileAsync(ms, $"waddle-{Global.GenerateRandom()}-{Context.User.Discriminator}.png");
                    }

                    catch (Exception ex) { }
                }
            }
        }

        public static System.Drawing.Image OvalImage(System.Drawing.Image img)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height);
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(0, 0, img.Width, img.Height);
                using (Graphics gr = Graphics.FromImage(bmp))
                {
                    gr.SetClip(gp);
                    gr.DrawImage(img, Point.Empty);
                }
            }
            return bmp;
        }

        [SlashCommand("dadjoke", "Gets a dad joke.")]
        public async Task DadJoke()
        {
            await Context.Channel.TriggerTypingAsync();
            DadJokeClient client = new DadJokeClient("ICanHazDadJoke.NET Readme", "https://github.com/mattleibow/ICanHazDadJoke.NET");
            string dadJoke = await client.GetRandomJokeStringAsync();
            await Context.Interaction.RespondAsync(dadJoke);
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

        //[SlashCommand("poll", "Generates a poll from you to this channel.")]
        //[RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.AddReactions)]
        //public async Task Poll([Summary(name: "Term", description: "What you would like to poll.")] string question)
        //{
        //    IDisposable tp = Context.Channel.EnterTypingState();
        //    MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);
        //    MySqlConnection queryConn = new MySqlConnection(Global.MySQL.ConnStr);

        //    try
        //    {
        //        conn.Open();
        //        MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Polls WHERE guildId = {Context.Guild.Id} AND author = {Context.User.Id}", conn);
        //        using MySqlDataReader reader = cmd.ExecuteReader();
        //        bool hasRan = false;

        //        try
        //        {
        //            while (reader.Read())
        //            {
        //                hasRan = true;

        //                if (reader.GetString(3) == "Active")
        //                {
        //                    EmbedBuilder eb = new EmbedBuilder
        //                    {
        //                        Title = "Poll already active",
        //                        Description = $"Your poll with ID {reader.GetInt64(0)} is already active, please close this poll by using the endpoll command."
        //                    };
        //                    eb.WithAuthor(Context.User);
        //                    eb.WithCurrentTimestamp();
        //                    eb.Color = Color.Red;
        //                    await Context.Interaction.RespondAsync("", embed: eb.Build());
        //                    tp.Dispose();

        //                    return;
        //                }

        //                else
        //                {
        //                    EmbedBuilder eb = new EmbedBuilder
        //                    {
        //                        Title = $"{question}"
        //                    };
        //                    eb.WithAuthor(Context.User);
        //                    eb.WithFooter($"Poll active at {Context.Interaction.CreatedAt} utc.");
        //                    RestUserMessage msg = await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
        //                    tp.Dispose();
        //                    await msg.AddReactionsAsync(Global.reactions.ToArray());
        //                    queryConn.Open();
        //                    AddToPolls(0, queryConn, msg.Id, Context.Guild.Id, Context.User.Id, "Active", Context.Channel.Id);
        //                    queryConn.Close();
        //                }
        //            }

        //            conn.Close();

        //            if (!hasRan)
        //            {
        //                EmbedBuilder eb = new EmbedBuilder()
        //                {
        //                    Title = $"{question}"
        //                };
        //                eb.WithAuthor(Context.User);
        //                eb.WithFooter($"Poll active at {Context.Interaction.CreatedAt.UtcDateTime} utc.");
        //                RestUserMessage msg = await Context.Interaction.Channel.SendMessageAsync("", false, eb.Build());
        //                tp.Dispose();
        //                await msg.AddReactionsAsync(Global.reactions.ToArray());
        //                queryConn.Open();
        //                AddToPolls(1, queryConn, msg.Id, Context.Guild.Id, Context.User.Id, "Active", Context.Channel.Id);
        //                queryConn.Close();
        //            }
        //        }

        //        catch { }

        //        finally
        //        {
        //            queryConn.Close();
        //        }
        //    }

        //    catch (Exception ex)
        //    {
        //        if (ex.Message.GetType() != typeof(NullReferenceException))
        //        {
        //            EmbedBuilder eb = new EmbedBuilder();
        //            eb.WithAuthor(Context.User);
        //            eb.WithTitle("Error getting info from database:");
        //            eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
        //            eb.WithCurrentTimestamp();
        //            eb.WithColor(Color.Red);
        //            eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
        //            await Context.Interaction.Channel.SendMessageAsync("", false, eb.Build());
        //            tp.Dispose();
        //        }
        //    }

        //    finally
        //    {
        //        conn.Close();
        //    }
        //}

        //[SlashCommand("endpoll", "Ends your current poll from this channel.")]
        //[RequireBotPermission(ChannelPermission.EmbedLinks)]
        //public async Task Endpoll()
        //{
        //    IDisposable tp = Context.Channel.EnterTypingState();
        //    MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);
        //    MySqlConnection queryConn = new MySqlConnection(Global.MySQL.ConnStr);

        //    try
        //    {
        //        conn.Open();
        //        MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Polls WHERE guildId = {Context.Guild.Id} AND author = {Context.User.Id}", conn);
        //        using MySqlDataReader reader = cmd.ExecuteReader();
        //        bool hasRan = false;

        //        try
        //        {
        //            while (reader.Read())
        //            {
        //                hasRan = true;

        //                if (reader.GetString(3) == "Active")
        //                {
        //                    try
        //                    {
        //                        ulong mId = (ulong)reader.GetInt64(0);
        //                        ulong chanId = (ulong)reader.GetInt64(4);
        //                        ITextChannel channel = (ITextChannel)Context.Guild.GetChannel(chanId);
        //                        IMessage msg = await channel.GetMessageAsync(mId);
        //                        EmbedBuilder eb = new EmbedBuilder();
        //                        eb.WithTitle("Getting poll results...");
        //                        eb.Color = Color.Orange;
        //                        RestUserMessage message = (RestUserMessage)await Context.Message.ReplyAsync("", false, eb.Build());

        //                        try
        //                        {
        //                            msg.Reactions.TryGetValue(Global.reactions[0], out ReactionMetadata YesReactions);
        //                            msg.Reactions.TryGetValue(Global.reactions[1], out ReactionMetadata NoReactions);
        //                            eb.Title = $"{msg.Embeds.First().Title}";
        //                            eb.WithAuthor(Context.User);
        //                            eb.WithFooter($"Poll ended at {Context.Interaction.CreatedAt.UtcDateTime} utc.");
        //                            eb.AddField("✅", $"{YesReactions.ReactionCount - 1}", true);
        //                            eb.AddField("❌", $"{NoReactions.ReactionCount - 1}", true);
        //                            eb.AddField("Results:", $"The poll was {(float)Math.Round((YesReactions.ReactionCount - 1f) / (YesReactions.ReactionCount - 1 + NoReactions.ReactionCount - 1) * 10000) / 100f}% positive.");
        //                            await Global.ModifyMessage(message, eb);
        //                            queryConn.Open();
        //                            AddToPolls(2, queryConn, msg.Id, Context.Guild.Id, Context.Message.Author.Id, "Inactive", Context.Channel.Id);
        //                            queryConn.Close();
        //                            tp.Dispose();
        //                        }

        //                        catch (Exception ex)
        //                        {
        //                            if (ex.GetType() == typeof(DivideByZeroException))
        //                            {
        //                                eb.AddField("Results:", $"The poll had zero votes.");
        //                                await Global.ModifyMessage(message, eb);
        //                                queryConn.Open();
        //                                AddToPolls(2, queryConn, msg.Id, Context.Guild.Id, Context.User.Id, "Inactive", Context.Channel.Id);
        //                                queryConn.Close();
        //                                tp.Dispose();
        //                            }

        //                            else
        //                            {
        //                                await Context.Interaction.RespondAsync($"Error: {ex}");
        //                                queryConn.Open();
        //                                AddToPolls(2, queryConn, Context.Message.Id, Context.Guild.Id, Context.User.Id, "Inactive", Context.Channel.Id);
        //                                queryConn.Close();
        //                                tp.Dispose();
        //                            }
        //                        }

        //                        finally
        //                        {
        //                            queryConn.Close();
        //                            tp.Dispose();
        //                        }
        //                    }

        //                    catch { }
        //                }

        //                else
        //                {
        //                    EmbedBuilder eb = new EmbedBuilder
        //                    {
        //                        Title = "Poll not active",
        //                        Description = $"You currently do not have any active polls. You can initiate one by using the poll command."
        //                    };
        //                    eb.WithAuthor(Context.User);
        //                    eb.WithCurrentTimestamp();
        //                    eb.Color = Color.Red;
        //                    await Context.Interaction.RespondAsync("", false, eb.Build());
        //                    tp.Dispose();
        //                }
        //            }

        //            conn.Close();

        //            if (!hasRan)
        //            {
        //                EmbedBuilder eb = new EmbedBuilder
        //                {
        //                    Title = "Poll not active",
        //                    Description = $"You currently do not have any active polls. You can initiate one by using the poll command."
        //                };
        //                eb.WithAuthor(Context.User);
        //                eb.WithCurrentTimestamp();
        //                eb.Color = Color.Red;
        //                await Context.Interaction.RespondAsync("", false, eb.Build());
        //                tp.Dispose();
        //            }
        //        }

        //        catch { }

        //        finally
        //        {
        //            conn.Close();
        //        }
        //    }

        //    catch (Exception ex)
        //    {
        //        if (ex.Message.GetType() != typeof(NullReferenceException))
        //        {
        //            EmbedBuilder eb = new EmbedBuilder();
        //            eb.WithAuthor(Context.User);
        //            eb.WithTitle("Error getting info from database:");
        //            eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
        //            eb.WithCurrentTimestamp();
        //            eb.WithColor(Color.Red);
        //            eb.WithFooter("Please DM the bot \"support <issue>\" about this error and the developers will look at your ticket");
        //            await Context.Interaction.RespondAsync("", embed: eb.Build());
        //            tp.Dispose();
        //        }
        //    }

        //    finally
        //    {
        //        conn.Close();
        //    }
        //}

        [SlashCommand("uptime", "Gets the percentage of uptime for each of the bot modules.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Uptime()
        {
            await Context.Channel.TriggerTypingAsync();
            UptimeClient _client = new UptimeClient(Global.StatusPageAPIKey);
            List<Monitor> monitors = await _client.GetMonitors();
            EmbedBuilder eb = new EmbedBuilder();
            monitors.ForEach(item => eb.AddField(item.Name, $"Status: {item.Status}\nUptime: {item.Uptime}%"));
            eb.Title = "Bot status";
            eb.Author = new EmbedAuthorBuilder()
            {
                Name = Context.User.ToString(),
                IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl()
            };
            eb.Description = "[View status page](https://status.finlaymitchell.ml)";
            eb.WithCurrentTimestamp();
            eb.WithFooter("Via UptimeRobot");
            eb.Color = Color.Green;
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("botinvite", "Gets an invite for the bot.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Invite()
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = "Invite me here!",
                Url = "https://bot.finlaymitchell.ml"
            };
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("support", "Gets an invite to the FinBot support website.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Support()
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = "Join my support server!",
                Url = "https://support.finlaymitchell.ml"
            };
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("changelog", "Gets the most recent update to the bots source code.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Changelog()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/Finlay-Mitchell/FinBot/commits");
            string json = await response.Content.ReadAsStringAsync();
            dynamic commits = JArray.Parse(json);
            string commit = commits[0].commit.message;
            string[] title = commit.Split(new string[] { "\n\n" }, 2, StringSplitOptions.None);
            string authorId = commits[0].committer.id;
            string authorName = commits[0].commit.author.name;
            string commitDate = commits[0].commit.author.date;
            string commitUrl = commits[0].html_url;
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle(title[0]);
            eb.Url = commitUrl;
            _ = title.Length == 2 ? eb.AddField("Most recent update:", title[1]) : eb.AddField("Most recent update:", "No description could be retrieved");
            eb.Color = Color.Magenta;
            eb.Footer = new EmbedFooterBuilder()
            {
                IconUrl = $"https://avatars.githubusercontent.com/u/{authorId}?v=4",
                Text = $"Author: {authorName}\nCommit date: {commitDate}"
            };
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }

        [SlashCommand("invite", "Gets an invite to the current guild in both the form of a URL and a QR code.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.CreateInstantInvite | ChannelPermission.AttachFiles | ChannelPermission.ManageMessages)]
        public async Task invite()
        {
            IReadOnlyCollection<RestInviteMetadata> invites = await Context.Guild.GetInvitesAsync();
            string url = "";

            foreach (RestInviteMetadata invite in invites)
            {
                if (Context.Guild.VanityURLCode != null)
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

            if (url == string.Empty)
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
            string url_safe = Global.GenerateRandom(); // This means there's a different attachment name every time that we use this command, stopping Discord from caching the message and showing us the wrong one.
            EmbedBuilder eb = new EmbedBuilder();
            IUserMessage msg = await Context.Channel.SendFileAsync(ms, $"guild_invite-{url_safe}.png"); // We do this because sending it to Discord and quickly removing it allows us to get a proxy url, which we do in the next line. 
            eb.ImageUrl = msg.Attachments.First().ProxyUrl;
            await msg.DeleteAsync(); //Since we've already got the URL, we can just delete the image and put it into a nicer looking embed.
            eb.Title = "Scan this invite QR code for the guild";
            eb.Description = $"or simply copy the URL here: {url}";
            await Context.Interaction.RespondAsync("", embed: eb.Build());
        }


        [SlashCommand("filldb", "ignore me")]
        public async Task filldb(string localchan)
        {
            if(!Global.IsDev(Context.User))
            {
                return;
            }

            IMongoCollection<BsonDocument> messageDB = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
            IMongoCollection<BsonDocument> users = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("users");
            EmbedBuilder embed = Global.EmbedMessage("Getting messages...", "Generating embed....", false, Color.DarkGreen);
            IUserMessage msg = await Context.Channel.SendMessageAsync("", false, embed.Build());
            messageDB.DeleteOne(new BsonDocument { { "_id", (decimal)msg.Id } });
            long index = 0;
            BsonArray attachments = new BsonArray();
            BsonArray embeds = new BsonArray();
            BsonArray embedFields = new BsonArray();
            BsonArray title = new BsonArray();
            SocketGuildChannel sGC;
            int cI = 0;
            ulong count = 0;

            try
            {
                if (localchan == "localchan")
                {
                    //ulong lastId = Context.Message.Id;
                    //for (int i = 0; i < int.MaxValue; i++)
                    //{
                    //    IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(fromMessageId: lastId, dir: Direction.Before, limit: 50).FlattenAsync();

                    //    foreach (IMessage message in messages)
                    //    {
                    //        index++;
                    //        BsonDocument s = await messageDB.Find(new BsonDocument { { "_id", (decimal)message.Id } }).FirstOrDefaultAsync();

                    //        if (s == null || string.IsNullOrEmpty(s.ToString()))
                    //        {
                    //            sGC = (SocketGuildChannel)message.Channel;

                    //            foreach (Attachment attachment in message.Attachments)
                    //            {
                    //                attachments.Add(attachment.ProxyUrl);
                    //            }

                    //            foreach (Embed e in message.Embeds)
                    //            {
                    //                foreach (EmbedField field in e.Fields)
                    //                {
                    //                    embedFields.Add(new BsonDocument { { "name", field.Name }, { "value", field.Value } });
                    //                }

                    //                title.Add(new BsonDocument { { "value", string.IsNullOrEmpty(e.Title) ? "" : e.Title }, { "url", string.IsNullOrEmpty(e.Url) ? "" : e.Url } });
                    //                embeds.Add(new BsonDocument { { "title", title}, { "description", string.IsNullOrEmpty(e.Description) ? "" : e.Description }, { "fields", embedFields },
                    //            { "footer", string.IsNullOrEmpty(e.Footer.ToString()) ? "" : e.Footer.ToString() }, { "video", string.IsNullOrEmpty(e.Video.ToString()) ? "" : e.Video.ToString() },
                    //            { "image", string.IsNullOrEmpty(e.Image.ToString()) ? "" : e.Image.ToString() }, { "colour", string.IsNullOrEmpty(e.Color.ToString()) ? "" : e.Color.Value.RawValue.ToString() } });
                    //            }

                    //            string reference = "";

                    //            if (message.Reference != null)
                    //            {
                    //                reference = message.Reference.MessageId.ToString();
                    //            }

                    //            BsonDocument user = await users.Find(new BsonDocument { { "_id", message.Author.Id.ToString() } }).FirstOrDefaultAsync();

                    //            if (user == null)
                    //            {
                    //                users.InsertOne(new BsonDocument { { "_id", message.Author.Id.ToString() }, { "discordTag", $"{message.Author.Username}#{message.Author.Discriminator}" },
                    //            { "avatarURL", message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl() } });
                    //            }

                    //            else
                    //            {
                    //                if (user.GetValue("discordTag") != $"{message.Author.Username}#{message.Author.Discriminator}")
                    //                {
                    //                    users.FindOneAndUpdate(new BsonDocument { { "_id", message.Author.Id.ToString() } }, new BsonDocument { { "discordTag", $"{message.Author.Username}#{message.Author.Discriminator}" } });
                    //                }

                    //                if (user.GetValue("avatarURL").ToString() != message.Author.GetAvatarUrl())
                    //                {
                    //                    users.FindOneAndUpdate(new BsonDocument { { "_id", message.Author.Id.ToString() } }, new BsonDocument { { "discordTag", $"{message.Author.Username}#{message.Author.Discriminator}" }, { "avatarURL", message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl() } });
                    //                }
                    //            }

                    //            messageDB.InsertOne(new BsonDocument { { "_id", (decimal)message.Id }, { "discordId",message.Author.Id.ToString() }, { "guildId", sGC.Guild.Id.ToString() }, { "channelId", sGC.Id.ToString() },
                    //            { "createdTimestamp",  (decimal)Global.ConvertToTimestamp(message.CreatedAt.DateTime) }, { "content", string.IsNullOrEmpty(message.Content) ? "" : message.Content},
                    //            { "attachments", attachments }, { "embeds", embeds }, {  "deleted", false }, { "replyingTo", reference } });

                    //            embed.Description = $"{message.Author}({message.Author.Id})\n{message.Id}\nAdded to database\nItem #{count}";
                    //            lastId = message.Id;
                    //        }

                    //        else
                    //        {
                    //            embed.Description = $"{message.Author}({message.Author.Id})\n{message.Id}\nAlready exists within database or couldn't fetch message.\nItem #{count}";
                    //        }

                    //        embed.Footer = new EmbedFooterBuilder()
                    //        {
                    //            Text = $"Getting message {index}/{messages.Count()}"
                    //        };

                    //        await Global.ModifyMessage(msg, embed);
                    //    }
                    //}
                }

                else
                {

                    foreach (SocketTextChannel channel in (from c in Context.Guild.Channels where c.GetType() == typeof(SocketTextChannel) select c).ToList())
                    {
                        cI++;
                        index = 0;
                        IEnumerable<IMessage> messages = await channel.GetMessagesAsync(int.MaxValue).FlattenAsync();

                        foreach (IMessage message in messages)
                        {
                            index++;
                            count++;
                            BsonDocument s = await messageDB.Find(new BsonDocument { { "_id", (decimal)message.Id } }).FirstOrDefaultAsync();

                            if (s == null || string.IsNullOrEmpty(s.ToString()))
                            {
                                sGC = (SocketGuildChannel)message.Channel;

                                foreach (Attachment attachment in message.Attachments)
                                {
                                    attachments.Add(attachment.ProxyUrl);
                                }

                                foreach (Embed e in message.Embeds)
                                {
                                    foreach (EmbedField field in e.Fields)
                                    {
                                        embedFields.Add(new BsonDocument { { "name", field.Name }, { "value", field.Value } });
                                    }

                                    title.Add(new BsonDocument { { "value", string.IsNullOrEmpty(e.Title) ? "" : e.Title }, { "url", string.IsNullOrEmpty(e.Url) ? "" : e.Url } });
                                    embeds.Add(new BsonDocument { { "title", title}, { "description", string.IsNullOrEmpty(e.Description) ? "" : e.Description }, { "fields", embedFields },
                                { "footer", string.IsNullOrEmpty(e.Footer.ToString()) ? "" : e.Footer.ToString() }, { "video", string.IsNullOrEmpty(e.Video.ToString()) ? "" : e.Video.ToString() },
                                { "image", string.IsNullOrEmpty(e.Image.ToString()) ? "" : e.Image.ToString() }, { "colour", string.IsNullOrEmpty(e.Color.ToString()) ? "" : e.Color.Value.RawValue.ToString() } });
                                }

                                string reference = "";

                                if (message.Reference != null)
                                {
                                    reference = message.Reference.MessageId.ToString();
                                }

                                BsonDocument user = await users.Find(new BsonDocument { { "_id", message.Author.Id.ToString() } }).FirstOrDefaultAsync();

                                if (user == null)
                                {
                                    users.InsertOne(new BsonDocument { { "_id", message.Author.Id.ToString() }, { "discordTag", $"{message.Author.Username}#{message.Author.Discriminator}" },
                                { "avatarURL", message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl() } });
                                }

                                else
                                {
                                    if (user.GetValue("discordTag") != $"{message.Author.Username}#{message.Author.Discriminator}")
                                    {
                                        users.FindOneAndUpdate(new BsonDocument { { "_id", message.Author.Id.ToString() } }, new BsonDocument { { "discordTag", $"{message.Author.Username}#{message.Author.Discriminator}" } });
                                    }

                                    if (user.GetValue("avatarURL").ToString() != message.Author.GetAvatarUrl())
                                    {
                                        users.FindOneAndUpdate(new BsonDocument { { "_id", message.Author.Id.ToString() } }, new BsonDocument { { "discordTag", $"{message.Author.Username}#{message.Author.Discriminator}" }, { "avatarURL", message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl() } });
                                    }
                                }

                                messageDB.InsertOne(new BsonDocument { { "_id", (decimal)message.Id }, { "discordId",message.Author.Id.ToString() }, { "guildId", sGC.Guild.Id.ToString() }, { "channelId", sGC.Id.ToString() },
                            { "createdTimestamp",  (decimal)Global.ConvertToTimestamp(message.CreatedAt.DateTime) }, { "content", string.IsNullOrEmpty(message.Content) ? "" : message.Content},
                            { "attachments", attachments }, { "embeds", embeds }, {  "deleted", false }, { "replyingTo", reference } });

                                embed.Description = $"{message.Author}({message.Author.Id})\n{message.Id}\nAdded to database\nChannel: {cI}/{Context.Guild.Channels.Where(x => x.GetType() == typeof(SocketTextChannel)).Count()}({channel.Id})\nItem #{count}";
                            }

                            else
                            {
                                embed.Description = $"{message.Author}({message.Author.Id})\n{message.Id}\nAlready exists within database or couldn't fetch message.\nChannel: {cI}/{Context.Guild.Channels.Where(x => x.GetType() == typeof(SocketTextChannel)).Count()}({channel.Id})\nItem #{count}";
                            }

                            embed.Footer = new EmbedFooterBuilder()
                            {
                                Text = $"Getting message {index}/{messages.Count()} in channel {channel.Name}"
                            };

                            try
                            {
                                await Global.ModifyMessage(msg, embed);
                            }

                            catch
                            {

                            }
                        }
                    }
                }

                embed = Global.EmbedMessage("Completed!", $"The database has been appended with {count} items", false, Color.Green);

                await Global.ModifyMessage(msg, embed);

            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.ToString());
                embed.Color = Color.Red;
                embed.Title = "An error occured:";
                embed.Description = $"{ex.Message}";
                await Global.ModifyMessage(msg, embed);
            }
        }
    }
}
