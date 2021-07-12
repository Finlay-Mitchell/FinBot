using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FinBot.Handlers;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace FinBot.Modules
{
    public class ConfigCommands : ModuleBase<ShardedCommandContext>
    {
        [Command("prefix"), Summary("Sets the new bot prefix for the current guild"), Remarks("(PREFIX)prefix <new_prefix>")]
        public async Task prefix([Remainder] string new_prefix)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.Administrator)
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "prefix", new_prefix } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("prefix", new_prefix));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Prefix updated!");
                embed.WithDescription($"Set the prefix for this guild to: {new_prefix}");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("setwelcomechannel"), Summary("Sets the channel where welcome messages for new members/leaving members are sent"),
            Remarks("(PREFIX)setwelcomechannel <channel>"), Alias("set_welcome_channel", "welcomechannel", "welcome_channel", "welcomemessages", "welcome_messages")]
        public async Task SetWelcomeChannel([Remainder] SocketTextChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels)
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "welcomechannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("welcomechannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the welcome channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("membercountchannel"), Summary("Sets the membercount channel"), Remarks("(PREFIX)membercountchannel <voice_channel>"),
            Alias("setmembercountchannel", "membercount_channel", "set_membercount_channel", "setmembercount")]
        public async Task SetMembercountChannel([Remainder] SocketVoiceChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels)
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "membercountchannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("membercountchannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the membercount channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
                string msg = $"Total Users: {Context.Guild.MemberCount}";

                if (channel.Name != msg)
                {
                    await channel.ModifyAsync(x => x.Name = msg);
                }
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }
        }

        [Command("enablelevelling"), Summary("Enables guild user levelling"), Remarks("(PREFIX)enablelevelling <on/off/true/false>"), Alias("enable_levelling", "levelling")]
        public async Task EnableLevelling([Remainder] string toggle)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.Administrator)
            {
                bool enabled = false;

                if (toggle == "true" || toggle == "on")
                {
                    enabled = true;
                }

                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "levelling", enabled } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("levelling", enabled));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set levelling to {enabled}!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("levellingchannel"), Summary("Sets the channel where users level up messages are setn"), Remarks("(PREFIX)levellingchannel <channel>"), Alias("levelling_channel")]
        public async Task LevellingChannel([Remainder] SocketTextChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels)
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "levellingchannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("levellingchannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the level log channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("modchannel"), Summary("sets the log channel for all user infractions"), Remarks("(PREFIX)modchannel <text channel>")]
        public async Task Modchannel([Remainder] SocketTextChannel channel)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels)
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await MongoHandler.FindById(collection, _id);

                if (guildDocument == null)
                {
                    MongoHandler.InsertGuild(_id);
                }

                BsonDocument guild = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                ulong _chanId = channel.Id;

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "modlogchannel", (decimal)_chanId } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("modlogchannel", _chanId));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription($"Successfully set the mod log channel to <#{_chanId}>!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, embed.Build());
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }
        }
    }
}
