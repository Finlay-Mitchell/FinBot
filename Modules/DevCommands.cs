using Discord;
using Discord.Commands;
using DiscColour = Discord.Color;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using FinBot.Handlers;
using System.Collections.Generic;
using FinBot.Services;
using Discord.Rest;
using System.Security.Cryptography;
using System.IO;
using System.Drawing;
using System.Net;
using System.Drawing.Drawing2D;
using Color = Discord.Color;
using FinBot.Attributes.Preconditions;
using FinBot.Attributes;
using System.Collections.ObjectModel;

namespace FinBot.Modules
{
    public class DevCommands : ModuleBase<ShardedCommandContext> //Dev commands hidden from regular users
    {
        public DiscordShardedClient _client;
        public IServiceProvider _services;

        public DevCommands(IServiceProvider services)
        {
            try
            {
                _client = services.GetRequiredService<DiscordShardedClient>();
                _services = services;
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        [Command("restart")]
        public async Task Reset([Remainder] string reason = "No reason provided.")
        {
            if (Global.IsDev(Context.User))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync($"Restarting bot with reason \"{reason}\"\n");
                _services.GetRequiredService<ShutdownService>().Shutdown(1);
            }
        }

        [Command("terminate")]
        public async Task Term()
        {
            if (Global.IsDev(Context.User))
            {
                await Context.Message.ReplyAsync($"Shutting down services...");
                _services.GetRequiredService<ShutdownService>().Shutdown(0);
            }
        }

        [Command("updateSupport")]
        public async Task UpdateSupport(ulong guildId, ulong msgId, [Remainder] string msg)
        {
            if (Global.IsDev(Context.User))
            {
                try
                {
                    SocketDMChannel chn = (SocketDMChannel)await _client.GetDMChannelAsync(guildId);
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Support ticket update");
                    eb.WithFooter($"Support ticket update for {msgId}");
                    eb.WithCurrentTimestamp();
                    eb.WithDescription(msg);
                    eb.WithColor(DiscColour.Purple);

                    try
                    {
                        await chn.SendMessageAsync("", false, eb.Build()); //This throws an exception claiming chn is null....yet it still sends the message.
                        await Context.Message.ReplyAsync("Sent support message response successfully");
                    }

                    catch { return; }
                }

                catch (Exception ex)
                {
                    await Context.Message.ReplyAsync($"I encountered an error trying to respond to that support message. Here are the details:\n{ex.Message}\n{ex.Source}");
                }
            }
        }

        [Command("tld")] //boilerplate code for python TLD module
        public Task Tld(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("exec")] //more boilerplate
        public Task Exec(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("reset_chatbot")]
        public Task ResetChatbot(params string[] arg)
        {
            return Task.CompletedTask;
        }

        [Command("getguilddata")]
        public async Task Getguilddata(params string[] inputOptions)
        {
            if (Global.IsDev(Context.User))
            {
                if (inputOptions.Length == 0)
                {
                    MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                    IMongoDatabase database = mongoClient.GetDatabase("finlay");
                    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                    ulong _id = Context.Guild.Id;

                    try
                    {
                        BsonDocument data = await MongoHandler.FindById(collection, _id);
                        await ReplyAsync(data.ToString());
                    }

                    catch (KeyNotFoundException)
                    {
                        await ReplyAsync("No data was found for guild data.");
                    }
                }

                else
                {
                    MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                    IMongoDatabase database = mongoClient.GetDatabase("finlay");
                    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                    ulong _id = Context.Guild.Id;
                    BsonDocument data = await MongoHandler.FindById(collection, _id);
                    string results = "";

                    for (int i = 0; i < inputOptions.Length; i++)
                    {
                        try
                        {
                            results += $"{data.GetElement(inputOptions[i])}\n\n";
                        }

                        catch (KeyNotFoundException)
                        {
                            results += $"No data found for {inputOptions[i]}.\n\n";
                        }
                    }

                    await ReplyAsync(results);
                }
            }
        }

        [Command("clearalldata")]
        public Task Clearalldata()
        {
            if (Global.IsDev(Context.User))
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                collection.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", _id));
            }

            return Task.CompletedTask;
        }

        [Command("EnBotClientCommands")]
        public async Task EnBotClientCommands(string tof)
        {
            if (Global.IsDev(Context.User))
            {
                if (tof == "true")
                {
                    Global.clientCommands = true;
                    await Context.Message.ReplyAsync($"Success, clientCommands set to {Global.clientCommands}");
                }

                else
                {
                    Global.clientCommands = false;
                    await Context.Message.ReplyAsync($"Success, clientCommands set to {Global.clientCommands}");
                }
            }
        }

