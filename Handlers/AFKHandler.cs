using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace FinBot.Handlers
{
    public class AFKHandler : ModuleBase<SocketCommandContext>
    {
        DiscordShardedClient _client;
        IMongoCollection<BsonDocument> collection;

        public AFKHandler(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();
            MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
            IMongoDatabase database = mongoClient.GetDatabase("finlay");
            collection = database.GetCollection<BsonDocument>("guilds");

            //_client.MessageReceived += CheckForAFK;
            //_client.MessageReceived += CheckIfUserIsAFK;
        }

        public async Task CheckIfUserIsAFK(SocketMessage msg)
        {
            if (msg.Author.IsBot || msg.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }

            SocketGuildChannel ContextChannel = (SocketGuildChannel)msg.Channel;
            ulong _id = ContextChannel.Guild.Id;
            BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "AFKUsers", new BsonDocument { { "User", msg.Author.Id.ToString() } } } };
            BsonDocument item = await collection.Find(document).FirstOrDefaultAsync();
            string itemVal = item?.GetValue($"AFKStatus").ToString();

            if (itemVal != null)
            {
                Global.ConsoleLog("TEST");
                BsonDocument DeleteDocument = new BsonDocument { { "$pull", new BsonDocument { { "AFKUsers", new BsonDocument { { "User", msg.Author.Id.ToString() } } } } } };
                collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), DeleteDocument);
                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} I have removed your AFK status.");
                SocketGuildUser user = (SocketGuildUser)msg.Author;
                //await user.ModifyAsync(x =>
                //{
                //    x.Nickname = null;
                //});
            }
        }

        public async Task CheckForAFK(SocketMessage msg)
        {
            if (msg.Author.IsBot || msg.Channel.GetType() == typeof(SocketDMChannel))
            {
                return;
            }

            if (msg.MentionedUsers.Count > 1 || msg.MentionedUsers.Count == 0)
            {
                return;
            }

            SocketGuildChannel ContextChannel = (SocketGuildChannel)msg.Channel;
            ulong _id = ContextChannel.Guild.Id;
            BsonDocument document = new BsonDocument { { "_id", (decimal)_id } };
            BsonDocument item = await collection.Find(document).FirstOrDefaultAsync();

            try
            {
                string itemVal = item?.GetValue($"AFKUsers").ToJson();
                List<string> stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();

                foreach(var i in stringArray)
                {
                    Global.ConsoleLog(i);
                }

                //if (stringArray.Contains($"{msg.Author.Id}"))
                //{
                //    SocketUserMessage message = (SocketUserMessage)msg;
                //    await message.ReplyAsync($"{msg.MentionedUsers.First()} is AFK: {itemVal}");
                //}
            }

            catch { }
        }
    }
}
