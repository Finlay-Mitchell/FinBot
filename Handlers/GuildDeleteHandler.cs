using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Handlers
{
    public class GuildDeleteHandler
    {
        private readonly DiscordShardedClient _client;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public GuildDeleteHandler(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _client.LeftGuild += OnGuildDelete;
        }

        private async Task OnGuildDelete(SocketGuild arg)
        {
            try
            {
                IMongoCollection<BsonDocument> guilds = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
                guilds.FindOneAndDelete(new BsonDocument { { "_id", (decimal)arg.Id } });
            }

            catch { return; }
        }
    }
}
