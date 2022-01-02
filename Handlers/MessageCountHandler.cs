using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using MongoDB.Bson;

using Microsoft.Extensions.DependencyInjection;

using System;
using System.Timers;
using System.Threading.Tasks;

namespace FinBot.Handlers
{
    public class MessageCountHandler : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public MessageCountHandler(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();

            Timer t = new Timer() { AutoReset = true, Interval = new TimeSpan(1, 30, 0).TotalSeconds, Enabled = true };
            t.Elapsed += HandleMessageCount;
        }

        private async void HandleMessageCount(object sender, ElapsedEventArgs e)
        {
            //IMongoCollection<BsonDocument> collection = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
            //BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
            //string itemVal = item?.GetValue("messagecountchannel").ToJson();
        }

    }
}
