using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FinBot.Modules;

using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace FinBot.Handlers.AutoMod
{
    public class AutoSlowmode : ModuleBase<ShardedCommandContext>
    {
        DiscordShardedClient _client;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public AutoSlowmode(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();

            _client.MessageReceived += DecideAutoSlowmode;
        }

        public async Task DecideAutoSlowmode(SocketMessage arg)
        {
            if(arg.Author.IsBot || arg.Channel.GetType() == typeof(SocketDMChannel)/* || Global.DevUIDs.Contains(arg.Author.Id)*/)
            {
                return;
            }

            SocketGuildChannel SGC = (SocketGuildChannel)arg.Channel;
            ulong _id = SGC.Guild.Id;
            IMongoCollection<BsonDocument> guild = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");

            try
            {
                BsonDocument item = await guild.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("autoslowmode").ToJson();

                if (itemVal == null || itemVal == "false")
                {
                    return;
                }
            }

            catch { return; }

            IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
            ulong channelId = arg.Channel.Id;
            long msgTimestamp = Global.ConvertToTimestamp(arg.CreatedAt.DateTime);
            var t = messages.CountDocuments(new BsonDocument { { "guildId", _id.ToString() }, { "channelId", channelId.ToString() }, { "deleted", false }, { "createdTimestamp", new BsonDocument { { "$lte", msgTimestamp }, { "$gte", msgTimestamp - 10 } } } });
            SocketTextChannel chan = (SocketTextChannel)arg.Channel;
            int interval = chan.SlowModeInterval;

            if ((t / 10) >= Global.MaxMessagesPerSecond)
            {
                await chan.ModifyAsync(x =>
                {
                    x.SlowModeInterval = interval + Global.SlowModeIncrementValue;
                });

                try
                {
                    BsonDocument guildDocument = await MongoHandler.FindById(guild, _id);

                    if (guildDocument == null)
                    {
                        MongoHandler.InsertGuild(_id);
                    }

                    BsonDocument item = await guild.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                    string itemVal = item?.GetValue("autoslowmodechannels").ToJson();

                    if (itemVal == null)
                    {
                        guild.InsertOne(new BsonDocument { { "_id", (decimal)_id }, { "autoslowmodechannels", new BsonArray { chan.Id.ToString() } } });
                    }

                    else
                    {
                        List<ulong> idArray = JsonConvert.DeserializeObject<ulong[]>(itemVal).ToList();

                        if (!idArray.Contains(chan.Id))
                        {
                            guild.UpdateOne(new BsonDocument { { "_id", (decimal)_id } }, new BsonDocument { { "$push", new BsonDocument { { "autoslowmodechannels", chan.Id.ToString() } } } });
                        }
                    }
                }

                catch { return; }
            }

            else
            {
                try
                {
                    BsonDocument guildDocument = await MongoHandler.FindById(guild, _id);

                    if (guildDocument == null)
                    {
                        MongoHandler.InsertGuild(_id);
                    }

                    BsonDocument item = await guild.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                    string itemVal = item?.GetValue("autoslowmodechannels").ToJson();

                    if (itemVal == null)
                    {
                        return;
                    }

                    else
                    {
                        List<ulong> idArray = JsonConvert.DeserializeObject<ulong[]>(itemVal).ToList();

                        if (idArray.Contains(chan.Id))
                        {
                            await chan.ModifyAsync(x =>
                            {
                                x.SlowModeInterval = interval - Global.SlowModeIncrementValue;
                            });

                            interval = chan.SlowModeInterval;

                            if (interval == Global.SlowModeIncrementValue)
                            {
                                guild.UpdateOne(new BsonDocument { { "_id", (decimal)_id } }, new BsonDocument { { "$pull", new BsonDocument { { "autoslowmodechannels", chan.Id.ToString() } } } });
                            }
                        }
                    }
                }

                catch { return; }
            }
        }
    }
}