        [Command("test")]
        public async Task test(string action = null, SocketUser member = null, SocketTextChannel channel = null)
        {
            //IEnumerable<RestAuditLogEntry> auditlogs = await Context.Guild.GetAuditLogsAsync(3, null, null, id, ActionType.ChannelUpdated).FlattenAsync();
            if (Global.IsDev(Context.User))
            {
                if (action != null)
                {
                    switch (action.ToLower())
                    {
                        case "roles":
                            await AuditRoles(Context, member ?? null);
                            break;
                        case "overwrites":
                            await AuditOverwrites(Context, channel ?? null);
                            break;
                    }
                }

                string result = "";

               

                await ReplyAsync(result);
            }
        }

        public async Task AuditRoles(ShardedCommandContext context, SocketUser user)
        {
            if (user == null)
            {
                await context.Channel.SendMessageAsync("", false, Global.EmbedMessage("Error", "Please mention a user", false, Color.Red).Build());
                return;
            }

            IUserMessage msg = await context.Message.ReplyAsync("Searching...this may take a few seconds");
            EmbedBuilder eb = CreateRoleChangesEmbed(context, user);
            eb.Footer = new EmbedFooterBuilder()
            {
                IconUrl = context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl(),
                Text = $"{context.User}"
            };
            await Global.ModifyMessage(msg, eb);
            //        await sent_message.add_reaction("⏩")
        }

        public EmbedBuilder CreateRoleChangesEmbed(ShardedCommandContext context, SocketUser user, int startIndex = 0)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithCurrentTimestamp();
            embed.Color = Color.Blue;
            embed.Title = $"Role changes for {user} - {user.Id}";

            return embed;
        }

        public async Task<string[]> GetRoleUpdates(ShardedCommandContext context, SocketUser user)
        {
            SocketGuild guild = context.Guild;
            ActionType action = ActionType.MemberRoleUpdated;
            string[] entries = { };

            IEnumerable<RestAuditLogEntry> auditSearch = await guild.GetAuditLogsAsync(int.MaxValue, null, null, user.Id, action).FlattenAsync();

            foreach (RestAuditLogEntry AuditLogEntry in auditSearch)
            {
                if (AuditLogEntry.Data is MemberRoleAuditLogData data)
                {
                    if (data.Target == user)
                    {
                        // IReadOnlyCollection<MemberRoleEditInfo> beforeRoles = ; //Woork on
                        DateTime date = AuditLogEntry.CreatedAt.DateTime;

                        // after roles
                        // taken roles
                        // added roles

                        //checks

                        
                    }
                }
            }

            return entries;
        }

        public async Task AuditOverwrites(ShardedCommandContext context, SocketTextChannel channel)
        {
            if(channel == null)
            {
                await context.Channel.SendMessageAsync("", false, Global.EmbedMessage("Error", "Plesse mention a channel", false, Color.Red).Build());
                return;
            }

            IUserMessage msg = await context.Message.ReplyAsync("Searching...this may take a few seconds");
            EmbedBuilder eb = CreateChannelUpdatesEmbed(context, channel, Context.User);
            await Global.ModifyMessage(msg, eb);
            //        await sent_message.add_reaction("⏩")

        }

        public EmbedBuilder CreateChannelUpdatesEmbed(ShardedCommandContext context, SocketTextChannel channel, SocketUser user)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithCurrentTimestamp();
            eb.Color = Color.Blue;
            eb.Title = $"Channel updates for {channel.Name} - {channel.Id}";
            eb.Footer = new EmbedFooterBuilder()
            {
                IconUrl = context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl(),
                Text = $"{context.User}"
            };

            return eb;
        }

        public async Task<string[]> GetchannelUpdates(ShardedCommandContext context, SocketTextChannel channel)
        {
            SocketGuild guild = context.Guild;
            ActionType action = ActionType.ChannelUpdated;
            string[] entries = { };

            IEnumerable<RestAuditLogEntry> auditSearch = await guild.GetAuditLogsAsync(int.MaxValue, null, null, null, action).FlattenAsync();

            foreach (RestAuditLogEntry AuditLogEntry in auditSearch)
            {
                if (AuditLogEntry.Data is ChannelUpdateAuditLogData data)
                {
                   
                }
            }

            return entries;
        }

        [Command("Update")]
        [RequireDeveloper]
        public async Task update([Remainder]string info)
        {
            string[] args = info.Split(new string[] { "===" }, 2, StringSplitOptions.None);

            string gitCommand = "git ";
            string gitAddArgument = @"add -A ";
            string gitCommitArgument = $@"commit -m ""{args[0]}"" -m ""{args[1]}""";
            string gitPushArgument = @"push";

            try
            {
                Process pr = Process.Start(gitCommand, gitAddArgument);
                pr.WaitForExit();
                pr = Process.Start(gitCommand, gitCommitArgument);
                pr.WaitForExit();
                Process.Start(gitCommand, gitPushArgument);
            }

            catch
            {
                await ReplyAsync("failed");
            }
            
        }
    }
}