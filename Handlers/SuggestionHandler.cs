using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using System.Linq;
using Discord.Rest;

namespace FinBot.Handlers
{
    public class SuggestionHandler : ModuleBase<SocketCommandContext>
    {
        DiscordShardedClient _client;
        IMongoCollection<BsonDocument> collection;

        public SuggestionHandler(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();
            MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
            IMongoDatabase database = mongoClient.GetDatabase("finlay");
            collection = database.GetCollection<BsonDocument>("guilds");

            _client.MessageReceived += CheckForSuggestionChannel;
        }

        public async Task CheckForSuggestionChannel(SocketMessage msg)
        {
            if (msg.Author.IsBot || msg.Channel.GetType() == typeof(SocketDMChannel) || Global.DevUIDs.Contains(msg.Author.Id))
            {
                return;
            }

            SocketGuildUser user = (SocketGuildUser)msg.Author;

            if (user.GuildPermissions.ManageMessages)
            {
                return;
            }

            SocketGuildChannel channel = (SocketGuildChannel)msg.Channel;
            ulong _id = channel.Guild.Id;
            BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
            string itemVal = item?.GetValue("suggestionschannel").ToString();

            if (itemVal != null)
            {
                if (itemVal == msg.Channel.Id.ToString())
                {
                    await msg.DeleteAsync();
                    RestUserMessage message = await msg.Channel.SendMessageAsync("", false, Global.EmbedMessage("Error", $" {msg.Author.Mention} You can't send messages here unless you're " +
                        $"executing the {await Global.DeterminePrefix(new ShardedCommandContext(_client, msg as SocketUserMessage))}suggest command.", false, Discord.Color.Red).Build());
                    await Task.Delay(5000);
                    await message.DeleteAsync();
                }
            }
        }
    }
}
