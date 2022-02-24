using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using FinBot.Attributes.Interactivity.Preconditions;
using FinBot.Handlers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using QuickChart;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FinBot.Modules.InteractivityCommands
{
    [Group("stats", "Commands relating to statistics.")]
    public class StatCommands : InteractionModuleBase<ShardedInteractionContext>
    {
        public InteractionService _commands { get; set; }
        private CommandHandler _handler;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public StatCommands(IServiceProvider services)
        {
            _handler = services.GetRequiredService<CommandHandler>();
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
                string username = string.Empty;
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
                        spaces = string.Empty;

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

        [SlashCommand("rank", "Gets the rank for a member in the server.")]
        [UserCommand("rank")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Rank([Summary(name: "User", description: "User to get the rank for - optional.")] SocketUser user = null)
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

        [SlashCommand("graph", "Gets the server user message stats in graph form.")]
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
                await Context.Interaction.DeferAsync();

                switch (graph)
                {
                    case chartTypes.pie:
                        qc.Config = $"{{type: 'pie', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.FollowupWithFileAsync(ms, $"guild_stats_pie-{Global.GenerateRandom()}.png");
                        tp.Dispose();
                        break;

                    case chartTypes.bar:
                        qc.Config = $"{{type: 'bar', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.FollowupWithFileAsync(ms, $"guild_stats_bar-{Global.GenerateRandom()}.png");
                        tp.Dispose();
                        break;

                    case chartTypes.line:
                        qc.Config = $"{{type: 'line', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.FollowupWithFileAsync(ms, $"guild_stats_line-{Global.GenerateRandom()}.png");
                        tp.Dispose();
                        break;

                    case chartTypes.dougnut:
                        qc.Config = $"{{type: 'doughnut', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.FollowupWithFileAsync(ms, $"guild_stats_doughnut-{Global.GenerateRandom()}.png");
                        tp.Dispose();
                        break;

                    case chartTypes.polararea:
                        qc.Config = $"{{type: 'polarArea', data: {{ {Username}, datasets: [{{ label: 'Leaderboard stats for {Context.Guild}', {Data} }}] }}, options: {{ plugins: {{ datalabels: {{ color: '#000000' }} }} }} }}";
                        bytes = wc.DownloadData(qc.GetUrl());
                        ms = new MemoryStream(bytes);
                        await Context.Interaction.FollowupWithFileAsync(ms, $"guild_stats_polararea-{Global.GenerateRandom()}.png");
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

        [SlashCommand("count", "Gets number of messages with the selected content inside.")]
        public async Task Count([Summary(name: "Phrase", description: "The phrase to search for.")] string phrase, [Summary(name: "User", description: "Specify a user to count the messages from - optional.")] SocketUser user = null)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = "Message search!";
            eb.Description = (user == null) ? $"Getting number of messages with phrase \"{phrase}\" in it! This may take some time." : $"Getting number of messages with phrase \"{phrase}\" in it from {user.Username}! This may take some time.";
            eb.Author = new EmbedAuthorBuilder()
            {
                Name = Context.User.Username,
                IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl()
            };
            eb.Color = Color.Orange;
            eb.WithCurrentTimestamp();
            await Context.Interaction.RespondAsync("", embed: eb.Build());
            IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
            //long count = (user == null) ? messages.CountDocuments(new BsonDocument { { "guildId", $"{Context.Guild.Id}" }, { "content", new Regex($".*{phrase.ToLower()}*.") } }) 
            //    : messages.CountDocuments(new BsonDocument { { "guildId", $"{Context.Guild.Id}" }, { "discordId", user.Id.ToString() }, { "content", new Regex($".*{phrase.ToLower()}*.") } });

            long count = (user == null) ? messages.CountDocuments(new BsonDocument { { "$text", new BsonDocument { { "$search", phrase.ToLower() } } }, { "guildId", $"{Context.Guild.Id}" } }) :
                messages.CountDocuments(new BsonDocument { { "$text", new BsonDocument { { "$search", phrase.ToLower() } } }, { "guildId", $"{Context.Guild.Id}" }, { "discordId", user.Id.ToString() } });
            //    }
            //});

            eb.Description = (user == null) ? $"The phrase \"{phrase}\" has been said {count} times!" : $"{user.Username} has said the phrase \"{phrase}\" {count} times!";
            eb.Color = Color.Green;
            eb.WithFooter("If you wish to search for a phrase, please encapsulate your selected phrase with straight double quotes(\"\").");

            BsonDocument mostRecentLegalMessage = await messages.Find(new BsonDocument { { "$text", new BsonDocument { { "$search", phrase.ToLower() } } }, { "deleted", false }, 
                { "guildId", $"{Context.Guild.Id}" } }).Sort(new BsonDocument { { "createdTimestamp", -1 } }).FirstOrDefaultAsync();
            
            if (mostRecentLegalMessage != null)
            {
                eb.AddField("_ _", $"[The previous message which matches your input](https://discord.com/channels/{Context.Guild.Id}/{mostRecentLegalMessage.GetValue("channelId")}/{mostRecentLegalMessage.GetValue("_id")})");
            }
            
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Content = string.Empty;
                x.Embed = eb.Build();
            });
        }

        [RequireDeveloper]
        [SlashCommand("test", "purely a test")]
        public async Task teset(SocketUser user = null)
        {
            await Context.Interaction.DeferAsync();

            if(user == null)
            {
                user = Context.User;
            }

                IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
                string data = $"[";
            try
            {
                //IFindFluent<BsonDocument, BsonDocument> t = messages.Find(new BsonDocument { { "guildId", $"{Context.Guild.Id}" }, { "discordId", $"{user.Id}" } });//.Sort(new BsonDocument { { "createdTimestamp", 1 } });
                var t = messages.Find(new BsonDocument { { "delted", false }, { "guildId", $"{Context.Guild.Id}" }, { "discordId", $"{user.Id}" } });//.Sort(new BsonDocument { { "createdTimestamp", 1 } });
                Global.ConsoleLog(t.Count().ToString());
                List<uint> test = new List<uint>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

                foreach (BsonDocument document in t.ToList())
                {
                    Global.ConsoleLog(document.ToString());

                    switch (Math.Floor((decimal)((Convert.ToUInt64(document.GetValue("createdTimestamp")) * 1000) / (1000 * 3600)) % 24))
                    {
                        case 1:
                            test[0] += 1;
                            break;

                        case 2:
                            test[1] += 1;
                            break;

                        case 3:
                            test[2] += 1;
                            break;

                        case 4:
                            test[3] += 1;
                            break;

                        case 5:
                            test[4] += 1;
                            break;

                        case 6:
                            test[5] += 1;
                            break;

                        case 7:
                            test[6] += 1;
                            break;

                        case 8:
                            test[7] += 1;
                            break;

                        case 9:
                            test[8] += 1;
                            break;

                        case 10:
                            test[9] += 1;
                            break;

                        case 11:
                            test[10] += 1;
                            break;

                        case 12:
                            test[11] += 1;
                            break;

                        case 13:
                            test[12] += 1;
                            break;

                        case 14:
                            test[13] += 1;
                            break;

                        case 15:
                            test[14] += 1;
                            break;

                        case 16:
                            test[15] += 1;
                            break;

                        case 17:
                            test[16] += 1;
                            break;

                        case 18:
                            test[17] += 1;
                            break;

                        case 19:
                            test[18] += 1;
                            break;

                        case 20:
                            test[19] += 1;
                            break;

                        case 21:
                            test[20] += 1;
                            break;

                        case 22:
                            test[21] += 1;
                            break;

                        case 23:
                            test[22] += 1;
                            break;

                        case 24:
                            test[23] += 1;
                            break;

                        default:
                            Global.ConsoleLog(Math.Floor((decimal)((Convert.ToUInt64(document.GetValue("createdTimestamp")) * 1000) / (1000 * 3600)) % 24).ToString());
                            break;
                    }
                }

                foreach (uint elem in test)
                {
                    data += $"{elem}, ";
                }
            }

            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }

            data = data.Substring(0, data.Length - 2);
            Chart qc = new Chart
            {
                Width = 500,
                Height = 300
            };
            WebClient wc = new WebClient();
            qc.Config = $"{{type: 'line', data: {{ labels:['01:00', '02:00', '03:00', '04:00', '05:00', '06:00', '07:00', '08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00', '19:00', '20:00', " +
                $"'21:00', '22:00', '23:00', '00:00'],  datasets: [{{ label: 'Activity graph for {user.Username}', data: {data}], fill: false }}] }}, options: {{ plugins: {{ datalabels: {{ display: true, " +
                $"align: 'bottom', backgroundColor: '#ccc', borderRadius: 3 }} }} }} }}";
            byte[] bytes = wc.DownloadData(qc.GetUrl());
            Global.ConsoleLog(qc.GetUrl());
            MemoryStream ms = new MemoryStream(bytes);
            await Context.Interaction.FollowupWithFileAsync(ms, $"user_activity_graph-{user.Id}-{Global.GenerateRandom()}.png");
        }

        //[SlashCommand("newtest", "hmm")]
        public async Task cmdtest(SocketUser user = null)
        {
            await Context.Interaction.DeferAsync();

            if (user == null)
            {
                user = Context.User;
            }

            IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
            string data = $"[";

            try
            {
                long t = 0;

                for (int i = 0; i < 24; i++)
                {
                    //t = messages.CountDocuments(new BsonDocument { { "discordId", $"{user.Id}" }, { "guildId", $"725886999646437407" }, { (Math.Floor((decimal)((Convert.ToUInt64("createdTimestamp") * 1000) / (1000 * 3600)) % 24)).ToString(), $"{i}" } });

                    data += $"{t}, ";

                    t = 0;
                }
            }

            catch(Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }


            data = data.Substring(0, data.Length - 2);
            Chart qc = new Chart
            {
                Width = 500,
                Height = 300
            };
            WebClient wc = new WebClient();
            qc.Config = $"{{type: 'line', data: {{ labels:['01:00', '02:00', '03:00', '04:00', '05:00', '06:00', '07:00', '08:00', '09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00', '19:00', '20:00', " +
                $"'21:00', '22:00', '23:00', '00:00'],  datasets: [{{ label: 'Activity graph for {user.Username}', data: {data}], fill: false }}] }}, options: {{ plugins: {{ datalabels: {{ display: true, " +
                $"align: 'bottom', backgroundColor: '#ccc', borderRadius: 3 }} }} }} }}";
            byte[] bytes = wc.DownloadData(qc.GetUrl());
            Global.ConsoleLog(qc.GetUrl());
            MemoryStream ms = new MemoryStream(bytes);
            await Context.Interaction.FollowupWithFileAsync(ms, $"user_activity_graph-{user.Id}-{Global.GenerateRandom()}.png");
        }
    }
}
