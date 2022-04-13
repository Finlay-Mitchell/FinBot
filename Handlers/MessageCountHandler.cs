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

            Timer t = new Timer() { AutoReset = true, Interval = 600000 , Enabled = true };
            t.Elapsed += HandleMessageCount;
        }

        private async void HandleMessageCount(object sender, ElapsedEventArgs e)
        {
            IMongoCollection<BsonDocument> guilds = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");

            foreach (BsonDocument document in await guilds.Find(new BsonDocument { { "messagecountchannel", new BsonDocument { { "$ne", BsonNull.Value }, { "$exists", true } } } }).ToListAsync())
            {
                SocketGuild guild = _client.GetGuild(Convert.ToUInt64(document.GetValue("_id")));
                SocketVoiceChannel channel = guild.GetVoiceChannel(Convert.ToUInt64(document.GetValue("messagecountchannel")));

                if(channel == null)
                {
                    return;
                }


                string channelName = channel.Name;
                int oldCount = 0;
                
                try
                {
                    oldCount = Convert.ToInt16(channelName.Split(": ")[1].Replace(",", ""));
                }

                catch 
                {
                    oldCount = 0;
                }

                IMongoCollection<BsonDocument> messages = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("messages");
                long messageCount = await messages.CountDocumentsAsync(new BsonDocument { { "guildId", $"{guild.Id}" }, { "deleted", false } });

                if (messageCount - oldCount > messageCount / 200)
                {
                    await channel.ModifyAsync(x => x.Name = $"Messages: {messageCount:n0}");
                }
            }
        }

    }
}
