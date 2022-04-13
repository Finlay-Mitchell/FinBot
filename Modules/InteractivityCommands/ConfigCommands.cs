using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using FinBot.Attributes.Interactivity.Preconditions;
using FinBot.Handlers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using QuickChart;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RequireUserPermissionAttribute = FinBot.Attributes.Interactivity.Preconditions.RequireUserPermissionAttribute;

namespace FinBot.Modules.InteractivityCommands
{
    [Group("config", "Commands relating to configuration.")]
    public class ConfigCommands : InteractionModuleBase<ShardedInteractionContext>
    {
        public InteractionService _commands { get; set; }
        private CommandHandler _handler;
        readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

        public ConfigCommands(IServiceProvider services)
        {
            _handler = services.GetRequiredService<CommandHandler>();
        }

        [SlashCommand("message_count_channel", "Sets the channel for which the guild message count will be displayed.")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task SetMessageCountChannel([Summary(name: "channel", description: "The channel to display the message count channel.")]SocketVoiceChannel channel)
        {
            IMongoCollection<BsonDocument> collection = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
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
                BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "messagecountchannel", (decimal)_chanId } };
                collection.InsertOne(document);
            }

            else
            {
                collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("messagecountchannel", _chanId));
            }

            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Success");
            embed.WithDescription($"Successfully set the message count channel to <#{_chanId}>!");
            embed.WithColor(Color.Green);
            embed.WithAuthor(Context.User);
            embed.WithCurrentTimestamp();
            await Context.Interaction.RespondAsync("", embed: embed.Build());
        }

        [Group("role_assign", "Commands relating to role assignment")]
        public class RoleAssignConfigCommands : InteractionModuleBase<ShardedInteractionContext>
        {
            public InteractionService _commands { get; set; }
            private CommandHandler _handler;
            readonly MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);

            public RoleAssignConfigCommands(IServiceProvider services)
            {
                _handler = services.GetRequiredService<CommandHandler>();
            }

            [SlashCommand("set_role_assign", "Sets the embed for role assignment.")]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task SetRoleAssign()
            {
                try
                {
                    await Context.Interaction.RespondAsync("The role assign embed has now been added, in order to configure this use the `message id` number at the bottom.", ephemeral: true);
                    Embed e = Global.EmbedMessage("Role assign", "This has not been configured yet", false).Build();
                    IMessage msg = await Context.Interaction.Channel.SendMessageAsync("", false, e);
                    IMongoCollection<BsonDocument> guilds = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
                    e.ToEmbedBuilder().Footer = new EmbedFooterBuilder()
                    {
                        Text = $"message id: {msg.Id}"
                    };
                    await Global.ModifyMessage((IUserMessage)msg, e.ToEmbedBuilder());
                    ulong _id = Context.Guild.Id;
                    BsonDocument guildDocument = await MongoHandler.FindById(guilds, _id);

                    if (guildDocument == null)
                    {
                        MongoHandler.InsertGuild(_id);
                    }

                    BsonDocument guild = await guilds.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();

                    if (guild == null)
                    {
                        BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "reactionRoles", new BsonDocument { { "_id", (decimal)msg.Id}, { "channel", (decimal)Context.Channel.Id }, 
                            { "embed", new BsonDocument { { "title", e.Title }, { "description", e.Description }, { "colour", e.Color.Value.RawValue } } }, { "roles", new BsonArray() } } } };
                        guilds.InsertOne(document);
                    }

                    else
                    {
                        await guilds.FindOneAndUpdateAsync(new BsonDocument { { "_id", (decimal)_id } }, new BsonDocument { { "$set", new BsonDocument { { "reactionRoles", 
                        new BsonDocument { { "_id", (decimal)msg.Id }, { "channel", (decimal)Context.Channel.Id }, { "embed", new BsonDocument { { "title", e.Title }, { "description", e.Description }, 
                            { "colour", e.Color.Value.RawValue } } }, { "roles", new BsonArray() } } } } } });
                    }
                }
                catch(Exception ex)
                {
                    Global.ConsoleLog(ex.Message);
                }
            }

            public enum colours
            {
                Black,
                Teal,
                DarkTeal,
                Green,
                DarkGreen,
                Blue,
                DarkBlue,
                Purple,
                DarkPurple,
                Magenta,
                DarkMagenta,
                Gold,
                LightOrange,
                Orange,
                DarkOrange,
                Red,
                DarkRed,
                LightGrey,
                LighterGrey,
                DarkGrey,
                DarkerGrey,
                None
            }

            [SlashCommand("edit_assign_embed", "Modify the role assignment embed.")]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task d([Summary(name: "title", description: "The title for the role assignment embed - optional.")] string title = null,
                [Summary(name: "description", description: "The description for the role assignment embed - optional.")] string description = null,
                [Summary(name: "colour", description: "The colour for the role assignment embed - optional.")] colours colour = colours.None)
            {
                IMongoCollection<BsonDocument> guilds = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await guilds.Find(new BsonDocument { { "_id", (decimal)_id }, { "reactionRoles", new BsonDocument { { "$ne", BsonNull.Value }, { "$exists", true } } } }).FirstOrDefaultAsync();

                if (guildDocument == null)
                {
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error!", "Reaction role message not set, please use the \"set_role_assign\" command in order to make one.", false, Color.Red).Build());
                    return;
                }

                try
                {
                    BsonDocument messageId = (BsonDocument)guildDocument.GetValue("reactionRoles");
                    SocketTextChannel channel = Context.Guild.GetTextChannel(Convert.ToUInt64(messageId.GetValue("channel").ToString()));
                    BsonDocument embeds = messageId.GetValue("embed").AsBsonDocument;
                    IMessage msg = await channel.GetMessageAsync(Convert.ToUInt64(messageId.GetValue("_id").ToString()));
                    EmbedBuilder embed = msg.Embeds.First().ToEmbedBuilder();
                    BsonDocument updateDocument = new BsonDocument();

                    Color value = colour switch
                    {
                        colours.Black => 0,
                        colours.Teal => 0x1ABC9C,
                        colours.DarkTeal => 0x11806A,
                        colours.Green => 0x2ECC71,
                        colours.DarkGreen => 0x1F8B4C,
                        colours.Blue => 0x3498DB,
                        colours.DarkBlue => 0x206694,
                        colours.Purple => 0x9B59B6,
                        colours.DarkPurple => 0x71368A,
                        colours.Magenta => 0xE91E63,
                        colours.DarkMagenta => 0xAD1457,
                        colours.Gold => 0xF1C40F,
                        colours.LightOrange => 0xC27C0E,
                        colours.Orange => 0xE67E22,
                        colours.DarkOrange => 0xA84300,
                        colours.Red => 0xE74C3C,
                        colours.DarkRed => 0x992D22,
                        colours.LightGrey => 0x979C9F,
                        colours.LighterGrey => 0x95A5A6,
                        colours.DarkGrey => 0x607D8B,
                        colours.DarkerGrey => 0x546E7A,
                        colours.None => new Color((uint)embeds.GetValue("colour"))
                    };

                    if (value.RawValue != embeds.GetValue("colour") && colour != colours.None)
                    {
                        updateDocument.Add("reactionRoles.embed.colour", value.RawValue);
                        embed.Color = value;
                    }

                    if(title != embeds.GetValue("title") && title != null)
                    {
                        updateDocument.Add("reactionRoles.embed.title", title);
                        embed.Title = title;
                    }

                    if(description != embeds.GetValue("description") && description != null)
                    {
                        updateDocument.Add("reactionRoles.embed.description", description);
                        embed.Description = description;
                    }

                    await Global.ModifyMessage((IUserMessage)msg, embed);
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Success!", "Role assign embed successfully changed.", false, Color.Green).Build());
                    await guilds.UpdateOneAsync(new BsonDocument { { "_id", (decimal)_id } }, new BsonDocument { { "$set", updateDocument } });
                }

                catch (Exception ex)
                {
                    Global.ConsoleLog(ex.Message + ex.StackTrace);
                }
            }

            [SlashCommand("add_reaction_role", "Adds a reaction role to the reaction role list.")]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task AddReactionRole()
            {
                IMongoCollection<BsonDocument> guilds = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await guilds.Find(new BsonDocument { { "_id", (decimal)_id }, { "reactionRoles", new BsonDocument { { "$ne", BsonNull.Value }, { "$exists", true } } } }).FirstOrDefaultAsync();

                if (guildDocument == null)
                {
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error!", "Reaction role message not set, please use the \"set_role_assign\" command in order to make one.", false, Color.Red).Build());
                    return;
                }


            }

            [SlashCommand("remove_reaction_role", "Removes a reaction role to the reaction role list.")]
            [RequireBotPermission(ChannelPermission.EmbedLinks)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task RemoveReactionRole()
            {
                IMongoCollection<BsonDocument> guilds = MongoClient.GetDatabase("finlay").GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument guildDocument = await guilds.Find(new BsonDocument { { "_id", (decimal)_id }, { "reactionRoles", new BsonDocument { { "$ne", BsonNull.Value }, { "$exists", true } } } }).FirstOrDefaultAsync();

                if (guildDocument == null)
                {
                    await Context.Interaction.RespondAsync("", embed: Global.EmbedMessage("Error!", "Reaction role message not set, please use the \"set_role_assign\" command in order to make one.", false, Color.Red).Build());
                    return;
                }


            }
        }
    }
}
