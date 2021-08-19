using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using System.Linq;
using FinBot.Handlers;
using Discord;

namespace FinBot.Services
{
    public class TwitchService : ModuleBase<ShardedCommandContext>
    {
        public static DiscordShardedClient _client;
        public static int callTotal = 0;

        public TwitchService(IServiceProvider services)
        {
            callTotal += 1;
            _client = services.GetRequiredService<DiscordShardedClient>();

            if (callTotal == 1)
            {
                CheckLiveUsers();
            }
        }

        public async void CheckLiveUsers()
        {
            MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
            IMongoDatabase database = mongoClient.GetDatabase("finlay");
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
            ulong _id = 0;
            EmbedBuilder eb = new EmbedBuilder();
            List<TwitchHandler.TwitchData> userInfo;
            Color TwitchColour = new Color(100, 65, 165);
            eb.Color = TwitchColour;
            string itemVal = "";
            BsonDocument item = null;
            List<string> stringArray = new List<string>();
            List<TwitchHandler.UserStreams> userStreams = new List<TwitchHandler.UserStreams>();
            string modlogchannel = "";
            SocketTextChannel logchannel;
            List<string> AlreadySent = new List<string>();
            string listUser = "";

            while (true)
            {
                foreach (SocketGuild guild in _client.Guilds)
                {
                    _id = guild.Id;
                    item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();

                    try
                    {
                        itemVal = item?.GetValue("TwitchUsers").ToJson();
                    }

                    catch { continue; }

                    if (itemVal != null)
                    {
                        stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();

                        foreach (string user in stringArray)
                        {
                            userStreams = await TwitchHandler.GetStreams(user);
                            listUser = $"{_id}_{user}";

                            if (userStreams.Count == 0)
                            {
                                if (AlreadySent.Contains(listUser))
                                {
                                    AlreadySent.Remove(listUser);
                                }

                                continue;
                            }

                            if (AlreadySent.Contains(listUser))
                            {
                                continue;
                            }

                            modlogchannel = await TwitchHandler.GetTwitchChannel(guild);

                            if (modlogchannel == "0")
                            {
                                continue;
                            }

                            logchannel = guild.GetTextChannel(Convert.ToUInt64(modlogchannel));
                            userInfo = await TwitchHandler.GetTwitchInfo(user);
                            eb.Title = $"{user} is live on Twitch!";
                            eb.ImageUrl = $"https://static-cdn.jtvnw.net/previews-ttv/live_user_{user}.jpg";
                            eb.Description = $"[Watch {user} live on Twitch!](https://twitch.tv/{user})";
                            eb.AddField("Stream information", $"title: {userStreams[0].title}\ngame name: {userStreams[0].game_name}");
                            eb.Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = userInfo[0].profile_image_url,
                                Text = $"Live started at: {userStreams[0].started_at}"
                            };
                            AlreadySent.Add(listUser);
                            await logchannel.SendMessageAsync("", false, eb.Build());
                        }
                    }
                }
            }
        }
    }
}
