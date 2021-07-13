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

        /// <summary>
        /// Check whether the user who joined was previously muted during the time where they left the guild, if true, re-add the muted role.
        /// </summary>
        /// <param name="arg">The user who joined the guild.</param>
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

        /// <summary>
        /// Get the welcome channel where the announcements for users joining/leaving are sent.
        /// </summary>
        /// <param name="guild">The guild to get the edata for.</param>
        /// <returns>A string containing the channel id.</returns>
        public async Task<string> GetWelcomeChannel(SocketGuild guild)
        {
            //If no data found, defaults to 0.
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

        /// <summary>
        /// Sends a message to the welcome/leave channel, if specified, when the user leaves.
        /// </summary>
        /// <param name="arg">The user who left the guild.</param>
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

        /// <summary>
        /// Sends a message to the welcome/leave channel, if specified, when the user joins.
        /// </summary>
        /// <param name="arg">The user who joined the guild.</param>
        /// <returns></returns>
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