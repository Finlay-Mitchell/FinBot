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
using FinBot.Services;
using Discord.Rest;
using WikiDotNet;
using System.Text;
using System.Net.Http.Headers;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web;
using System.Data.SQLite;
using QuickChart;
using System.CodeDom.Compiler;
using System.Dynamic;
using System.Reflection;
using ICanHazDadJoke.NET;

namespace FinBot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("reddit"), Summary("Shows a post from the selected subreddit"), Remarks("(PREFIX)reddit <subreddit>"), Alias("r")]
        public async Task Reddit(string subreddit)
        {
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync($"https://www.reddit.com/r/{subreddit}.json");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            RedditHandler data = JsonConvert.DeserializeObject<RedditHandler>(resp);
            Regex r = new Regex(@"https:\/\/i.redd.it\/(.*?)\.");
            IEnumerable<Child> childs = data.Data.Children.Where(x => r.IsMatch(x.Data.Url.ToString()));
            SocketTextChannel Chan = Context.Message.Channel as SocketTextChannel;

            if (!childs.Any())
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Title = "Subreddit not found!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but the [subreddit](https://www.reddit.com/r/{subreddit}) you tried to post from was not found or no images could be retrieved, please try again.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }
                .WithColor(221, 65, 36)
                .Build());
            }

            Random rand = new Random();
            int count = childs.Count();
            Child post = childs.ToArray()[rand.Next() % childs.Count()];
            EmbedBuilder b = new EmbedBuilder()
            {
                Color = new Color(0xFF4301),
                Title = subreddit,
                Description = $"{post.Data.Title}\n",
                ImageUrl = post.Data.Url.ToString(),
                Footer = new EmbedFooterBuilder()
                {
                    Text = "u/" + post.Data.Author
                }
            };
            b.AddField("Post info", $"{post.Data.Ups} upvotes\nurl: https://reddit.com/{post.Data.Permalink}");
            b.WithCurrentTimestamp();

            if (!(Chan.IsNsfw) && post.Data.over_18)
            {
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Title = "NSFW post!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but the post you tried to send has been flagged as NSFW. Please try this in a NSFW channel.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }
                .WithColor(221, 65, 36)
                .Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, b.Build());
            }
        }

        [Command("guess"), Summary("The bot will guess the contents of an image"), Remarks("(PREFIX)guess <image>)")]
        public async Task Guess(params string[] arg)
        {
            if (arg.Length == 1)
            {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                if (Uri.TryCreate(arg.First(), UriKind.RelativeOrAbsolute, out Uri i))
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                {
                    IDisposable tp = Context.Channel.EnterTypingState();
                    HttpClient HTTPClient = new HttpClient();
                    string url = "https://www.google.com/searchbyimage?image_url=" + arg.First();
                    HTTPClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
                    HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync(url);
                    string resp = await HTTPResponse.Content.ReadAsStringAsync();
                    Regex r = new Regex("title=\"Search\" value=\"(.*?)\" aria-label=\"Search\"");

                    if (r.IsMatch(resp))
                    {
                        Match mtch = r.Match(resp);
                        string val = mtch.Groups[1].Value;
                        RestUserMessage msg = await Context.Channel.SendMessageAsync($"Is that {val}?");
                        List<IEmote> reactions = new List<IEmote>()
                        {
                            new Emoji("✅"),
                            new Emoji("❌")
                        };
                        await msg.AddReactionsAsync(reactions.ToArray());
                    }

                    else
                    {
                        await Context.Channel.SendMessageAsync(@"Unable to guess.");
                    }

                    tp.Dispose();
                }

                else
                {
                    await Context.Channel.SendMessageAsync("invalid URL.");
                }
            }

            else if (Context.Message.Attachments.Count == 1)
            {
                IDisposable tp = Context.Channel.EnterTypingState();
                HttpClient HTTPClient = new HttpClient();
                string url = "https://www.google.com/searchbyimage?image_url=" + Context.Message.Attachments.First().ProxyUrl;
                HTTPClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
                HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync(url);
                string resp = await HTTPResponse.Content.ReadAsStringAsync();
                Regex r = new Regex("title=\"Search\" value=\"(.*?)\" aria-label=\"Search\"");

                if (r.IsMatch(resp))
                {
                    Match mtch = r.Match(resp);
                    string val = mtch.Groups[1].Value;
                    RestUserMessage msg = await Context.Channel.SendMessageAsync($"Is that {val}?");
                    List<IEmote> reactions = new List<IEmote>()
                        {
                            new Emoji("✅"),
                            new Emoji("❌")
                        };
                    await msg.AddReactionsAsync(reactions.ToArray());
                    tp.Dispose();
                }

                else
                {
                    await Context.Channel.SendMessageAsync(@"Unable to guess.");
                }

                tp.Dispose();
            }

            else
            {
                await Context.Channel.SendMessageAsync("theres nothing to guess or there is too much.");
            }
        }

        [Command("ping"), Summary("gives you a ping to the Discord API"), Remarks("(PREFIX)ping")]
        public async Task Ping()
        {
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
            {
                Title = $"Pong: {Context.Client.Latency}ms!",
            }
            .WithColor(255, 105, 180)
            .Build());
        }

        [Command("say"), Summary("repeats the text you enter to it"), Remarks("(PREFIX)say <text>"), Alias("echo", "repeat")]
        public async Task Echo([Remainder] string echo)
        {
            IDisposable tp = Context.Channel.EnterTypingState();

            if (Context.Message.MentionedEveryone)
            {
                await Context.Message.Channel.SendMessageAsync("Sorry but you can't mention everyone");
                tp.Dispose();
                return;
            }

            if (Context.Message.MentionedRoles.Any())
            {
                await Context.Channel.SendMessageAsync("sorry but you can't mention roles.");
                tp.Dispose();
                return;
            }

            if (Context.Message.MentionedUsers.Any())
            {
                await Context.Channel.SendMessageAsync("sorry but you can't mention users.");
                tp.Dispose();
                return;
            }

            if (echo.Length != 0)
            {
                await Context.Channel.SendMessageAsync(SayText(string.Join(' ', echo)));
                tp.Dispose();
            }

            else
            {
                await Context.Channel.SendMessageAsync($"What do you want me to say? please do {Global.Prefix}say <msg>.");
                tp.Dispose();
            }

            tp.Dispose();
        }

        public string SayText(string text)
        {
            string final = text.ToLower();
            //string[] leetWords = File.ReadAllLines(Global.CensoredWordsPath);
            //Dictionary<string, string> leetRules = Global.LoadLeetRules();
            //Regex re = new Regex(@"\b(" + string.Join("|", leetWords.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            final = Regex.Replace(final, $"([{Global.Prefix}-])", "");
            final = Regex.Replace(final, @"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", "");
            final = Regex.Replace(final, "@", "");

            //foreach (KeyValuePair<string, string> x in leetRules)
            //{
             //   final = final.Replace(x.Key, x.Value);
            //}

            //final = final.ToLower();
        //    final = Regex.Replace(final, re.ToString(), "");

            if (string.IsNullOrEmpty(final) || string.IsNullOrWhiteSpace(final))
            {
                final = "Whoopsie daisy, my filter has filtered your text and it's returned an empty string, try again, with more sufficient text.";
            }

            return final;
        }

        [Command("userinfo"), Summary("shows information on a user"), Remarks("(PREFIX)userinfo || (PREFIX)userinfo <user>."), Alias("whois", "user", "info", "user-info")]
        public async Task Userinfo(params string[] arg)
        {
            if (arg.Length == 0)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, UserInfo(Context.Message.Author));
            }

            else
            {
                if (Context.Message.MentionedUsers.Any())
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, UserInfo(Context.Message.MentionedUsers.First()));
                }
            }
        }

        public Embed UserInfo([Remainder] SocketUser user)
        {
            SocketGuildUser SGU = (SocketGuildUser)user;
            string nickState = "";
            string ClientError = "None(offline)";

            if (SGU.Nickname != null)
            {
                nickState = SGU.Nickname;
            }

            if (user.IsBot && user.Status.ToString().ToLower() == "online")
            {
                ClientError = "Server hosted";
            }

            EmbedBuilder eb = new EmbedBuilder();
            eb.AddField("User name", user);
            eb.AddField("Nickname?", nickState == "" ? "None" : nickState);
            eb.AddField("ID:", $"{user.Id}");
            eb.AddField("Status", user.Status);
            eb.AddField("Active clients", String.IsNullOrEmpty(String.Join(separator: ", ", values: user.ActiveClients.ToList().Select(r => r.ToString()))) ? ClientError : String.Join(separator: ", ", values: user.ActiveClients.ToList().Select(r => r.ToString())));
            eb.AddField("Created at UTC", user.CreatedAt.UtcDateTime.ToString("r"));
            eb.AddField("Joined at UTC?", SGU.JoinedAt.HasValue ? SGU.JoinedAt.Value.UtcDateTime.ToString("r") : "No value :/");
            eb.AddField($"Roles: [{SGU.Roles.Count}]", $"<@&{String.Join(separator: ">, <@&", values: SGU.Roles.Select(r => r.Id))}>");
            eb.AddField($"Permissions: [{SGU.GuildPermissions.ToList().Count}]", $"{String.Join(separator: ", ", values: SGU.GuildPermissions.ToList().Select(r => r.ToString()))}");
            eb.WithAuthor(SGU);
            eb.WithColor(Color.DarkPurple);
            eb.WithTitle($"{user.Username}");
            eb.WithDescription($"Heres some stats for {user} <3");
            eb.WithCurrentTimestamp();
            eb.Build();
            return eb.Build();
        }

        [Command("serverinfo"), Summary("Shows information on the server"), Remarks("(PREFIX)serverinfo"), Alias("server", "server-info")]
        public async Task ServerInfo()
        {
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
            eb.AddField("Boost level", Context.Guild.PremiumTier, true);
            eb.AddField("Number of roles", Context.Guild.Roles.Count, true);
            eb.AddField("Number of channels", $"Text channels: {Context.Guild.TextChannels.Count}\nVoice channels: {Context.Guild.VoiceChannels.Count}\nCategories: {Context.Guild.CategoryChannels.Count}", true);
            eb.AddField($"VIP perks [{Context.Guild.Features.Count}]", String.IsNullOrEmpty(String.Join(separator: ", ", values: Context.Guild.Features.ToList().Select(r => r.ToString())).ToLower()) ? "None" : String.Join(separator: ", ", values: Context.Guild.Features.ToList().Select(r => r.ToString())).ToLower().Replace("_", " "), true);
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.Blue);
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("botInfo"), Summary("shows some basic bot information"), Remarks("(PREFIX)botinfo")]
        public async Task BotInfo()
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);
            await Context.Channel.TriggerTypingAsync();
            EmbedBuilder eb = new EmbedBuilder();
            eb.AddField("Developer:", "Finlay Mitchell");
            eb.AddField("Version: ", Global.Version);
            eb.AddField("Language", "C# - Discord.net API");
            eb.WithAuthor(GuildUser);
            eb.WithColor(Color.Gold);
            eb.WithTitle("Bot info");
            eb.AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}");
            eb.AddField("Runtime", $"{RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}");
            eb.AddField($"Heap size", $"{Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString()} MB");
            eb.AddField("How many servers am I in?", Context.Client.Guilds.Count());
            eb.AddField("Invite to your server", "[Invite link](https://discord.com/api/oauth2/authorize?client_id=811919327086903307&permissions=8&scope=bot)"); 
            eb.AddField("Join the support server", "[here](https://discord.com/invite/j345G7RuF6)");
            eb.AddField($"Special thanks", "Thomas_Waffles#0001");
            eb.WithDescription($"Here's some info on me");
            eb.WithCurrentTimestamp();
            eb.WithDescription("To support the developers, [please feel free to donate](http://ec2-35-176-187-24.eu-west-2.compute.amazonaws.com/donate.html)!");
            await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("8ball"), Summary("with 8ball magic, ask it a question and it will respond to you"), Remarks("(PREFIX)8ball <question>"), Alias("eightball", "ask")]
        public async Task EightBall([Remainder] string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("Please enter a parameter");
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                string[] answers = { "As I see it, yes.", "Ask again later.", "It is certain.", "It is decidedly so.", "Don't count on it.", "Better not tell you now.", "Concentrate and ask again.", " Cannot predict now.",
                "Most likely.", "My reply is no", "Yes.", "You may rely on it.", "Yes - definitely.", "Very doubtful.", "Without a doubt.", " My sources say no.", " Outlook not so good.", "Outlook good.", "Reply hazy, try again",
                "Signs point to yes"};
                Random rand = new Random();
                int index = rand.Next(answers.Length);
                await Context.Message.Channel.SendMessageAsync(answers[index]);
            }
        }

        [Command("topic"), Summary("sends a random conversation starter"), Remarks("(PREFIX)topic")]
        public async Task Topic()
        {
            await Context.Channel.TriggerTypingAsync();
            string[] topic = File.ReadAllLines(Global.TopicsPath);
            Random rand = new Random();
            int index = rand.Next(topic.Length);
            await Context.Message.Channel.SendMessageAsync(topic[index]);
        }

        [Command("roll"), Summary("rolls a random number between 0 and your number"), Remarks("(PREFIX)roll <number>")]
        public async Task Roll(int num)
        {
            Random r = new Random();
            int ans = r.Next(0, num);
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.Channel.SendMessageAsync($"Number: {ans}");
        }

        [Command("av"), Summary("show users avatar"), Remarks("(PREFIX)av <user>"), Alias("Avatar", "profile")]
        public async Task AvAsync(params string[] arg)
        {
            if (arg.Length == 0)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, AV(Context.Message.Author));
            }

            else
            {
                if (Context.Message.MentionedUsers.Any())
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, AV(Context.Message.MentionedUsers.First()));
                }
            }
        }

        public Embed AV(SocketUser user)
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                ImageUrl = user.GetAvatarUrl(size: 1024)
            };
            eb.WithAuthor(Context.Message.Author)
            .WithColor(Color.DarkTeal)
            .WithDescription($"Heres the profile picture for {user}")
            .WithFooter($"{Context.Message.Author}")
            .WithCurrentTimestamp()
            .Build();
            return eb.Build();
        }

        [Command("remind", RunMode = RunMode.Async), Summary("Reminds you with a custom message (In Seconds)"), Remarks("(PREFIX)remain <seconds> <message>"), Alias("Timer")]
        public async Task Remind(int seconds, [Remainder] string remindMsg)
        {
            if (seconds > 604800)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("Can not set a reminder longer than a week");
            }

            else if (seconds < 0)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("Can not set reminders for less than a second");
            }

            else if (remindMsg.Contains("@everyone") || remindMsg.Contains("@here"))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync($"Sorry but can't mention everybody");
            }

            else if (Context.Message.MentionedUsers.Any())
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync($"Sorry but you can't mention users");
            }

            else if (Context.Message.MentionedRoles.Any())
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync($"Sorry but you can't mention roles");
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync($"Ok, set the reminder '{remindMsg}' in {seconds} seconds.");
                await ReminderService.RemindAsyncSeconds(Context.User, seconds, remindMsg, Context.Message.Channel);
            }
        }

        [Command("embed"), Summary("Displays your message in an embed message"), Remarks("(PREFIX)embed <title>, <description>"), Alias("embedmessage")]
        public async Task CmdEmbedMessage([Remainder] string text = "")
        {
            if (text.Contains(","))
            {
                string[] result = text.Split(',');
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, EmbedMessage(result[0], result[1]).Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, EmbedMessage(text).Build());
            }
        }

        private EmbedBuilder EmbedMessage(string title, string msg = "")
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(msg);
            embed.Color = ColorPicker();
            return embed;
        }

        private Color ColorPicker()
        {
            Color[] Colours = {Color.Blue, Color.DarkBlue, Color.DarkerGrey, Color.DarkGreen, Color.DarkGrey, Color.DarkMagenta, Color.DarkOrange, Color.DarkPurple, Color.DarkRed, Color.DarkTeal, Color.Default, Color.Gold, Color.Green,
            Color.LighterGrey, Color.LightGrey, Color.LightOrange, Color.Magenta, Color.Orange, Color.Purple, Color.Red, Color.Teal };
            Random rand = new Random();
            int index = rand.Next(Colours.Length);
            return Colours[index];
        }

        [Command("translate")]
        public async Task Translate([Remainder] string translate)
        {
            translate = Regex.Replace(translate, @"^\s*""?|""?\s*$", "");
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={HttpUtility.UrlEncode(translate)}";
            WebClient webClient = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            string result = webClient.DownloadString(url);

            try
            {
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);

                if (result == translate)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "Error in translation",
                        Description = $"There was an error translating {translate}.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.Blue,
                    Description = $"{result}",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }

            catch
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "Error in translation",
                    Description = $"There was an error translating {translate}.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("wiki", RunMode = RunMode.Async), Alias("wikipedia"), Summary("Searches Wikipedia"), Remarks("(PREFIX)wiki <phrase>")]
        public async Task Wikipedia([Remainder] string search = "")
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                await Context.Channel.SendMessageAsync("A search needs to be entered! E.G: `wiki testing`");
                return;
            }

            await WikiSearch(search, Context.Channel);
        }

        private async Task WikiSearch(string search, ISocketMessageChannel channel, int maxSearch = 10)
        {
            Color wikipediaSearchColor = new Color(237, 237, 237);
            EmbedBuilder embed = new EmbedBuilder();
            StringBuilder sb = new StringBuilder();
            embed.WithTitle($"Wikipedia Search '{search}'");
            embed.WithColor(wikipediaSearchColor);
            embed.WithFooter($"Search by {Context.User}", Context.User.GetAvatarUrl());
            embed.WithCurrentTimestamp();
            embed.WithDescription("Searching Wikipedia...");
            RestUserMessage message = await channel.SendMessageAsync("", false, embed.Build());
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
            await ModifyMessage(message, embed);
        }

        public static async Task ModifyMessage(IUserMessage baseMessage, string newMessage)
        {
            await baseMessage.ModifyAsync(x => { x.Content = newMessage; });
        }

        public static async Task ModifyMessage(IUserMessage baseMessage, EmbedBuilder embed)
        {
            await baseMessage.ModifyAsync(x => { x.Embed = embed.Build(); });
        }

        [Command("time"), Summary("Gets the time of a location"), Remarks("(PREFIX)time <location>")]
        public async Task Time([Remainder] string loc)
        {
            Regex r1 = new Regex(".*");
            loc = "time " + loc;

            if (r1.IsMatch(loc.ToLower()))
            {
                HttpClient HTTPClient = new HttpClient();
                HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync($"https://www.google.com/search?q={loc.ToLower().Replace(' ', '+')}");
                string resp = await HTTPResponse.Content.ReadAsStringAsync();
                Regex x = new Regex(@"<div class=""BNeawe iBp4i AP7Wnd""><div><div class=""BNeawe iBp4i AP7Wnd"">(.*?)<\/div><\/div>");

                if (x.IsMatch(resp))
                {
                    string time = x.Match(resp).Groups[1].Value;
                    HTTPClient.Dispose();

                    if (!loc.Contains("in"))
                    {
                        int index = loc.IndexOf("time");
                        loc = loc.Insert(index + 4, " in ");
                    }

                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.Blue,
                        Description = $"The current {loc.ToLower()} is {time}",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());
                }

                else
                {
                    HTTPClient.Dispose();

                    if (!loc.Contains("in"))
                    {
                        int index = loc.IndexOf("time");
                        loc = loc.Insert(index + 4, " in");
                    }

                    await Context.Message.Channel.SendMessageAsync($"Sorry buddy but could not get the {loc.ToLower().Replace("what time is it ", "")}");
                }
            }
        }

        [Command("Roleinfo"), Summary("Gets information on the parsed role"), Remarks("(PREFIX)roleinfo <role name> or <@role>"), Alias("role", "role-info", "ri")]
        public async Task RoleInfo([Remainder] SocketRole role)
        {
            if (Context.Guild.Roles.Any(x => x.Name == role.ToString()))
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.AddField("Role name", role.Mention, true);
                eb.AddField("Role Id", role.Id, true);
                eb.AddField("Users with role", role.Members.Count(), true);
                eb.AddField("Mentionable", role.IsMentionable ? "Yes" : "No", true);
                eb.AddField("Displayed separately", role.IsHoisted ? "Yes" : "No", true);
                eb.AddField("Colour", role.Color, true);
                eb.AddField($"Permissions[{role.Permissions.ToList().Count}]", String.IsNullOrEmpty(String.Join(separator: ", ", values: role.Permissions.ToList().Select(r => r.ToString()))) ? "None" : String.Join(separator: ", ", values: role.Permissions.ToList().Select(r => r.ToString())), true);
                eb.WithFooter($"Role created at {role.CreatedAt}");
                eb.WithColor(role.Color);
                await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
            }
        }

        [Command("roles"), Summary("Gets a list of all the roles in the server"), Remarks("(PREFIX)roles")]
        public async Task GetRoles()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.AddField($"Roles [{Context.Guild.Roles.Count}]:", String.IsNullOrEmpty(String.Join(separator: ", ", values: Context.Guild.Roles.ToList().Select(r => r.ToString()))) ? "There are no roles" : $"{String.Join(separator: ", ", values: Context.Guild.Roles.ToList().Select(r => r.Mention))}");
            eb.WithAuthor(Context.Message.Author);
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.DarkPurple);
            await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("rps"), Summary("Play rock paper sciessors with the bot"), Remarks("(PREFIX)rps <rock/paper/scissors>"), Alias("rockpaperscissors", "rock-paper-scissors")]
        public async Task RPS(string choice)
        {
            if (choice.ToLower() != "rock" && choice.ToLower() != "paper" && choice.ToLower() != "scissors")
            {
                await Context.Message.Channel.SendMessageAsync("Please enter a valid option. Either \"rock\", \"paper\" or \"scissors\"");
            }

            else
            {
                string[] options = { "Rock", "Paper", "Scissors" };
                Random rand = new Random();
                int index = rand.Next(options.Length);
                choice = choice.ToLower();

                if (choice == "rock" && options[index] == "Paper")
                {
                    await Context.Message.Channel.SendMessageAsync("I win!");
                }

                else if (choice == "rock" && options[index] == "Scissors")
                {
                    await Context.Message.Channel.SendMessageAsync("You win!");
                }

                else if (choice == "paper" && options[index] == "Rock")
                {
                    await Context.Message.Channel.SendMessageAsync("You win!");
                }

                else if (choice == "Paper" && options[index] == "Scissors")
                {
                    await Context.Message.Channel.SendMessageAsync("I win!");
                }

                else if (choice == "scissors" && options[index] == "Rock")
                {
                    await Context.Message.Channel.SendMessageAsync("I win!");
                }

                else if (choice == "scissors" && options[index] == "Paper")
                {
                    await Context.Message.Channel.SendMessageAsync("You win!");
                }

                else
                {
                    await Context.Channel.SendMessageAsync("Draw");
                }
            }
        }

        [Command("flip"), Summary("Challenge me to a coin flip!"), Remarks("(PREFIX)flip <heads/tails>"), Alias("coinflip", "coin", "coin-flip", "flipcoin", "flip-coin")]
        public async Task Flip(string choice = "")
        {
            if (choice.ToLower() != "heads" && choice.ToLower() != "tails" && choice.ToLower() != "")
            {
                await Context.Message.Channel.SendMessageAsync("Please enter a valid argument. Either heads or tails or leave it blank");
            }

            else
            {
                choice = choice.ToLower();
                string[] options = { "heads", "tails" };
                Random rand = new Random();
                int index = rand.Next(options.Length);

                if (string.IsNullOrWhiteSpace(choice))
                {
                    await Context.Message.Channel.SendMessageAsync(options[index]);
                }

                else
                {

                    if (options[index] == "heads" && choice == "tails")
                    {
                        await Context.Message.Channel.SendMessageAsync("I win!");
                    }

                    else if (options[index] == "heads" && choice == "heads")
                    {
                        await Context.Message.Channel.SendMessageAsync("You win!");
                    }

                    if (options[index] == "tails" && choice == "heads")
                    {
                        await Context.Message.Channel.SendMessageAsync("I win!");
                    }
                }
            }
        }

        [Command("ysearch", RunMode = RunMode.Async), Summary("Search YouTube for a specific keyword"), Remarks("(PREFIX)ysearch <query>")]
        public async Task SearchYouTube([Remainder] string args = "")
        {
            string searchFor = string.Empty;
            EmbedBuilder embed = new EmbedBuilder();
            string embedThumb = Context.User.GetAvatarUrl();
            StringBuilder sb = new StringBuilder();
            List<Google.Apis.YouTube.v3.Data.SearchResult> results = null;
            embed.ThumbnailUrl = embedThumb;

            if (string.IsNullOrEmpty(args))
            {
                embed.Title = $"No search term provided!";
                embed.WithColor(new Discord.Color(255, 0, 0));
                sb.AppendLine("Please provide a term to search for!");
                embed.Description = sb.ToString();
                await ReplyAsync("", false, embed.Build());
                return;
            }

            else
            {
                searchFor = args;
                embed.WithColor(new Color(0, 255, 0));
                results = await SearchChannelsAsync(searchFor);
            }

            if (results != null)
            {
                string videoUrlPrefix = $"https://www.youtube.com/watch?v=";
                embed.Title = $"YouTube Search For (**{searchFor}**)";
                Google.Apis.YouTube.v3.Data.SearchResult thumbFromVideo = results.Where(r => r.Id.Kind == "youtube#video").Take(1).FirstOrDefault();

                if (thumbFromVideo != null)
                {
                    embed.ThumbnailUrl = thumbFromVideo.Snippet.Thumbnails.Default__.Url;
                }

                foreach (var result in results.Where(r => r.Id.Kind == "youtube#video").Take(3))
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
                        fullVideoUrl = $"{videoUrlPrefix}{result.Id.VideoId.ToString()}";
                    }

                    sb.AppendLine($":video_camera: **__{result.Snippet.ChannelTitle}__** -> [**{result.Snippet.Title}**]({fullVideoUrl})\n\n *{description}*\n");
                }

                embed.Description = sb.ToString();
                await ReplyAsync("", false, embed.Build());
            }
        }

        private string getYouTubeApiRequest(string url)
        {
            string reponse = string.Empty;
            string fullUrl = $"https://www.googleapis.com/youtube/v3/search?key={Global.YouTubeAPIKey}{url}";
            Console.WriteLine($"YouTube API Request {fullUrl}");
            string response = string.Empty;
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders
                    .Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //test = httpClient.PostAsJsonAsync<FaceRequest>(fullUrl, request).Result;                             
                response = httpClient.GetStringAsync(url).Result;
            }
            return response;
        }

        public string getLatestVideoByID(string id, int numVideos = 1)
        {
            string videoURL = string.Empty;
            string url = $"&channelId={id}&part=snippet,id&order=date&maxResults={numVideos}";
            YouTubeModel.Video videos = JsonConvert.DeserializeObject<YouTubeModel.Video>(getYouTubeApiRequest(url));
            videoURL = $"https://www.youtube.com/watch?v={videos.items[0].id.videoId}";
            return videoURL;
        }

        public string getRandomVideoByID(string id, int numVideos = 50)
        {
            string videoURL = string.Empty;
            string url = $"&channelId={id}&part=snippet,id&order=date&maxResults={numVideos}";
            YouTubeModel.Video videos = JsonConvert.DeserializeObject<YouTubeModel.Video>(getYouTubeApiRequest(url));
            Random getRandom = new Random();
            int random = getRandom.Next(0, numVideos);
            videoURL = $"https://www.youtube.com/watch?v={videos.items[random].id.videoId}";
            return videoURL;
        }

        public async Task<List<Google.Apis.YouTube.v3.Data.SearchResult>> SearchChannelsAsync(string keyword = "space", int maxResults = 5)
        {
            YouTubeService youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Global.YouTubeAPIKey,
                ApplicationName = this.GetType().ToString()

            });
            SearchResource.ListRequest searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = keyword;
            searchListRequest.MaxResults = maxResults;
            Google.Apis.YouTube.v3.Data.SearchListResponse searchListResponse = await searchListRequest.ExecuteAsync();
            return searchListResponse.Items.ToList();
        }




        [Command("TranslateTo"), Summary("Translates the input text to the language you specify"), Remarks("(PREFIX)TranslateTo <language code> <text>"), Alias("Translate to", "Translate too")]
        public async Task TranslateTo(string toLanguage, [Remainder] string translate)
        {
            translate = Regex.Replace(translate, @"^\s*""?|""?\s*$", "");
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(translate)}";
            WebClient webClient = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            string result = webClient.DownloadString(url);

            try
            {
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);

                if (result == translate)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "Error in translation",
                        Description = $"There was an error translating {translate}. Did you use the right [language code](https://sites.google.com/site/opti365/translate_codes)?",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.Blue,
                    Description = $"{result}",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }

            catch
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "Error in translation",
                    Description = $"There was an error translating {translate}. Did you use the right [language code](https://sites.google.com/site/opti365/translate_codes)?",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("fact"), Summary("Gives you a random fact"), Remarks("(PREFIX)fact")]
        public async Task Fact()
        {
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync("https://uselessfacts.jsph.pl/random.json");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            resp = Regex.Replace(resp, @"[\[\]]", "");
            APIJsonItems APIData = JsonConvert.DeserializeObject<APIJsonItems>(resp);
            string URL = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={HttpUtility.UrlEncode(APIData.text)}";
            WebClient webClient = new WebClient
            {
                Encoding = Encoding.UTF8
            };
            string result = webClient.DownloadString(URL);
            result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
            await Context.Message.Channel.SendMessageAsync(result);
        }

        [Command("catfact"), Summary("Gets a random cat fact"), Remarks("(PREFIX)catfact")]
        public async Task catFact()
        {
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync("https://meowfacts.herokuapp.com/");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            resp = Regex.Replace(resp, @"[\[\]]", "");
            APIJsonItems APIData = JsonConvert.DeserializeObject<APIJsonItems>(resp);
            await Context.Message.Channel.SendMessageAsync(APIData.data);
        }

        [Command("trivia"), Summary("Gives you a random trivia question and censored out answer"), Remarks("(PREFIX)trivia")]
        public async Task Trivia()
        {
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync("http://jservice.io/api/random");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            resp = Regex.Replace(resp, @"[\[\]]", "");
            APIJsonItems APIData = JsonConvert.DeserializeObject<APIJsonItems>(resp);
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithTitle("Trivia question");
            eb.AddField($"__{APIData.question}?__", $"\n᲼᲼\n᲼᲼\n||{APIData.answer}||", false); // Empty spaces are just hidden from being shown on Discord, see https://www.quora.com/How-do-you-get-an-invisible-name-in-Discord#:~:text=There%20is%20no%20actual%20invisible,the%20character%20if%20you%20wish.
            eb.WithColor(Color.DarkPurple);
            eb.WithAuthor(Context.Message.Author);
            await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
        }

        public class APIJsonItems
        {
            public string answer { get; set; }
            public string question { get; set; }
            public string data { get; set; }
            public string text { get; set; }
        }

        [Command("leaderboard"), Summary("Gets the top 10 members in the leaderboard for the guild"), Remarks("(PREFIX)leaderboard")]
        public async Task GetLeaderboard()
        {
            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.LevelPath}");
            using SQLiteCommand cmd = new SQLiteCommand(conn);
            conn.Open();
            List<Dictionary<string, dynamic>> scores = new List<Dictionary<string, dynamic>>();
            cmd.CommandText = $"SELECT * FROM Levels WHERE guildId = '{Context.Guild.Id}' ORDER BY XP DESC LIMIT 10";
            using SQLiteDataReader reader = cmd.ExecuteReader();
            int count = 0;
            EmbedBuilder b = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Message.Author.ToString(),
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                },
                Title = $"Leaderboard for {Context.Guild}",
                Color = Color.Green,
            };

            while (reader.Read())
            {
                count++;

                if (count <= 10)
                {
                    Dictionary<string, dynamic> arr = new Dictionary<string, dynamic>();
                    string username = Context.Guild.GetUser(Convert.ToUInt64(reader.GetString(0))).Username;
                    arr.Add("name", username);
                    arr.Add("score", reader.GetInt64(4));
                    scores.Add(arr);
                }

                else
                {
                    break;
                }
            }

            conn.Close();
            Dictionary<string, List<Dictionary<string, dynamic>>> final_object = new Dictionary<string, List<Dictionary<string, dynamic>>>();
            final_object.Add("scores", scores);
            HttpClient HTTPClient = new HttpClient();
            var content = JsonConvert.SerializeObject(final_object);
            var buffer = Encoding.UTF8.GetBytes(content);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage HTTPResponse = await HTTPClient.PostAsync("http://thom.club:8080/format_leaderboard", new StringContent(JsonConvert.SerializeObject(final_object), Encoding.UTF8, "application/json"));
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            Dictionary<string, string> APIData = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp);
            b.WithCurrentTimestamp();
            b.Description = APIData["description"];
            await Context.Message.Channel.SendMessageAsync("", false, b.Build());
        }

        [Command("Rank"), Summary("Gets the rank for you or another user in a server"), Remarks("(PREFIX)rank (optional)<user>")]
        public async Task Rank(params string[] arg)
        {
            if (arg.Length == 0)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, GetRank(Context.Message.Author, Context.Guild.Id));
            }

            else
            {
                if (Context.Message.MentionedUsers.Any())
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, GetRank(Context.Message.MentionedUsers.First(), Context.Guild.Id));
                }
            }
        }

        public Embed GetRank(SocketUser user, ulong guild)
        {
            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.LevelPath}");
            using SQLiteCommand cmd = new SQLiteCommand(conn);
            conn.Open();
            cmd.CommandText = $"SELECT * FROM Levels WHERE guildId = '{guild}' AND userId = '{user.Id}'";
            using SQLiteDataReader reader = cmd.ExecuteReader();
            EmbedBuilder b = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Message.Author.ToString(),
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                },
                Title = $"Score for {user.Username}",
                Color = Color.Green,
            };

            while (reader.Read())
            {
                b.Description = $"**{user.Username} - __{reader.GetInt64(4)}__**\nLevel - {reader.GetInt64(3)}";
            }

            conn.Close();
            b.WithCurrentTimestamp();
            return b.Build();
        }

        [Command("snipe"), Summary("Gets the most recent deleted message from your guild"), Remarks("(PREFIX)snipe")]
        public async Task Snipe()
        {
            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.SnipeLogs}");
            using var cmd = new SQLiteCommand(conn);
            conn.Open();
            cmd.CommandText = $"SELECT * FROM SnipeLogs WHERE guildId = '{Context.Guild.Id}'";
            using SQLiteDataReader reader = cmd.ExecuteReader();
            EmbedBuilder b = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Message.Author.ToString(),
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                },
                Color = Color.Green,
            };

            while (reader.Read())
            {
                b.Title = reader.GetString(0);
                b.WithFooter($"{Global.UnixTimeStampToDateTime(reader.GetInt64(1))}");
            }

            conn.Close();
            await Context.Message.Channel.SendMessageAsync("", false, b.Build());
        }

        [Command("stats"), Summary("Gets the server stats in a fancy graph"), Remarks("(PREFIX)stats <pie, bar, line, doughnut, polararea>")]
        public async Task stats(params string[] graph)
        {
            if (graph.Length == 0)
            {
                EmbedBuilder noOp = new EmbedBuilder();
                noOp.WithTitle("Error");
                noOp.WithDescription("Please enter an option!");
                noOp.WithColor(Color.Red);
                noOp.WithAuthor(Context.Message.Author);
                await Context.Channel.SendMessageAsync("", false, noOp.Build());
                return;
            }

            else if (graph.Length == 1)
            {
                SQLiteConnection conn = new SQLiteConnection($"data source = {Global.LevelPath}");
                using SQLiteCommand cmd = new SQLiteCommand(conn);
                conn.Open();
                cmd.CommandText = $"SELECT * FROM Levels WHERE guildId = '{Context.Guild.Id}' ORDER BY XP DESC LIMIT 10";
                using SQLiteDataReader reader = cmd.ExecuteReader();
                int count = 0;
                string Username = "labels: [";
                string Data = "data: [";

                while (reader.Read())
                {
                    count++;

                    if (count <= 10)
                    {
                        Username += $"'{Context.Guild.GetUser(Convert.ToUInt64(reader.GetString(0))).Username}', ";
                        Data += $"{reader.GetInt64(4)}, ";
                    }

                    else
                    {
                        break;
                    }
                }

                conn.Close();
                Chart qc = new Chart();
                qc.Width = 500;
                qc.Height = 300;
                Username = Username.Remove(Username.LastIndexOf(','));
                Username += "]";
                Data = Data.Remove(Data.LastIndexOf(','));
                Data += "]";

                switch (graph[0].ToLower())
                {
                    case "pie":
                        qc.Config = $"{{type: 'pie', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        break;

                    case "bar":
                        qc.Config = $"{{type: 'bar', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        break;

                    case "line":
                        qc.Config = $"{{type: 'line', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        break;

                    case "doughnut":
                        qc.Config = $"{{type: 'doughnut', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        break;

                    case "polararea":
                        qc.Config = $"{{type: 'polarArea', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        break;
                    default:
                        await ReplyAsync("Please only enter \"pie\", \"bar\", \"line\", \"doughnut\" or \"polararea\"");
                        break;
                }

                await ReplyAsync(qc.GetUrl());
            }
        }

        [Command("dadjoke"), Summary("Gets a random dad joke"), Remarks("(PREFIX)dadjoke") Alias("badjoke")]
        public async Task test()
        {
            var client = new DadJokeClient("ICanHazDadJoke.NET Readme", "https://github.com/mattleibow/ICanHazDadJoke.NET");
            string dadJoke = await client.GetRandomJokeStringAsync();
            await Context.Message.Channel.SendMessageAsync(dadJoke);
        }

        //This is just boilerplate code so my bot A.) doesn't complain when I execute functions from the Python module and b.) to include it in the help command
        [Command("audit"), Summary("Gets audit log info on user/channel/guild"), Remarks("(PREFIX)audit [roles] <user> | (PREFIX)audit [overrides] <channel>")]
        public async Task audit(params string[] args)
        {
            return;
        }
    }
}