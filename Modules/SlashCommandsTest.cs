using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using FinBot.Handlers;
using FinBot.Services;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using QuickChart;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using WikiDotNet;

namespace FinBot.Modules
{
    public class SlashCommandsTest : InteractionModuleBase<ShardedInteractionContext> 
    {
        public InteractionService _commands { get; set; }
        private CommandHandler _handler;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public SlashCommandsTest(IServiceProvider services)
        {
            _handler = services.GetRequiredService<CommandHandler>();
        }

        [SlashCommand("ping", "Gets latency information on the bot")]
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

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = "";
                x.Embed = eb.Build();
            });
        }

        [SlashCommand("reddit", "Gets a post from the parsed subreddit")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Reddit([Summary(name: "subreddit", description: "The subreddit to get a post from(Spaces are ignored and words are joined together).")] string subreddit)
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


        [SlashCommand("say", "Repeats the parsed text")]
        public async Task Echo([Summary(name: "Text", description: "The text to repeat.")] string echo)
        {
            await Context.Channel.TriggerTypingAsync();
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
        public async Task UserInfo([Summary(name: "User", description: "The user to gather information on.")] SocketUser user = null)
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
                _ = nickState == "" ? null : eb.AddField("Nickname", nickState);
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

        [SlashCommand("8ball", "Ask a question and it will respond to you")]
        public async Task EightBall([Summary(name: "Question", description: "The question.")] string input)
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
            [Summary(name: "Number2", description: "The maximum number to chooes from")][MinValue(int.MinValue)][MaxValue(int.MaxValue)] int num2)
        {
            await Context.Channel.TriggerTypingAsync();
            Random r = new Random();
            int ans = r.Next(num1, num2);
            await Context.Interaction.RespondAsync($"Number: {ans}");
        }

        [SlashCommand("av", "Gets users avatar image.")]
        [UserCommand("av")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task AV([Summary(name: "User", description: "The user to get the avatar for.")] SocketUser user = null)
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
            [Summary(name: "ReminderMessage", description: "The message to remind you for.")] string remindMsg = "\"No content set\"")
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
        public async Task RoleInfo([Summary(name: "role", description: "The role to get the information on.")] SocketRole role)
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

        [SlashCommand("rps", "Play rock paper scissors with the bot")]
        public async Task RPS([Summary(name: "option", description: "Either rock, paper or scissors.")] RPSOptions option)
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
        public async Task Flip([Summary(name: "Option", description: "your bet of coin/whether you want to return a result.")] coinFlipParseOptions flipOption = coinFlipParseOptions.random)
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

        [SlashCommand("leaderboard", "Gets the top 10 members most active members the guild.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetLeaderboard()
        {
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
                            Name = Context.User.ToString(),
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        },
                        Title = $"Please enable levelling",
                        Description = $"Please enable levelling by using the enablelevelling command!",
                        Color = Color.Orange,
                    };

                    await Context.Interaction.RespondAsync("", embed: eb.Build());
                    return;
                }

                else if (toLevel.ToLower() == "off")
                {
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.User.ToString(),
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        },
                        Title = $"Please enable levelling",
                        Description = $"Please enable levelling by using the enablelevelling command!",
                        Color = Color.Orange,
                    };

                    await Context.Interaction.RespondAsync("", embed: eb.Build());
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
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Leaderboard for {Context.Guild}",
                    Color = Color.Green,
                };
                string format = "```";
                string username = "";
                SocketGuildUser user;
                int spaceCount;
                string spaces;
                IMongoCollection<BsonDocument> users = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("users");

                while (reader.Read())
                {
                    count++;

                    if (count <= 10)
                    {
                        Dictionary<string, dynamic> arr = new Dictionary<string, dynamic>();
                        user = (SocketGuildUser)Context.User;

                        if (Context.Guild.GetUser((ulong)reader.GetInt64(0)) == null)
                        {
                            BsonDocument dbUser = await users.Find(new BsonDocument { { "_id", reader.GetInt64(0).ToString() } }).FirstOrDefaultAsync();

                            if (dbUser == null)
                            {
                                username = $"<@{reader.GetInt64(0)}>";
                            }

                            else
                            {
                                username = dbUser.GetValue("discordTag").ToString();
                            }
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
                        spaceCount = 32 - username.Length;
                        spaces = "";

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
                    await Context.Interaction.RespondAsync("", embed: b.Build());
                }

                catch
                {
                    b.WithCurrentTimestamp();
                    b.Description = format + "```";
                    await Context.Interaction.RespondAsync("", embed: b.Build());
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

                    return;
                }
            }

        }

        [SlashCommand("rank", "Gets the rank for you/another user in the server.")]
        [UserCommand("rank")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Rank([Summary(name: "User", description: "User to get the rank for.")] SocketUser user = null)
        {
            string toLevel = await Global.DetermineLevel(Context.Guild);

            if (toLevel.ToLower() == "false")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the enablelevelling command!",
                    Color = Color.Orange,
                };
                await Context.Interaction.RespondAsync("", embed: b.Build());

                return;
            }

            else if (toLevel.ToLower() == "off")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the enablelevelling command!",
                    Color = Color.Orange,
                };
                await Context.Interaction.RespondAsync("", embed: b.Build());

                return;
            }

            if (user == null)
            {
                user = Context.Interaction.User;
            }

            await Context.Interaction.RespondAsync("", embed: await GetRankAsync(user, Context.Guild.Id));
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
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
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

        [SlashCommand("stats", "Gets the server user message stats in graph form.")]
        public async Task Stats([Summary(name: "Graph", description: "Gets the type of graph to generate.")] chartTypes graph)
        {
            IDisposable tp = Context.Channel.EnterTypingState();
            string toLevel = await Global.DetermineLevel(Context.Guild);

            if (toLevel.ToLower() == "false")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the enablelevelling command!",
                    Color = Color.Orange,
                };
                await Context.Interaction.RespondAsync("", embed: b.Build());
                tp.Dispose();

                return;
            }

            else if (toLevel.ToLower() == "off")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.User.ToString(),
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the enablelevelling command!",
                    Color = Color.Orange,
                };
                await Context.Interaction.RespondAsync("", embed: b.Build());
                tp.Dispose();

                return;
            }

            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Levels WHERE guildId = {Context.Guild.Id} ORDER BY totalXP DESC LIMIT 10", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                int count = 0;
                string Username = "labels: [";
                string Data = "data: [";
                SocketGuildUser user = (SocketGuildUser)Context.User;
                IMongoCollection<BsonDocument> users = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("users");

                while (reader.Read())
                {
                    count++;

                    if (count <= 10)
                    {

                        if (Context.Guild.GetUser((ulong)reader.GetInt64(0)) == null)
                        {
                            BsonDocument dbUser = await users.Find(new BsonDocument { { "_id", reader.GetInt64(0).ToString() } }).FirstOrDefaultAsync();

                            if (dbUser == null)
                            {
                                Username += $"'<@{reader.GetInt64(0)}>', ";
                            }

                            else
                            {
                                Username = dbUser.GetValue("discordTag").ToString();
                            }
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
                WebClient wc = new WebClient();
                byte[] bytes;
                MemoryStream ms;

                switch (graph)
                {
                    case chartTypes.pie:
                        qc.Config = $"{{type: 'pie', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.Channel.SendFileAsync(ms, $"guild_stats_pie-{Global.GenerateRandom()}.png");
                        tp.Dispose();
                        break;

                    case chartTypes.bar:
                        qc.Config = $"{{type: 'bar', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.Channel.SendFileAsync(ms, $"guild_stats_bar-{Global.GenerateRandom()}.png");
                        tp.Dispose();
                        break;

                    case chartTypes.line:
                        qc.Config = $"{{type: 'line', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.Channel.SendFileAsync(ms, $"guild_stats_line-{Global.GenerateRandom()}.png"); 
                        tp.Dispose();
                        break;

                    case chartTypes.dougnut:
                        qc.Config = $"{{type: 'doughnut', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.Channel.SendFileAsync(ms, $"guild_stats_doughnut-{Global.GenerateRandom()}.png");
                        tp.Dispose();
                        break;

                    case chartTypes.polararea:
                        qc.Config = $"{{type: 'polarArea', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.Channel.SendFileAsync(ms, $"guild_stats_polararea-{Global.GenerateRandom()}.png");
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
                    await Context.Interaction.Channel.SendMessageAsync("", false, eb.Build());
                    tp.Dispose();
                }
            }
        }

        public enum chartTypes
        {
            pie,
            bar,
            polararea,
            dougnut,
            line,
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
                return messages.CountDocuments(new BsonDocument { { "guildId", guildId.ToString() }, { "channelId", chanId.ToString() }, { "deleted", true } });
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                return 0;
            }
        }

        [SlashCommand("snipe", "Gets a deleted message from the database")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Snipe([Summary(name: "index", description: "Selects deleted message by index.")][MinValue(0)] int num = 0)
        {
            if (num < 0)
            {
                await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error", $"You cannot provide a negative index. Index must be unspecified or ≥ 0.", false, Color.Red).Build());
                return;
            }

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

                IFindFluent<BsonDocument, BsonDocument> message = messages.Find(new BsonDocument { { "guildId", Context.Guild.Id.ToString() }, { "channelId", Context.Channel.Id.ToString() }, { "deleted", true } }).Sort(new BsonDocument { { "deletedTimestamp", -1 } }).Limit(1).Skip(num);

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
                    BsonDocument previousMessage = await messages.Find(new BsonDocument { { "guildId", Context.Guild.Id.ToString() }, { "channelId", Context.Channel.Id.ToString() }, { "deleted", false },
                        { "createdTimestamp", new BsonDocument { { "$gte", document.GetValue("createdTimestamp") } } } }).Sort(new BsonDocument { { "createdTimestamp", 1 } }).Limit(1).FirstOrDefaultAsync();
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
                    BsonDocument[] embeds = document.GetValue("embeds").AsBsonArray.Select(x => x.AsBsonDocument).ToArray();

                    if (embeds.Count() > 0)
                    {
                        foreach (BsonDocument embed in embeds)
                        {
                            description = string.IsNullOrEmpty(embed.GetValue("description").ToString()) ? "" : embed.GetValue("description").ToString();
                            content = "";
                            footer = string.IsNullOrEmpty(embed.GetValue("footer").ToString()) ? "" : embed.GetValue("footer").ToString();
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

                            if (!string.IsNullOrEmpty(embed.GetValue("image").ToString()))
                            {
                                eb.ImageUrl = embed.GetValue("image").ToString();
                            }

                            eb.Color = string.IsNullOrEmpty(embed.GetValue("colour").ToString()) ? Color.Default : new Color(Convert.ToUInt32(embed.GetValue("colour")));
                        }

                        eb.Description = embedContent;
                    }

                    else
                    {
                        eb.Description = document.GetValue("content").ToString();
                    }

                    IReadOnlyCollection<IMessage> test = await Context.Channel.GetMessagesAsync(Convert.ToUInt64(previousMessage.GetValue("_id")), Direction.Before, 1).FirstOrDefaultAsync();
                    eb.AddField("_ _", $"[The previous message](https://discord.com/channels/{Context.Guild.Id}/{Context.Channel.Id}/{test.First().Id})", true);

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
                                if (field.GetValue("name") == "" && field.GetValue("value") == "")
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
    }
}
