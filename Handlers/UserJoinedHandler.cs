using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

using Discord.Commands;

namespace FinBot.Handlers
{
    public class UserJoinedHandler : ModuleBase<SocketCommandContext>
    {

        private DiscordShardedClient _client;
        MongoClient MongoClient = new MongoClient(Global.mongoconnstr);

        public UserJoinedHandler(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _client.UserJoined += HandleWelcomeAsync;
            _client.UserLeft += HandleGoodbyeAsync;
        }

        public async Task<string> GetWelcomeChannel(SocketGuild guild)
        {
            try
            {
                IMongoDatabase database = MongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("welcomechannel").ToString();

                if (itemVal != null)
                {
                    return itemVal;
                }

                else
                {
                    return Global.Prefix;
                }
            }

            catch
            {
                return Global.Prefix;
            }
        }

        public async Task HandleGoodbyeAsync(SocketGuildUser arg)
        {
            try
            {
                ulong GuildWelcomeChannel = Convert.ToUInt64(GetWelcomeChannel(arg.Guild).Result);

                if (GuildWelcomeChannel == 0)
                {
                    return;
                }

                else
                {
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Title = $"***Sorry to see you go!***",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = arg.GetAvatarUrl(),
                            Text = $"{arg.Username}#{arg.Discriminator}"
                        },
                        Description = $"{arg} has left the server, goodbye",
                        ThumbnailUrl = Global.WelcomeMessageURL,
                        Color = Color.Green
                    };

                    SocketTextChannel Channel = (SocketTextChannel)arg.Guild.GetChannel(GuildWelcomeChannel);
                    await Channel.SendMessageAsync("", false, eb.Build());

                    await arg.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = $"***Sorry to see you leave!***",
                        Description = "Sorry to see you leave the server, Hope to see you soon!",
                        Color = Color.Green

                    }.Build());
                }
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        public async Task HandleWelcomeAsync(SocketGuildUser arg)
        {
            try
            {
                ulong GuildWelcomeChannel = Convert.ToUInt64(GetWelcomeChannel(arg.Guild).Result);

                if (GuildWelcomeChannel == 0)
                {
                    return;
                }

                else
                {
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Title = $"***Welcome to {arg.Guild.Name}!***",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = arg.GetAvatarUrl(),
                            Text = $"{arg.Username}#{arg.Discriminator}"
                        },
                        Description = $"Welcome, {arg.Mention} to {arg.Guild.Name}. You are member #{arg.Guild.Users.Count}!",
                        ThumbnailUrl = Global.WelcomeMessageURL,
                        Color = Color.Green
                    };

                    SocketTextChannel Channel = (SocketTextChannel)arg.Guild.GetChannel(GuildWelcomeChannel);
                    await Channel.SendMessageAsync("", false, eb.Build());

                    await arg.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = $"***Welcome to {arg.Guild.Name}!***",
                        Description = $"Welcome to {arg.Guild.Name} Please read the rules carefully and enjoy your stay!",
                        Color = Color.Green

                    }.Build());
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
