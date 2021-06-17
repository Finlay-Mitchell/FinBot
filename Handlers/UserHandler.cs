using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

using Discord.Commands;
using MySql.Data.MySqlClient;
using System.Linq;

namespace FinBot.Handlers
{
    public class UserHandler : ModuleBase<SocketCommandContext>
    {

        private readonly DiscordShardedClient _client;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public UserHandler(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            
            _client.UserJoined += HandleWelcomeAsync;
            _client.UserLeft += HandleGoodbyeAsync;
            _client.UserJoined += CheckForMutedAsync;
        }

        private async Task CheckForMutedAsync(SocketGuildUser arg)
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM MutedUsers WHERE userId = {arg.Id} AND guildId = {arg.Guild.Id}", conn);
                using MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    IRole role = (arg.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");
                    await arg.AddRoleAsync(role);
                }
                conn.Close();
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }

            finally
            {
                conn.Close();
            }
        }

        public async Task<string> GetWelcomeChannel(SocketGuild guild)
        {
            //This tries to get the welcome channel ID from the database, if not found, defaults to 0

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
                    return "0";
                }
            }

            catch
            {
                return "0";
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
                    try
                    {
                        await arg.SendMessageAsync("", false, new EmbedBuilder()
                        {
                            Title = $"***Sorry to see you leave!***",
                            Description = "Sorry to see you leave the server, Hope to see you soon!",
                            Color = Color.Green

                        }.Build());
                    }

                    catch {}

                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Title = $"***Sorry to see you go!***",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = arg.GetAvatarUrl(),
                            Text = $"{arg.Username}#{arg.Discriminator}"
                        },
                        Description = $"{arg} has left the server, goodbye",
                        Color = Color.Green
                    };

                    SocketTextChannel Channel = (SocketTextChannel)arg.Guild.GetChannel(GuildWelcomeChannel);
                    await Channel.SendMessageAsync("", false, eb.Build());
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
                        Description = $"Welcome, {arg.Mention} to {arg.Guild.Name}. You are member #{arg.Guild.MemberCount}!",
                        Color = Color.Green
                    };

                    SocketTextChannel Channel = (SocketTextChannel)arg.Guild.GetChannel(GuildWelcomeChannel);
                    await Channel.SendMessageAsync("", false, eb.Build());

                    await arg.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Title = $"***Welcome to {arg.Guild.Name}!***",
                        Description = $"Welcome to {arg.Guild.Name} Please read the rules carefully and enjoy your stay!",
                        Color = Color.Green

                    }.WithCurrentTimestamp().Build());
                }
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }
    }
}