using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace FinBot.Handlers
{
    public class MemberCountHandler : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordShardedClient _client;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public MemberCountHandler(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();

            _client.UserJoined += HandleUserCount;
            _client.UserLeft += HandleUserCount;
        }


        public async Task<string> GetUserCountChannel(SocketGuild guild)
        {
            //This tries to get the user count channel ID from the database, if not found, defaults to 0

            try
            {
                IMongoDatabase database = MongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("membercountchannel").ToString();

                if (itemVal != null)
                {
                    return itemVal;
                }

                else
                {
                    return "0";
                }
            }

            catch
            {
                return "0";
            }
        }
        
        public async Task HandleUserCount(SocketGuildUser arg)
        {
            try
            {
                ulong MemberCountChannel = Convert.ToUInt64(GetUserCountChannel(arg.Guild).Result);

                if (MemberCountChannel == 0)
                {
                    return;
                }

                else
                {
                    SocketVoiceChannel channel = arg.Guild.GetVoiceChannel(MemberCountChannel);
                    SocketGuild guild = (channel as SocketGuildChannel)?.Guild;
                    string msg = $"Total Users: {guild.MemberCount}";


                    if (channel.Name != msg)
                    {
                        await channel.ModifyAsync(x => x.Name = msg);
                    }
                }
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                return;
            }
        }
    }
}
