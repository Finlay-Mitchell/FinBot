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
using Color = Discord.Color;
using FinBot.Attributes.Preconditions;
using System.Reflection;
using Discord.Webhook;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Linq;

namespace FinBot.Modules
{
    public class DevCommands : ModuleBase<ShardedCommandContext> //Dev commands hidden from regular users
    {
        public DiscordShardedClient _client;
        public IServiceProvider _services;
        IUserMessage UpdateMessage;

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
        [RequireDeveloper]
        public async Task Reset([Remainder] string reason = "No reason provided.")
        {
            await Context.Channel.TriggerTypingAsync();
            await Context.Message.Channel.SendMessageAsync($"Restarting bot with reason \"{reason}\"\n");
            _services.GetRequiredService<ShutdownService>().Shutdown(1);
        }

        [Command("terminate")]
        [RequireDeveloper]
        public async Task Term()
        {
            await Context.Message.ReplyAsync($"Shutting down services...");
            _services.GetRequiredService<ShutdownService>().Shutdown(0);
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

                        if (data != null)
                        {
                            await ReplyAsync(data.ToString());
                        }

                        else
                        {
                            await ReplyAsync("Guild config data is null.");
                        }
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
            if (channel == null)
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
        public async Task update([Remainder] string info)
        {
            string[] args = info.Split(new string[] { "===" }, 2, StringSplitOptions.None);
            string gitCommand = "git "; //The beginning string for every command.
            string gitAddArgument = "add -A ";
            string gitCommitArgument = args.Length == 2 ? $@"commit -m ""{args[0]}"" -m ""{args[1]}""" : $@"commit -m ""{args[0]}"""; //Determines whether we commit a title & description or just a title and commits it.
            string gitPushArgument = "push"; //Pushes the changes to Git.
            string gitPull = "pull"; //We do this so the server can pull the changes from Github.

            try
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Color = Color.Orange;
                eb.Title = "Updating...";
                eb.WithCurrentTimestamp();
                UpdateMessage = await Context.Message.ReplyAsync("", false, eb.Build());
                Process pr = new Process();
                pr = Process.Start(gitCommand, gitAddArgument);
                ModifyUpdateEmbed("Began the update...");
                pr.WaitForExit(); //We do this in between every process because it means that processes don't overlap and break, this ensures each command will execute and finish before starting a new one.
                pr = Process.Start(gitCommand, gitCommitArgument);
                ModifyUpdateEmbed($"Began git commit with {args.Length} arguments...");
                pr.WaitForExit();
                pr = Process.Start(gitCommand, gitPushArgument);
                ModifyUpdateEmbed("Pushing to main...");
                pr.WaitForExit();
                pr = Process.Start(gitCommand, gitPull);
                ModifyUpdateEmbed("Pulling from main...");
                pr.WaitForExit();
                eb.Color = Color.Green;
                eb.Title = "Success!";
                eb.Description = "The bot has been updated successfully";
                eb.WithFooter("Bot restarting...");
                eb.WithCurrentTimestamp();
                await Global.ModifyMessage(UpdateMessage, eb);
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                };
                pr = new Process { StartInfo = startInfo };
                pr.Start();
                await pr.StandardInput.WriteLineAsync("taskkill /im FinBot.exe /f"); //This kills the bot process but we've already got the tasks below which will run regardless, so it works fine.
                await pr.StandardInput.WriteLineAsync($"dotnet build {Global.BotDirectory}"); //Now we've closed the application, we can compile and build the bot.
                await pr.StandardInput.WriteLineAsync("FinBot.exe"); //Now we're able to just relaunch the bot.
                pr.WaitForExit();
            }

            catch (Exception ex)
            {
                EmbedBuilder eb = new EmbedBuilder();
                eb.Color = Color.Red;
                eb.WithCurrentTimestamp();
                eb.Title = "Error";
                eb.Description = $"Error whilst updating: {ex.Message}";
                await Global.ModifyMessage(UpdateMessage, eb);
            }
        }

        public async void ModifyUpdateEmbed(string description)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Color = Color.Orange;
            eb.Title = "Updating...";
            eb.WithCurrentTimestamp();
            eb.Description = description;
            await Global.ModifyMessage(UpdateMessage, eb);
        }

        [Command("execute")]
        [RequireDeveloper]
        public async Task GPe(params string[] args)
        {
            //Doesn't work amazingly, but somewhat does.
            string joined = string.Join(" ", Context.Message.Content.Replace("```cs", "").Replace("```", "").Split(' ').Skip(1));
            Script<object> create = CSharpScript.Create(joined, ScriptOptions.Default.WithImports("System", "System.Threading.Tasks", "System.Linq").WithReferences(Assembly.GetAssembly(typeof(EmbedBuilder)),
                        Assembly.GetAssembly(typeof(DiscordWebhookClient)), Assembly.GetExecutingAssembly()).WithImports("Discord", "Discord.WebSocket", "Discord.Commands"), typeof(ShardedCommandContext));

            try
            {
                ScriptState<object> state = await create.RunAsync(Context);

                if (state.ReturnValue == null)
                {
                    await Context.Message.AddReactionAsync(Emote.Parse("<a:tick:859032462410907649>"));
                }
            }

            catch (CompilationErrorException cee)
            {
                await ReplyAsync("", false, new EmbedBuilder
                {
                    Title = "'Twas an error",
                    Description = cee.Message,
                    Color = Color.Red
                }.WithCurrentTimestamp().Build());
            }

            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("Twitch")]
        [RequireDeveloper]
        public async Task test(params string[] args)
        {
            List<TwitchHandler.TwitchData> userInfo = await TwitchHandler.GetTwitchInfo(args[1]);
            Color TwitchColour = new Color(100, 65, 165);
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = userInfo[0].display_name;
            eb.Color = TwitchColour;
            eb.WithCurrentTimestamp();

            switch (args[0].ToLower())
            {

                case "channel":
                    eb.Description = userInfo[0].description;
                    eb.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = userInfo[0].profile_image_url,
                        Text = $"created at: {userInfo[0].created_at}"
                    };
                    eb.AddField("Twitch account information", $"Link to profile: https://www.twitch.tv/{args[1]} \nView count: {userInfo[0].view_count}\nUser id: {userInfo[0].id}");
                    break;

                case "av": case "avatar": case "pfp":
                    eb.Description = $"Here's the profile picture for {userInfo[0].display_name}:";
                    eb.ImageUrl = userInfo[0].profile_image_url;
                    break;

                case "livetest":
                    List<TwitchHandler.UserStreams> userStreams = await TwitchHandler.GetStreams(args[1]);

                    if(userStreams.Count == 0)
                    {
                        await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", $"The user {args[1]} is not currently live on Twitch.", false, Color.Red).Build());
                        return;
                    }

                    eb.Title = $"{args[1]} is live on Twitch!";
                    eb.ImageUrl = $"https://static-cdn.jtvnw.net/previews-ttv/live_user_{args[1]}.jpg";
                    eb.Description = $"[Watch {args[1]} live on Twitch!](https://twitch.tv/{args[1]})";
                    eb.AddField("Stream information", $"title: {userStreams[0].title}\ngame name: {userStreams[0].game_name}\nviewer count: {userStreams[0].viewer_count}");
                    eb.Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = userInfo[0].profile_image_url,
                        Text = $"Live started at: {userStreams[0].started_at}"
                    };

                    break;
            }

            await ReplyAsync("", false, eb.Build());
        }
    }
}