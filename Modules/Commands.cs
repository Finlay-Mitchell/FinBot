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
using QuickChart;
using ICanHazDadJoke.NET;
using System.Data;
using MySql.Data.MySqlClient;
using Google.Apis.YouTube.v3.Data;
using MongoDB.Driver;
using MongoDB.Bson;
using UptimeSharp;
using UptimeSharp.Models;

namespace FinBot.Modules
{
    public class Commands : ModuleBase<ShardedCommandContext>
    {
        [Command("reddit"), Summary("Shows a post from the selected subreddit"), Remarks("(PREFIX)reddit <subreddit>"), Alias("r")]
        public async Task Reddit([Remainder] string subreddit)
        {
            subreddit = subreddit.Replace(" ", "");
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
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }
                .WithColor(221, 65, 36)
                .Build());

                return;
            }

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
            b.AddField("Post info", $"{post.Data.Ups} upvotes\nurl: https://reddit.com/{post.Data.Permalink}\nCreated at: {Global.UnixTimeStampToDateTime(post.Data.Created)}");
            b.WithCurrentTimestamp();
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.ReplyAsync("", false, b.Build());
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
                    Regex r = new Regex("value=\"(.*?)\" aria-label=\"Search\"");

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
                    }

                    tp.Dispose();
                }

                else
                {
                    await Context.Message.ReplyAsync("invalid URL.");
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
                Regex r = new Regex("value=\"(.*?)\" aria-label=\"Search\"");

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
                }

                tp.Dispose();
            }

            else
            {
                if (arg.Length <= 1)
                {
                    await Context.Message.ReplyAsync("There's nothing to guess. Please provide an image");
                }

                else
                {
                    await Context.Message.ReplyAsync("There's nothing to guess. Please only provide one parameter.");
                }
            }
        }

        [Command("ping"), Summary("gives you a ping to the Discord API"), Remarks("(PREFIX)ping")]
        public async Task Ping()
        {
            DateTime before = DateTime.Now;
            await Context.Channel.TriggerTypingAsync();
            RestUserMessage message = (RestUserMessage)await Context.Message.ReplyAsync("Pong!");
            DateTime after = DateTime.Now;
            ulong snowflake = (ulong)Math.Round((after - before).TotalSeconds * 1000);
            ulong Heartbeat = (ulong)Math.Round((double)Context.Client.Latency);
            ulong totalLatency = (ulong)Math.Round((message.CreatedAt - Context.Message.CreatedAt).TotalSeconds * 1000);
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = $"Pong!";
            eb.AddField("Ping to discord", $"{Math.Floor((double)snowflake / 2)}ms");
            eb.AddField("Heartbeat(Me -> Discord -> Me)", $"{Heartbeat}ms");
            eb.AddField("Total time(Your message -> my reply)", $"{totalLatency}ms");
            eb.WithCurrentTimestamp();
            eb.WithAuthor(Context.Message.Author);

            if (totalLatency <= 100)
            {
                eb.Color = Color.Green;
            }

            else if (totalLatency <= 250)
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

        [Command("say"), Summary("repeats the text you enter to it"), Remarks("(PREFIX)say <text>"), Alias("echo", "repeat", "reply")]
        public async Task Echo([Remainder] string echo)
        {
            IDisposable tp = Context.Channel.EnterTypingState();

            if (Context.Message.MentionedUsers.Any() || Context.Message.MentionedRoles.Any() || Context.Message.MentionedEveryone)
            {
                await Context.Message.Channel.SendMessageAsync("Sorry, but you can't mention people");
                tp.Dispose();
                return;
            }

            if (echo.Length != 0)
            {
                await Context.Channel.SendMessageAsync(SayText(string.Join(' ', echo), Context));
                tp.Dispose();
                return;
            }

            else
            {
                await Context.Message.ReplyAsync($"What do you want me to say? please do {await Global.DeterminePrefix(Context)}say <msg>.");
                tp.Dispose();
                return;
            }
        }

        public string SayText(string text, SocketCommandContext context)
        {
            string final = text.ToLower();
            final = Regex.Replace(final, $"([{Global.DeterminePrefix(context).Result}-@])", "");
            final = Regex.Replace(final, @"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", "");

            if (string.IsNullOrEmpty(final) || string.IsNullOrWhiteSpace(final))
            {
                final = "Whoopsie daisy, my filter has filtered your text and it's returned an empty string, try again, with more sufficient text.";
            }

            return final;


            /*
             * 
             * REDESIGN THE CHAT FILTRATION SYSTEM
             * 
             */

            //string final = text.ToLower();
            //string finalcompare = final;
            //string[] leetWords = File.ReadAllLines(Global.CensoredWordsPath);
            //Dictionary<string, string> leetRules = Global.LoadLeetRules();
            //Regex re = new Regex(@"\b(" + string.Join("|", leetWords.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            //final = Regex.Replace(final, $"([{Global.Prefix}-])", "");
            //finalcompare = Regex.Replace(final, @"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?", "");
            //final = Regex.Replace(final, "@", "");

            //foreach (KeyValuePair<string, string> x in leetRules)
            //{
            //    finalcompare = final.Replace(x.Key, x.Value);
            //}

            //final = final.ToLower();
            //finalcompare = final;
            //final = Regex.Replace(final, re.ToString(), "");

            //if(re.IsMatch(finalcompare))
            //{
            //    return "Nice try";
            //}

            //if (string.IsNullOrEmpty(final) || string.IsNullOrWhiteSpace(final))
            //{
            //    final = "Whoopsie daisy, my filter has filtered your text and it's returned an empty string, try again, with more sufficient text.";
            //}





            //  return final;

        }

        [Command("userinfo"), Summary("shows information on a user"), Remarks("(PREFIX)userinfo || (PREFIX)userinfo <user>."), Alias("whois", "user", "info", "user-info")]
        public async Task Userinfo(params string[] arg)
        {
            if (arg.Length == 0)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, UserInfo(Context.Message.Author));
            }

            else
            {
                if (Context.Message.MentionedUsers.Any())
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, UserInfo(Context.Message.MentionedUsers.First()));
                }

                else if (arg[0].Length == 17 || arg[0].Length == 18)
                {
                    SocketUser user = Context.Guild.GetUser(Convert.ToUInt64(arg[0]));
                    await Context.Message.ReplyAsync("", false, await GetRankAsync(user, Context.Guild.Id));
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
            eb.AddField("Active clients", String.IsNullOrEmpty(String.Join(separator: ", ", values: user.ActiveClients.ToList().Select(r => r.ToString()))) || user.IsBot ? ClientError : String.Join(separator: ", ", values: user.ActiveClients.ToList().Select(r => r.ToString())));
            eb.AddField("Created at UTC", user.CreatedAt.UtcDateTime.ToString("r"));
            eb.AddField("Joined at UTC?", SGU.JoinedAt.HasValue ? SGU.JoinedAt.Value.UtcDateTime.ToString("r") : "No value :/");
            eb.AddField($"Roles: [{SGU.Roles.Count}]", $"<@&{String.Join(separator: ">, <@&", values: SGU.Roles.Select(r => r.Id))}>");
            _ = eb.AddField($"Permissions: [{SGU.GuildPermissions.ToList().Count}]", $"{string.Join(separator: ", ", values: SGU.GuildPermissions.ToList().Select(r => r.ToString()))}");
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
            string boosttier = "";

            switch (Context.Guild.PremiumTier.ToString())
            {
                case "Tier1":
                    boosttier = "Tier 1";
                    break;

                case "Tier2":
                    boosttier = "Tier 2";
                    break;

                case "Tier3":
                    boosttier = "Tier 3";
                    break;
            }

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
            eb.AddField($"VIP perks [{Context.Guild.Features.Count}]", String.IsNullOrEmpty(String.Join(separator: ", ", values: Context.Guild.Features.ToList().Select(r => r.ToString())).ToLower()) ? "None" : String.Join(separator: ", ", values: Context.Guild.Features.ToList().Select(r => r.ToString())).ToLower().Replace("_", " "), true);
            eb.WithCurrentTimestamp();
            eb.WithColor(Color.Blue);
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("botInfo"), Summary("shows some basic bot information"), Remarks("(PREFIX)botinfo")]
        public async Task BotInfo()
        {
            await Context.Channel.TriggerTypingAsync();
            EmbedBuilder eb = new EmbedBuilder();
            eb.AddField("Developers:", "Finlay Mitchell, Thomas Waffles");
            eb.AddField("Version: ", Global.Version);
            eb.AddField("Languages", "C# - Discord.net API\nPython - Discord.py");
            eb.WithAuthor(Context.Message.Author);
            eb.WithColor(Color.Gold);
            eb.WithTitle("Bot info");
            eb.AddField("Uptime", $"{(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}");
            eb.AddField("Runtime", $"{RuntimeInformation.FrameworkDescription} {RuntimeInformation.OSArchitecture}");
            eb.AddField($"Heap size", $"{Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString()} MB");
            eb.AddField("How many servers am I in?", Context.Client.Guilds.Count());
            eb.AddField("Invite to your server", "[Invite link](http://bot.finlaymitchell.ml)");
            eb.AddField("Join the support server", "[here](http://server.finlaymitchell.ml)");
            eb.WithDescription($"Here's some info on me");
            eb.WithCurrentTimestamp();
            eb.WithDescription("To support the developers, [please feel free to donate](http://donate.finlaymitchell.ml)!");
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("8ball"), Summary("with 8ball magic, ask it a question and it will respond to you"), Remarks("(PREFIX)8ball <question>"), Alias("eightball", "ask")]
        public async Task EightBall([Remainder] string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("Please enter a parameter");
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                string[] answers = { "As I see it, yes.", "Ask again later.", "It is certain.", "It is decidedly so.", "Don't count on it.", "Better not tell you now.", "Concentrate and ask again.", " Cannot predict now.",
                "Most likely.", "My reply is no", "Yes.", "You may rely on it.", "Yes - definitely.", "Very doubtful.", "Without a doubt.", " My sources say no.", " Outlook not so good.", "Outlook good.", "Reply hazy, try again",
                "Signs point to yes"};
                Random rand = new Random();
                int index = rand.Next(answers.Length);
                await Context.Message.ReplyAsync(answers[index]);
            }
        }

        [Command("topic"), Summary("sends a random conversation starter"), Remarks("(PREFIX)topic")]
        public async Task Topic()
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
            Random r = new Random();
            int ans = r.Next(0, num);
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.ReplyAsync($"Number: {ans}");
        }

        [Command("av"), Summary("show users avatar"), Remarks("(PREFIX)av <user>"), Alias("Avatar", "profile")]
        public async Task AvAsync(params string[] arg)
        {
            if (arg.Length == 0)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, AV(Context.Message.Author));
            }

            else
            {
                if (Context.Message.MentionedUsers.Any())
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, AV(Context.Message.MentionedUsers.First()));
                }

                else if (arg[0].Length == 17 || arg[0].Length == 18)
                {
                    SocketUser user = Context.Guild.GetUser(Convert.ToUInt64(arg[0]));
                    await Context.Message.ReplyAsync("", false, await GetRankAsync(user, Context.Guild.Id));
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

        /*
         * 
         * REDESIGN THIS FEATURE
         * 
         */

        //[Command("remind", RunMode = RunMode.Async), Summary("Reminds you with a custom message (In Seconds)"), Remarks("(PREFIX)remain <seconds> <message>"), Alias("Timer")]
        //public async Task Remind(string duration, [Remainder] string remindMsg)
        //{
        //    if (remindMsg.Contains("@everyone") || remindMsg.Contains("@here"))
        //    {
        //        await Context.Channel.TriggerTypingAsync();
        //        await Context.Message.ReplyAsync($"Sorry but can't mention everybody");
        //    }

        //    else if (Context.Message.MentionedUsers.Any())
        //    {
        //        await Context.Channel.TriggerTypingAsync();
        //        await Context.Message.ReplyAsync($"Sorry but you can't mention users");
        //    }

        //    else if (Context.Message.MentionedRoles.Any())
        //    {
        //        await Context.Channel.TriggerTypingAsync();
        //        await Context.Message.ReplyAsync($"Sorry but you can't mention roles");
        //    }

        //    else
        //    {
        //        await Context.Channel.TriggerTypingAsync();
        //        await ReminderService.setReminder(Context.Guild, Context.User, (SocketTextChannel)Context.Channel, DateTime.Now, duration, remindMsg);
        //    }
        //}

        [Command("embed"), Summary("Displays your message in an embed message"), Remarks("(PREFIX)embed <title>, <description>"), Alias("embedmessage")]
        public async Task CmdEmbedMessage([Remainder] string text = "")
        {
            if (text.Contains(","))
            {
                string[] result = text.Split(',');
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, EmbedMessage(result[0], result[1]).Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, EmbedMessage(text).Build());
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

        [Command("translate"), Summary("Translates inputted text to English"), Remarks("(PREFIX)translate <text>"), Alias("t", "trans")]
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
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
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
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
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
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
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
                await Context.Message.ReplyAsync("A search needs to be entered! E.G: `wiki testing`");
                return;
            }

            await WikiSearch(search, Context.Channel, Context.Message);
        }

        private async Task WikiSearch(string search, ISocketMessageChannel channel, SocketUserMessage msg, int maxSearch = 10)
        {
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
                await Context.Message.ReplyAsync("", false, eb.Build());
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
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        [Command("rps"), Summary("Play rock paper sciessors with the bot"), Remarks("(PREFIX)rps <rock/paper/scissors>"), Alias("rockpaperscissors", "rock-paper-scissors")]
        public async Task RPS(string choice)
        {
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
                await Context.Message.ReplyAsync("", false, embed.Build());
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
                Google.Apis.YouTube.v3.Data.SearchResult thumbFromVideo = results.Where(r => r.Id.Kind == "youtube#video").Take(1).FirstOrDefault();

                if (thumbFromVideo != null)
                {
                    embed.ThumbnailUrl = thumbFromVideo.Snippet.Thumbnails.Default__.Url;
                }

                foreach (Google.Apis.YouTube.v3.Data.SearchResult result in results.Where(r => r.Id.Kind == "youtube#video").Take(3))
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
                await Context.Message.ReplyAsync("", false, embed.Build());
            }
        }

        [Command("TranslateTo"), Summary("Translates the input text to the language you specify"), Remarks("(PREFIX)TranslateTo <language code> <text>"), Alias("trto", "tto")]
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
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
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
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
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
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
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
            await Context.Message.ReplyAsync(result);
        }

        [Command("catfact"), Summary("Gets a random cat fact"), Remarks("(PREFIX)catfact")]
        public async Task catFact()
        {
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync("https://meowfacts.herokuapp.com/");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            resp = Regex.Replace(resp, @"[\[\]]", "");
            APIJsonItems APIData = JsonConvert.DeserializeObject<APIJsonItems>(resp);
            await Context.Message.ReplyAsync(APIData.data);
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
            await Context.Message.ReplyAsync("", false, eb.Build());
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
            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);

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
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                        },
                        Title = $"Please enable levelling",
                        Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                        Color = Color.Orange,
                    };

                    await Context.Message.ReplyAsync("", false, eb.Build());
                    return;
                }

                else if (toLevel.ToLower() == "off")
                {
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                        },
                        Title = $"Please enable levelling",
                        Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                        Color = Color.Orange,
                    };

                    await Context.Message.ReplyAsync("", false, eb.Build());
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
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
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
                    Dictionary<string, List<Dictionary<string, dynamic>>> final_object = new Dictionary<string, List<Dictionary<string, dynamic>>>();
                    final_object.Add("scores", scores);
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

                catch (Exception)
                {
                    b.WithCurrentTimestamp();
                    b.Description = format + "```";
                    await Context.Message.ReplyAsync("", false, b.Build());
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

        [Command("Rank"), Summary("Gets the rank for you or another user in a server"), Remarks("(PREFIX)rank (optional)<user>"), Alias("level")]
        public async Task Rank(params string[] arg)
        {
            string toLevel = await Global.DetermineLevel(Context.Guild);

            if (toLevel.ToLower() == "false")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                    Color = Color.Orange,
                };

                await Context.Message.ReplyAsync("", false, b.Build());
                return;
            }

            else if (toLevel.ToLower() == "off")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                    Color = Color.Orange,
                };

                await Context.Message.ReplyAsync("", false, b.Build());
                return;
            }

            if (arg.Length == 0)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, await GetRankAsync(Context.Message.Author, Context.Guild.Id));
            }

            else
            {
                if (Context.Message.MentionedUsers.Any())
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, await GetRankAsync(Context.Message.MentionedUsers.First(), Context.Guild.Id));
                }

                else if (arg[0].Length == 17 || arg[0].Length == 18)
                {
                    SocketUser user = Context.Guild.GetUser(Convert.ToUInt64(arg[0]));
                    await Context.Message.ReplyAsync("", false, await GetRankAsync(user, Context.Guild.Id));
                }
            }
        }

        public Task<Embed> GetRankAsync(SocketUser user, ulong guild)
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);

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
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
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

        [Command("snipe"), Summary("Gets the most recent deleted message from your guild"), Remarks("(PREFIX)snipe")]
        public async Task Snipe()
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM snipelogs WHERE guildId = {Context.Guild.Id}", conn);
                MySqlDataReader reader = cmd.ExecuteReader();
                EmbedBuilder b = new EmbedBuilder();

                while (reader.Read())
                {
                    b.Title = "Sniped message";
                    b.Description = (string)reader[0];
                    b.WithFooter($"{Global.UnixTimeStampToDateTime(reader.GetDouble(1))}");
                    SocketGuildUser user = (SocketGuildUser)Context.Message.Author;
                    string username = "";
                    string avurl = "";
                    ulong uId = Convert.ToUInt64(reader[3]);

                    if (Context.Guild.GetUser(uId) == null || Context.Guild.GetUser(uId).GetType() == typeof(SocketUnknownUser))
                    {
                        username = $"<@{reader[3]}>";
                        avurl = "";
                    }

                    else
                    {
                        user = Context.Guild.GetUser(uId);
                        avurl = user.GetAvatarUrl();

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
                        IconUrl = avurl,
                    };
                    b.WithAuthor(Author);
                    b.Color = Color.Red;
                }

                conn.Close();
                await Context.Message.ReplyAsync("", false, b.Build());
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
                }
            }
        }

        [Command("stats"), Summary("Gets the server stats in a fancy graph"), Remarks("(PREFIX)stats <pie, bar, line, doughnut, polararea>")]
        public async Task stats(params string[] graph)
        {
            string toLevel = await Global.DetermineLevel(Context.Guild);

            if (toLevel.ToLower() == "false")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                    Color = Color.Orange,
                };

                await Context.Message.ReplyAsync("", false, b.Build());
                return;
            }

            else if (toLevel.ToLower() == "off")
            {
                EmbedBuilder b = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                    },
                    Title = $"Please enable levelling",
                    Description = $"Please enable levelling by using the {await Global.DeterminePrefix(Context)}enablelevelling <true/on> command!",
                    Color = Color.Orange,
                };

                await Context.Message.ReplyAsync("", false, b.Build());
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
                return;
            }

            else if (graph.Length == 1)
            {
                MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);

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
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            break;

                        case "bar":
                            qc.Config = $"{{type: 'bar', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            break;

                        case "line":
                            qc.Config = $"{{type: 'line', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            break;

                        case "doughnut":
                            qc.Config = $"{{type: 'doughnut', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            break;

                        case "polararea":
                            qc.Config = $"{{type: 'polarArea', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                            await Context.Message.ReplyAsync(qc.GetUrl());
                            break;
                        default:
                            await ReplyAsync("Please only enter \"pie\", \"bar\", \"line\", \"doughnut\" or \"polararea\"");
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
                    }
                }
            }
        }

        [Command("dadjoke"), Summary("Gets a random dad joke"), Remarks("(PREFIX)dadjoke"), Alias("badjoke")]
        public async Task dadjoke()
        {
            DadJokeClient client = new DadJokeClient("ICanHazDadJoke.NET Readme", "https://github.com/mattleibow/ICanHazDadJoke.NET");
            string dadJoke = await client.GetRandomJokeStringAsync();
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.ReplyAsync(dadJoke);
        }

        [Command("poll")]
        public async Task poll([Remainder] string question)
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);
            MySqlConnection queryConn = new MySqlConnection(Global.MySQL.connStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Polls WHERE guildId = {Context.Guild.Id} AND author = {Context.Message.Author.Id}", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                bool hasRan = false;

                while (reader.Read())
                {
                    hasRan = true;

                    if (reader.GetString(3) == "Active")
                    {
                        EmbedBuilder eb = new EmbedBuilder();
                        eb.Title = "Poll already active";
                        eb.Description = $"Your poll with ID {reader.GetInt64(0)} is already active, please close this poll by doing {await Global.DeterminePrefix(Context)}endpoll";
                        eb.WithAuthor(Context.Message.Author);
                        eb.WithCurrentTimestamp();
                        eb.Color = Color.Red;
                        await Context.Message.ReplyAsync("", false, eb.Build());
                        return;
                    }

                    else
                    {
                        EmbedBuilder eb = new EmbedBuilder();
                        eb.Title = $"{question}";
                        eb.WithAuthor(Context.Message.Author);
                        eb.WithFooter($"Poll active at {Context.Message.Timestamp}");
                        var msg = await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
                        await msg.AddReactionsAsync(Global.reactions.ToArray());
                        queryConn.Open();
                        MySqlCommand cmd1 = new MySqlCommand($"UPDATE Polls SET message = {msg.Id}, guildId = {Context.Guild.Id}, author = {Context.Message.Author.Id}, state = 'Active', chanId = {Context.Message.Channel.Id} WHERE guildId = {Context.Guild.Id} AND author = {Context.Message.Author.Id}", queryConn);
                        cmd1.ExecuteNonQuery();
                        queryConn.Close();
                    }
                }

                conn.Close();

                if (!hasRan)
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Title = $"{question}";
                    eb.WithAuthor(Context.Message.Author);
                    eb.WithFooter($"Poll active at {Context.Message.Timestamp}");
                    var msg = await Context.Message.Channel.SendMessageAsync("", false, eb.Build());
                    await msg.AddReactionsAsync(Global.reactions.ToArray());
                    queryConn.Open();
                    MySqlCommand cmd1 = new MySqlCommand($"INSERT INTO Polls(message, guildId, author, state, chanId) VALUES ({msg.Id}, {Context.Guild.Id}, {Context.Message.Author.Id}, 'Active', {Context.Message.Channel.Id})", queryConn);
                    cmd1.ExecuteNonQuery();
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
                }
            }
        }

        [Command("endpoll")]
        public async Task endpoll()
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);
            MySqlConnection queryConn = new MySqlConnection(Global.MySQL.connStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM Polls WHERE guildId = {Context.Guild.Id} AND author = {Context.Message.Author.Id}", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                bool hasRan = false;

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
                            await ModifyMessage(message, eb);
                            queryConn.Open();
                            MySqlCommand cmd1 = new MySqlCommand($"UPDATE Polls SET state = 'Inactive' WHERE guildId = {Context.Guild.Id} AND author = {Context.Message.Author.Id}", queryConn);
                            cmd1.ExecuteNonQuery();
                            queryConn.Close();
                        }

                        catch (Exception ex)
                        {
                            await Context.Message.ReplyAsync($"Error: {ex.Message}");
                            queryConn.Open();
                            MySqlCommand cmd1 = new MySqlCommand($"UPDATE Polls SET state = 'Inactive' WHERE guildId = {Context.Guild.Id} AND author = {Context.Message.Author.Id}", queryConn);
                            cmd1.ExecuteNonQuery();
                            queryConn.Close();
                        }

                        finally
                        {
                            queryConn.Open();
                        }
                    }

                    else
                    {
                        EmbedBuilder eb = new EmbedBuilder();
                        eb.Title = "Poll not active";
                        eb.Description = $"You currently do not have any active polls. You can initiate one by using the {await Global.DeterminePrefix(Context)}poll command";
                        eb.WithAuthor(Context.Message.Author);
                        eb.WithCurrentTimestamp();
                        eb.Color = Color.Red;
                        await Context.Message.ReplyAsync("", false, eb.Build());
                    }
                }

                conn.Close();

                if (!hasRan)
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.Title = "Poll not active";
                    eb.Description = $"You currently do not have any active polls. You can initiate one by using the {await Global.DeterminePrefix(Context)}poll command";
                    eb.WithAuthor(Context.Message.Author);
                    eb.WithCurrentTimestamp();
                    eb.Color = Color.Red;
                    await Context.Message.ReplyAsync("", false, eb.Build());
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
                }
            }
        }

        [Command("uptime"), Summary("Gets the percentage of uptime for each of the bot modules"), Remarks("(PREFIX)uptime"), Alias("status")]
        public async Task uptime()
        {
            UptimeClient _client = new UptimeClient(Global.StatusPageAPIKey);
            List<Monitor> monitors = await _client.GetMonitors();

            EmbedBuilder eb = new EmbedBuilder();
            monitors.ForEach(item => eb.AddField(item.Name, $"Status: {item.Status}\nUptime: {item.Uptime}%"));
            eb.Title = "Bot status";
            eb.Author = new EmbedAuthorBuilder()
            {
                Name = Context.Message.Author.ToString(),
                IconUrl = Context.Message.Author.GetAvatarUrl(),
                Url = Context.Message.GetJumpUrl()
            };
            eb.Description = "[View status page](https://status.finlaymitchell.ml)";
            eb.WithCurrentTimestamp();
            eb.WithFooter("Via UptimeRobot");
            eb.Color = Color.Green;
            await Context.Message.ReplyAsync("", false, eb.Build());
        }

        /*
         * 
         * BOILERPLACE CODE FOR PYTHON MODULE 
         * 
         */

        [Command("chatbot"), Summary("ALlows you to interact with the AI chatbot"), Remarks("(PREFIX)chatbot")]
        public Task chatbot(params string[] arg)
        {
            return Task.CompletedTask;
        }
    }
}