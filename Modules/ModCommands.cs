using Discord;
using Discord.Commands;
using Discord.WebSocket;
using FinBot.Handlers;
using FinBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Bson;
using Newtonsoft.Json;
using FinBot.Interactivity;
using System.IO;
using System.Net;

namespace FinBot.Modules
{
    public class ModCommands : InteractiveBase
    {
        private DiscordShardedClient _client;

        public ModCommands(IServiceProvider services)
        {
            //Sometimes, when using a bot-initiated command, it throws an exception that there's no service for DiscordShardedClient, this works around that and allows it to work.

            try
            {
                _client = services.GetRequiredService<DiscordShardedClient>();
            }

            catch(Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        [Command("clear"), Summary("clears a specified amount of messages from the chat"), Remarks("(PREFIX) clear<amount>"), Alias("purge", "clr")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ReadMessageHistory | ChannelPermission.ManageMessages)]
        public async Task Purge(int amount)
        {
            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

            if (!UserCheck.GuildPermissions.ManageMessages && !Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }

            else
            {
                if (amount >= 15 && amount > 1)
                {
                    IUserMessage conformMessage = await Context.Channel.SendMessageAsync("", false, Global.EmbedMessage("Purge conformation", $"Please type \"yes\" to purge {amount} of messages.", false, Color.Orange).Build());
                    SocketMessage conformation = await NextMessageAsync(timeout: TimeSpan.FromSeconds(10));

                    if (conformation.Content != null)
                    {
                        await conformMessage.DeleteAsync();

                        if (conformation.Content.ToLower() == "yes")
                        {
                            IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync((int)amount + 2).FlattenAsync();
                            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
                            await Context.Channel.TriggerTypingAsync();
                            IUserMessage msg = await Context.Message.Channel.SendMessageAsync($"Purge completed!");
                            await Task.Delay(2000);
                            await msg.DeleteAsync();

                            return;
                        }

                        else
                        {
                            await Context.Message.DeleteAsync();
                            await conformation.DeleteAsync();
                            IUserMessage msg = await Context.Message.Channel.SendMessageAsync("Purge canceled.");
                            await Task.Delay(2000);
                            await msg.DeleteAsync();

                            return;
                        }
                    }

                    else
                    {
                        await Context.Message.DeleteAsync();
                        await conformation.DeleteAsync();
                        IUserMessage msg = await Context.Message.Channel.SendMessageAsync("Purge canceled.");
                        await Task.Delay(2000);
                        await msg.DeleteAsync();
                        
                        return;
                    }
                }

                else if (amount < 15 && amount >= 1)
                {
                    IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync((int)amount + 1).FlattenAsync();
                    await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
                    await Context.Channel.TriggerTypingAsync();
                    IUserMessage msg = await Context.Message.Channel.SendMessageAsync($"Purge completed!");
                    await Task.Delay(2000);
                    await msg.DeleteAsync();

                    return;
                }

                else
                {
                    await Context.Message.DeleteAsync();
                    IUserMessage msg = await Context.Message.Channel.SendMessageAsync($"Please enter a number equal to 1 or larger.");
                    await Task.Delay(2000);
                    await msg.DeleteAsync();

                    return;
                }
            }
        }

        [Command("slowmode"), Summary("sets the slowmode of the current chat"), Remarks("(PREFIX)slowmode <time in seconds | \"spam\" for 15 seconds | \"simp\" for 7 seconds>"), Alias("slow")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageChannels)]
        public async Task Slowmode([Remainder] string value)
        {
            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

            if (UserCheck.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                try
                {
                    int val = 0;

                    try
                    {
                        val = Convert.ToInt32(value);
                    }

                    catch
                    {
                        if (value == "spam")
                        {
                            value = "15";
                            val = 15;
                        }

                        else
                        {
                            value = "0";
                            val = 0;
                        }
                    }

                    if (val > 21600)
                    {
                        await Context.Channel.TriggerTypingAsync();
                        await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                        {
                            Color = Color.LightOrange,
                            Title = "Slowmode interval to large!",
                            Description = $"Sorry, {Context.Message.Author.Mention} but the max slowmode you can have is 21600 seconds (6 hours).",
                            Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                                Text = $"{Context.User}"
                            },
                        }.Build());
                    }

                    SocketTextChannel chan = Context.Guild.GetTextChannel(Context.Channel.Id);
                    await chan.ModifyAsync(x =>
                    {
                        x.SlowModeInterval = val;
                    });
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.Green,
                        Title = $"Set the slowmode to {value}!",
                        Description = $"{Context.Message.Author.Mention} successfully modified the slowmode of <#{Context.Channel.Id}> to {value} seconds!",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{Context.User}"
                        },
                    }.Build());
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }
        }

        /// <summary>
        /// Writes a query to the moderation log database.
        /// </summary>
        /// <param name="conn">The connection string.</param>
        /// <param name="userId">The id of the user of whom has earned the infraction.</param>
        /// <param name="action">The action taken towards the user.</param>
        /// <param name="ModeratorId">The id of the moderator who gave the user the infraction.</param>
        /// <param name="reason">The reason for the infraction.</param>
        /// <param name="GuildId">The id of the guild that the user earned their infraction in.</param>
        /// <param name="indx">The option for what kind of interaction is made with the database.</param>
        public static void AddToModlogs(MySqlConnection conn, ulong userId, Action action, ulong ModeratorId, string reason, ulong GuildId, int indx)
        {
            try
            {
                DateTime dt = DateTime.Now;
                long time = Global.ConvertToTimestamp(dt);
                MySqlCommand cmd = new MySqlCommand($"INSERT INTO modlogs(userId, action, moderatorId, reason, guildId, dateTime, indx) VALUES({userId}, '{action}', {ModeratorId}, '{reason}', {GuildId}, {time}, {indx})", conn);
                cmd.ExecuteNonQuery();
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        /// <summary>
        /// Makes a DELETE query from the database.
        /// </summary>
        /// <param name="type">The option for what kind of interaction is made with the database.</param>
        /// <param name="conn">The connection string.</param>
        /// <param name="guildId">The guild id of which the request is being made from.</param>
        /// <param name="id">The id of the user in question.</param>
        /// <param name="number">The index of the infraction being removed.</param>
        public void DeleteFromModlogs(uint type, MySqlConnection conn, ulong guildId, ulong id, int number = 0)
        {
            try
            {
                if (type == 0)
                {
                    MySqlCommand cmd = new MySqlCommand($"DELETE FROM modlogs WHERE guildId = {guildId} AND userId = {id} AND indx = {number}", conn);
                    cmd.ExecuteNonQuery();
                }

                else
                {
                    MySqlCommand cmd = new MySqlCommand($"DELETE FROM modlogs WHERE guildId = {guildId} AND userId = {id}", conn);
                    cmd.ExecuteNonQuery();
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        /// <summary>
        /// Where a users infraction is dealt with.
        /// </summary>
        /// <param name="userId">The id of the user whom earned the infraction.</param>
        /// <param name="action">The action taken towards the user.</param>
        /// <param name="ModeratorId">The id of the moderator who gave the user the infraction.</param>
        /// <param name="reason">The reason for the infraction.</param>
        /// <param name="GuildId">The id of the guild where the infraction took place.</param>
        public async void AddModlogs(ulong userId, Action action, ulong ModeratorId, string reason, ulong GuildId)
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

                try
                {
                    MySqlConnection queryConn = new MySqlConnection(Global.MySQL.ConnStr);
                    conn.Open();
                    MySqlCommand query = new MySqlCommand($"SELECT * FROM modlogs WHERE guildId = {GuildId} AND userId = {userId}", conn);
                    using MySqlDataReader rdr = query.ExecuteReader();
                    int indx = 0;

                    while (rdr.Read())
                    {
                        indx++;

                        /*
                         * 
                         * THIS NEEDS TO BE IMPLEMENTED SUCH AS:
                         * 5 warns in 1 day - 6 hour mute
                         * 10 warns - 12 hour mute
                         * 15 warns - 24 hour mute
                         * 
                         */

//                        if (indx % 5 == 0)
//                        {
//                            await AddMuteAsync(userID, GuildId);
//                            SocketGuild guild = _client.GetGuild(GuildId);
//                            string modlogchannel = await Global.GetModLogChannel(guild);

//                            if (modlogchannel == "0")
//                            {
//                                return;
//                            }

//                            SocketUser user = guild.GetUser(userId);
//                            SocketTextChannel logchannel = guild.GetTextChannel(Convert.ToUInt64(modlogchannel));
//                            EmbedBuilder eb = new EmbedBuilder();
//                            eb.WithTitle($"{user} automuted");
//                            eb.AddField("User", $"{user.Username}", true);
//                            eb.AddField("Moderator", $"LexiBot automod.", true);
//                            eb.AddField("Reason", $"\"Too many infractions.\"", true);
//                            eb.AddField("Infraction count", indx.ToString(), true);
//                            eb.WithAuthor(user);
//                            eb.WithCurrentTimestamp();
//                            await logchannel.SendMessageAsync("", false, eb.Build());

//;
//                            //    string[] formats = { @"h\h", @"s\s", @"m\m\ s\s", @"h\h\ m\m\ s\s", @"m\m", @"h\h\ m\m", @"d\d h\h\ m\m\ s\s", @"d\d", @"d\d h\h", @"d\d h\h m\m", @"d\d h\h m\m s\s" };
//                            //    TimeSpan t = TimeSpan.ParseExact("1h", formats, null);
//                            //    await MuteService.MuteAsyncSeconds((SocketUser)user, Global.Client.GetGuild(Global.GuildId), t, Global.Client.GetGuild(Global.GuildId).GetTextChannel(Global.ModLogChannel));
//                        }
                    }

                    indx += 1;

                    if (indx % 5 == 0)
                    {
                        await AddMuteAsync(userId, GuildId);
                        SocketGuild guild = _client.GetGuild(GuildId);
                        string modlogchannel = await Global.GetModLogChannel(guild);

                        if (modlogchannel == "0")
                        {
                            return;
                        }

                        SocketUser user = guild.GetUser(userId);
                        SocketTextChannel logchannel = guild.GetTextChannel(Convert.ToUInt64(modlogchannel));
                        EmbedBuilder eb = new EmbedBuilder();
                        eb.WithTitle($"{user} automuted");
                        eb.AddField("User", $"{user.Username}", true);
                        eb.AddField("Moderator", $"LexiBot automod.", true);
                        eb.AddField("Reason", $"\"Too many infractions.\"", true);
                        eb.AddField("Infraction count", indx.ToString(), true);
                        eb.WithAuthor(user);
                        eb.WithCurrentTimestamp();
                        await logchannel.SendMessageAsync("", false, eb.Build());
                        await MuteService.SetMute(guild, user, logchannel, DateTime.Now, "1h30m", null, null);
                        //    string[] formats = { @"h\h", @"s\s", @"m\m\ s\s", @"h\h\ m\m\ s\s", @"m\m", @"h\h\ m\m", @"d\d h\h\ m\m\ s\s", @"d\d", @"d\d h\h", @"d\d h\h m\m", @"d\d h\h m\m s\s" };
                        //    TimeSpan t = TimeSpan.ParseExact("1h", formats, null);
                        //    await MuteService.MuteAsyncSeconds((SocketUser)user, Global.Client.GetGuild(Global.GuildId), t, Global.Client.GetGuild(Global.GuildId).GetTextChannel(Global.ModLogChannel));
                    }

                    queryConn.Open();
                    AddToModlogs(queryConn, userId, action, ModeratorId, reason, GuildId, indx);
                    queryConn.Close();
                }

                catch (Exception ex)
                {
                    Global.ConsoleLog(ex.Message);
                }

                finally
                {
                    conn.Close();
                }
            }

            catch { }
        }

        /// <summary>
        /// An emum containing the different obtainable infractions.
        /// </summary>
        public enum Action
        {
            Warned,
            Kicked,
            Banned,
            Muted,
            VoiceMuted,
            TempMuted
        }

        /// <summary>
        /// Handles the mute of a user.
        /// </summary>
        /// <param name="userId">The id of the user to be muted.</param>
        /// <param name="guildId">The id of the guild where the user has been muted from.</param>
        public async Task AddMuteAsync(ulong userId, ulong guildId)
        {
            SocketGuild guild = _client.GetGuild(guildId);
            SocketGuildUser user = guild.GetUser(userId);
            IRole role = (guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");

            if (role == null)
            {
                role = await guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false), null, false, null);
            }

            try
            {
                await role.ModifyAsync(x => x.Position = guild.CurrentUser.Hierarchy);

                foreach (var channel in guild.TextChannels)
                {
                    if (!channel.GetPermissionOverwrite(role).HasValue || channel.GetPermissionOverwrite(role).Value.SendMessages == PermValue.Allow)
                    {
                        await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                    }
                }
            }

            catch { }

            finally
            {
                await user.AddRoleAsync(role);
            }
        }

        [Command("clearlogs"), Summary("Clears users logs"), Remarks("(PREFIX)clearlogs <user> <amount>"), Alias("clearlog", "cl")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Clearwarn(string user1 = null, int number = 999)
        {
            SocketGuildUser user = Context.User as SocketGuildUser;
            IReadOnlyCollection<SocketUser> mentions = Context.Message.MentionedUsers;

            if (!user.GuildPermissions.Administrator && !Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                await Context.Message.ReplyAsync("", false, new Discord.EmbedBuilder()
                {
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                    Title = "You do not have permission to execute this command",
                    Description = "You do not have the valid permission to execute this command",
                    Color = Color.Red
                }.Build());

                return;
            }

            if (mentions.Count == 0)
            {
                EmbedBuilder noUser = new EmbedBuilder();
                noUser.WithTitle("Error");
                noUser.WithDescription("Please mention a user!");
                noUser.WithColor(Color.Red);
                noUser.WithAuthor(Context.Message.Author);
                noUser.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, noUser.Build());

                return;
            }

            SocketUser u = mentions.First();

            try
            {
                MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

                try
                {
                    conn.Open();
                    DeleteFromModlogs(0, conn, Context.Guild.Id, u.Id, number);
                    EmbedBuilder b = new EmbedBuilder()
                    {
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = u.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{u}"
                        },
                        Title = $"Successfully cleared log for **{u.Username}**",
                        Color = Color.DarkMagenta,
                        Description = $"Log for {u.Username} cleared.",
                        Fields = new List<EmbedFieldBuilder>()
                    }.WithCurrentTimestamp();
                    await Context.Message.ReplyAsync("", false, b.Build());
                }

                catch (Exception ex)
                {
                    Global.ConsoleLog(ex.Message);
                }

                finally
                {
                    conn.Close();
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        [Command("ClearAllModLogs"), Summary("Clears all logs for a user"), Remarks("(PREFIX)ClearAllModLogs <user>"), Alias("clearalllogs", "cal", "caml")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task ClearAllModLogs(string user1 = null)
        {
            SocketGuildUser user = Context.User as SocketGuildUser;
            IReadOnlyCollection<SocketUser> mentions = Context.Message.MentionedUsers;

            if (!user.GuildPermissions.Administrator && !Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                await Context.Message.ReplyAsync("", false, new Discord.EmbedBuilder()
                {
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                    Title = "You do not have permission to execute this command",
                    Description = "You do not have the valid permission to execute this command",
                    Color = Color.Red
                }.WithCurrentTimestamp().Build());

                return;
            }

            if (mentions.Count == 0)
            {
                EmbedBuilder noUser = new EmbedBuilder();
                noUser.WithTitle("Error");
                noUser.WithDescription("Please mention a user!");
                noUser.WithColor(Color.Red);
                noUser.WithAuthor(Context.Message.Author);
                noUser.WithCurrentTimestamp();
                await Context.Message.ReplyAsync("", false, noUser.Build());

                return;
            }

            try
            {
                SocketUser u = mentions.First();
                MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

                try
                {
                    conn.Open();
                    DeleteFromModlogs(1, conn, Context.Guild.Id, u.Id);
                    EmbedBuilder b = new EmbedBuilder()
                    {
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = u.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{u}"
                        },
                        Title = $"Successfully cleared all logs for **{u.Username}**",
                        Color = Color.DarkMagenta,
                        Description = $"Modlogs for {u.Username} have been cleared",
                        Fields = new List<EmbedFieldBuilder>()
                    };
                    await Context.Message.ReplyAsync("", false, b.Build());
                }

                catch (Exception ex)
                {
                    Global.ConsoleLog(ex.Message);
                }

                finally
                {
                    conn.Close();
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        [Command("ban"), Summary("bans user from the guild"), Remarks("(PREFIX)ban <user> (optional)prune days (optional)reason")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUser(IGuildUser user, [Remainder] string reason = "No reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.BanMembers && !Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }

            if (user == Context.User)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to ban yourself.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }

            await Context.Message.DeleteAsync();

            if (user.GuildPermissions.ManageMessages)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "I don't have Permission!",
                    Description = "I do not have permission to ban a moderator.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{user.Username}#{user.Discriminator}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }

            try
            {
                await user.SendMessageAsync($"You've been banned from {Context.Guild}.\nReason: {reason}\nTime of ban: {DateTime.Now}.");
            }

            catch { }

            await user.BanAsync(0, $"{reason} - {Context.Message.Author}");
            await Context.Guild.AddBanAsync(user, 0, reason);
            AddModlogs(user.Id, Action.Banned, Context.Message.Author.Id, reason, Context.Guild.Id);
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = $"***{user.Username} has been banned***",
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    Text = $"{user.Username}#{user.Discriminator}",
                },
                Description = $"{user} has been banned at {DateTime.Now}\nReason: {reason}",
                Color = Color.DarkRed,
            };
            eb.WithCurrentTimestamp();
            await Context.Channel.TriggerTypingAsync();
            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("kick"), Summary("kicks member from the guild"), Remarks("(PREFIX)kick <user> (optional)<reason>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task KickUser(IGuildUser user, [Remainder] string reason = "No reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.KickMembers && !Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }

            if (user == Context.User)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to kick yourself.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }

            await Context.Message.DeleteAsync();

            if (user.GuildPermissions.ManageMessages)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "I don't have Permission!",
                    Description = "I do not have permission to kick a moderator.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{user.Username}#{user.Discriminator}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }

            try
            {
                await user.SendMessageAsync($"You've been kicked from {Context.Guild}.\nReason: {reason}\nTime of kick: {DateTime.Now}.");
            }

            catch { }

            await user.KickAsync($"{reason} - {Context.Message.Author}");
            AddModlogs(user.Id, Action.Kicked, Context.Message.Author.Id, reason, Context.Guild.Id);
            await Context.Channel.TriggerTypingAsync();
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = $"***{user.Username} has been kicked***",
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                    Text = $"{user.Username}#{user.Discriminator}"
                },
                Description = $"{user} has been kicked at {DateTime.Now}\nReason: {reason}",
                Color = Color.Orange
            };
            eb.WithCurrentTimestamp();
            await Context.Channel.TriggerTypingAsync();
            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("vcmute"), Summary("Mutes a user from voice channels"), Remarks("(PREFIX)vcmute <user> (optional) <user>"), Alias("voicechatmute")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.MuteMembers)]
        public async Task VcMute(SocketGuildUser user, [Remainder] string reason = "No reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.DeafenMembers || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                SocketVoiceChannel vc = Context.Guild.GetUser(user.Id).VoiceChannel;
                SocketGuildUser User = Context.Guild.GetUser(user.Id);

                if (user == Context.User)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "You don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to vcmute yourself.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{Context.User}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                await Context.Message.DeleteAsync();

                if (user.GuildPermissions.ManageMessages)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = "I do not have permission to vcmute a moderator.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                if (vc == null)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "User not in voice channel!",
                        Description = $"User needs to be in a voice channel.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                else
                {
                    if (vc.GetUser(user.Id).IsMuted)
                    {
                        await Context.Channel.TriggerTypingAsync();
                        await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                        {
                            Color = Color.LightOrange,
                            Title = "User already muted!",
                            Description = $"This user is already muted.",
                            Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                                Text = $"{user.Username}#{user.Discriminator}"
                            },
                        }.WithCurrentTimestamp().Build());

                        return;
                    }

                    else
                    {
                        await vc.GetUser(user.Id).ModifyAsync(x => x.Mute = true);
                        AddModlogs(user.Id, Action.VoiceMuted, Context.Message.Author.Id, reason, Context.Guild.Id);
                        EmbedBuilder eb = new EmbedBuilder()
                        {
                            Title = $"***{user.Username} has been voice chat muted***",
                            Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                                Text = $"{user.Username}#{user.Discriminator}"
                            },
                            Description = $"{user} has been muted at {DateTime.Now}\nReason: {reason}",
                            Color = Color.Orange
                        };
                        eb.WithCurrentTimestamp();
                        await Context.Channel.TriggerTypingAsync();
                        await Context.Channel.SendMessageAsync("", false, eb.Build());

                        return;
                    }
                }
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }
        }

        [Command("vcunmute"), Summary("Unmutes a user from voice channels"), Remarks("(PREFIX)vcunmute <user>"), Alias("(PREFIX)vcunmute")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.MuteMembers)]
        public async Task VcUnMute(SocketUser user)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.MuteMembers || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                SocketVoiceChannel vc = Context.Guild.GetUser(user.Id).VoiceChannel;

                if (vc.GetUser(user.Id).IsMuted)
                {
                    if (vc == null)
                    {
                        await Context.Message.DeleteAsync();
                        await Context.Channel.TriggerTypingAsync();
                        await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                        {
                            Color = Color.LightOrange,
                            Title = "User not in voice channel!",
                            Description = $"User needs to be in a voice channel.",
                            Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                                Text = $"{user.Username}#{user.Discriminator}"
                            },
                        }.WithCurrentTimestamp().Build());

                        return;
                    }

                    else
                    {
                        await Context.Message.DeleteAsync();
                        await vc.GetUser(user.Id).ModifyAsync(x => x.Mute = false);
                        EmbedBuilder eb = new EmbedBuilder()
                        {
                            Title = $"***{user.Username} has been voice chat unmuted***",
                            Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                                Text = $"{user.Username}#{user.Discriminator}"
                            },
                            Description = $"{user} has been unmuted at {DateTime.Now}",
                            Color = Color.Orange
                        };
                        eb.WithCurrentTimestamp();
                        await Context.Channel.TriggerTypingAsync();
                        await Context.Channel.SendMessageAsync("", false, eb.Build());

                        return;
                    }
                }

                else
                {
                    await Context.Message.DeleteAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "User not muted!",
                        Description = $"This user is not currently muted.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }
        }

        [Command("Warn"), Summary("Warns a user"), Remarks("(PREFIX)warn <user> (optional)<reason>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        public async Task Warn(SocketUser user, [Remainder] string reason = "No reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageMessages || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                SocketGuildUser User = Context.Guild.GetUser(user.Id);

                if (user == Context.User)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "You don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to warn yourself.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{Context.User}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                else if (User.GuildPermissions.ManageMessages)
                {
                    await Context.Message.DeleteAsync();
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = "I do not have permission to warn a moderator.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                else
                {
                    await Context.Message.DeleteAsync();
                    AddModlogs(user.Id, Action.Warned, Context.Message.Author.Id, reason, Context.Guild.Id);
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Title = $"***{user.Username} has been warned***",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                        Description = $"{user} has been warned at {DateTime.Now}\nReason: {reason}",
                        Color = Color.Orange
                    };
                    eb.WithCurrentTimestamp();
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, eb.Build());

                    return;
                }
            }

            else
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());

                return;
            }
        }

        [Command("mute"), Summary("Mutes a user and stops them from talking in text channels"), Remarks("(PREFIX)mute <user> (optional)<reason>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Mute(SocketGuildUser user, [Remainder] string reason = "No reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.ManageRoles && !Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }

            else
            {
                if (user == Context.User)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "You don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to mute yourself.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{Context.User}"
                        },
                    }.Build());

                    return;
                }

                if (user.GuildPermissions.ManageMessages)
                {
                    await Context.Message.DeleteAsync();
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = "I do not have permission to mute a moderator.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                IRole role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");

                if (role == null)
                {
                    role = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false), null, false, null);
                }

                if (role.Position > Context.Guild.CurrentUser.Hierarchy)
                {
                    await Context.Message.DeleteAsync();
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = $"Sorry, but the role has a higher hierarchy than me.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                if (user.Roles.Contains(role))
                {
                    await Context.Message.DeleteAsync();
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "User is already muted!",
                        Description = $"{user} is already muted.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                try
                {
                    await role.ModifyAsync(x => x.Position = Context.Guild.CurrentUser.Hierarchy);

                    foreach (var channel in Context.Guild.TextChannels)
                    {
                        if (!channel.GetPermissionOverwrite(role).HasValue || channel.GetPermissionOverwrite(role).Value.SendMessages == PermValue.Allow)
                        {
                            await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                        }
                    }
                }

                catch { }

                MySqlConnection QueryConn = new MySqlConnection(Global.MySQL.ConnStr);

                try
                {
                    QueryConn.Open();
                    await MuteService.InsertToDBAsync(3, QueryConn, user.Id, Context.Guild.Id, 0, 0, (SocketTextChannel)Context.Channel);
                    QueryConn.Close();
                }

                catch (Exception ex)
                {
                    Global.ConsoleLog(ex.Message);
                }

                await Context.Message.DeleteAsync();
                await user.AddRoleAsync(role);
                AddModlogs(user.Id, Action.Muted, Context.Message.Author.Id, reason, Context.Guild.Id);
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.Green,
                    Title = $"Muted user {user}!",
                    Description = $"{user} has been muted at {DateTime.Now}\nReason: {reason}",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{user.Username}#{user.Discriminator}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("unmute"), Summary("Unmutes a muted user"), Remarks("(PREFIX)unmute <user>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        public async Task unmute([Remainder] SocketGuildUser user)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.ManageRoles && !Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.Build());
            }

            else
            {
                IRole role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");
                await Context.Message.DeleteAsync();

                if (role == null)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "Role does not exist!",
                        Description = $"The muted role does not exist to unmute a member.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                if (role.Position > Context.Guild.CurrentUser.Hierarchy)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = "This role has a higher hierarchy than me.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                if (user.Roles.Contains(role))
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.Green,
                        Title = $"Unmuted user {user}!",
                        Description = $"{user} has been successfully unmuted.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());
                    await user.RemoveRoleAsync(role);
                    MySqlConnection Queryconn = new MySqlConnection(Global.MySQL.ConnStr);
                    Queryconn.Open();
                    await MuteService.InsertToDBAsync(4, Queryconn, user.Id, Context.Guild.Id, 0, 0, (SocketTextChannel)Context.Channel);
                    Queryconn.Close();

                    return;
                }

                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = $"{user} is not muted!",
                    Description = $"{user} is not currently muted.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{user.Username}#{user.Discriminator}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("censor"), Summary("Adds a word to the guild blacklist, meaning members can't say it."), Remarks("(PREFIX)blacklist <phrase>"), Alias("bl", "blacklist")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        public async Task Blacklist([Remainder] string phrase)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageMessages || Global.DevUIDs.Contains(Context.Message.Author.Id))
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

                try
                {
                    string itemVal = guild?.GetValue("blacklistedterms").ToJson();
                    List<string> stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

                    if (re.IsMatch(phrase))
                    {
                        EmbedBuilder errembed = new EmbedBuilder();
                        errembed.WithTitle("Error");
                        errembed.WithDescription("This word is already included in the censor list!");
                        errembed.WithColor(Color.Red);
                        errembed.WithAuthor(Context.Message.Author);
                        await Context.Message.ReplyAsync("", false, errembed.Build());
                        return;
                    }
                }

                catch { }

                if (guild == null)
                {
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "blacklistedterms", phrase } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Push("blacklistedterms", phrase));
                }

                await Context.Message.DeleteAsync();
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Blacklist updated!");
                embed.WithDescription("Successfully added word to the word blacklist!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.Channel.SendMessageAsync("", false, embed.Build());
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
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("clearcensored"), Summary("Clears the guild word blacklist"), Remarks("(PREFIX)clearblacklist"), Alias("blclear", "clearblacklist", "clearcensor")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Blclear()
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageMessages || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                collection.UpdateMany(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Unset("blacklistedterms"));
                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription("Successfully cleared the censor list!");
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
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("uncensor"), Summary("Removes a word from the guild censor list."), Remarks("(PREFIX)blacklist <phrase>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Whitelist([Remainder] string phrase)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageMessages || Global.DevUIDs.Contains(Context.Message.Author.Id))
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
                int items = 0;

                try
                {
                    string itemVal = guild?.GetValue("blacklistedterms").ToJson();
                    List<string> stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();
                    items = stringArray.Count();
                    await ReplyAsync($"{items}");
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

                    if (re.IsMatch(phrase))
                    {
                        collection.UpdateMany(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Pull("blacklistedterms", phrase));
                    }

                    else
                    {
                        EmbedBuilder errembed = new EmbedBuilder();
                        errembed.WithTitle("Error");
                        errembed.WithDescription("This word is not included in the censor list!");
                        errembed.WithColor(Color.Red);
                        errembed.WithAuthor(Context.Message.Author);
                        await Context.Message.ReplyAsync("", false, errembed.Build());
                        return;
                    }
                }

                catch { }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Blacklist updated!");
                embed.WithDescription("Successfully removed word from the blacklist!");
                embed.WithColor(Color.Green);
                embed.WithAuthor(Context.Message.Author);
                embed.WithCurrentTimestamp();
                await Context.Message.Channel.SendMessageAsync("", false, embed.Build());

                if (items >= 1) //This simply means that if there was 1 or less elements in the array at the time of counting, there will be 0 at this point so we can just remove the array to save storage & because it fixes lots of silly bugs with matching.
                {
                    collection.UpdateMany(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Unset("blacklistedterms"));
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
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("getcensors"), Summary("Gets the list of censored terms in a guild."), Remarks("(PREFIX)getcensors"), Alias("censors", "getguildcensors", "censoredlist", "censoredterms", "getcensoredterms", "censored", "censorlist", "censor_list")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task GetCensoredTerms()
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument data = await MongoHandler.FindById(collection, _id);
                string blacklistedItems = "";

                try
                {
                    blacklistedItems += $"{data.GetElement("blacklistedterms").Value}";
                    EmbedBuilder embed = new EmbedBuilder();
                    embed.WithTitle("Guild blacklist!");
                    embed.AddField("Items:", $"{Regex.Replace(blacklistedItems, @"[\]\[]", "")}");
                    embed.WithColor(Color.Green);
                    embed.WithAuthor(Context.Message.Author);
                    embed.WithCurrentTimestamp();
                    await Context.Message.Channel.SendMessageAsync("", false, embed.Build());
                }

                catch (KeyNotFoundException)
                {
                    EmbedBuilder errembed = new EmbedBuilder();
                    errembed.WithTitle("Error");
                    errembed.WithDescription("There are no items in the censor list!");
                    errembed.WithColor(Color.Red);
                    errembed.WithAuthor(Context.Message.Author);
                    await Context.Message.ReplyAsync("", false, errembed.Build());
                    return;
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
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("disablelinks"), Summary("enables and disables users being able to send links"), Remarks("(PREFIX)disablelinks <on/true/false/off>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task DisableLinks(string onoroff)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                bool enabled = false;

                if (onoroff == "true" || onoroff == "on")
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
                    BsonDocument document = new BsonDocument { { "_id", (decimal)_id }, { "disablelinks", enabled } };
                    collection.InsertOne(document);
                }

                else
                {
                    collection.UpdateOne(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("disablelinks", enabled));
                }

                EmbedBuilder embed = new EmbedBuilder();
                embed.WithTitle("Success");
                embed.WithDescription("Users are no longer able to send links!");
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
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        /*
         * This is boilerplaate code for python module
         */
        [Command("ModLogs"), Summary("Gets the modlogs for the current user"), Remarks("(PREFIX)modlogs <user>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages | ChannelPermission.AddReactions)]
        public Task Modlogs(params string[] arg)
        {
            return Task.CompletedTask;
        }

        [Command("tempmute", RunMode = RunMode.Async), Summary("Reminds you with a custom message (In Seconds)"), Remarks("(PREFIX)tempmute <seconds> <message>"), Alias("tm")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task Remind(SocketGuildUser user, string duration, [Remainder] string reason = "No reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.ManageRoles && !Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "You don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }

            else
            {
                if (user == Context.User)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "You don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to mute yourself.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{Context.User}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                await Context.Message.DeleteAsync();

                if (user.GuildPermissions.ManageMessages)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = "I do not have permission to mute a moderator.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                IRole role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");

                if (role == null)
                {
                    role = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false), null, false, null);
                }

                if (role.Position > Context.Guild.CurrentUser.Hierarchy)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = "This role has a higher hierarchy than me.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                if (user.Roles.Contains(role))
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "User is already muted!",
                        Description = $"{user} is already muted.",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                    }.WithCurrentTimestamp().Build());

                    return;
                }

                try
                {
                    await role.ModifyAsync(x => x.Position = Context.Guild.CurrentUser.Hierarchy);

                    foreach (SocketTextChannel channel in Context.Guild.TextChannels)
                    {
                        if (!channel.GetPermissionOverwrite(role).HasValue || channel.GetPermissionOverwrite(role).Value.SendMessages == PermValue.Allow)
                        {
                            await channel.AddPermissionOverwriteAsync(role, new OverwritePermissions(sendMessages: PermValue.Deny));
                        }
                    }
                }

                catch { }

                await user.AddRoleAsync(role);
                AddModlogs(user.Id, Action.TempMuted, Context.Message.Author.Id, reason, Context.Guild.Id);
                await MuteService.SetMute(Context.Guild, user, (SocketTextChannel)Context.Channel, DateTime.Now, duration, reason, (ShardedCommandContext)Context);
                await Context.Channel.TriggerTypingAsync();
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder()
                {
                    Color = Color.Orange,
                    Title = $"Muted user {user}!",
                    Description = $"{user} has been successfully muted for {duration}\nreason: {reason}",
                    Footer = new EmbedFooterBuilder()
                    {
                        IconUrl = user.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{user.Username}#{user.Discriminator}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        [Command("copyemote"), Summary("Allows you to copy an emote from elsewhere to the guild"), Remarks("(PREFIX)copyemote <emote_id> <guild_id> <new_name> OR (PREFIX)copyemote <emote_url> <new_name>"), Alias("copyemoji", "stealemote", "stealemoji")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        [RequireBotPermission(GuildPermission.ManageEmojisAndStickers)]
        public async Task EMDEL(params string[] args)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageEmojisAndStickers || Global.DevUIDs.Contains(Context.Message.Author.Id))
            {
                string emoji = args[0];
                string name = "";
                ulong guildId = 0;

                if (args.Length == 2)
                {
                    name = args[1];
                }

                else if (args.Length == 3)
                {
                    guildId = Convert.ToUInt64(args[1]);
                    name = args[2];
                }

                else
                {
                    await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "Please use the syntax: `copyemote <emote_name> <guildId> <new_emote_name>` or `copyemote <emote_url> <new_emoji_name>`", false, Color.Red).Build());
                    return;
                }

                Regex r = new Regex(@"(?i)(https:\/\/)|cdn.discordapp.com\/emojis\/(.*?)\.");

                if (r.IsMatch(emoji))
                {
                    if (!emoji.StartsWith("https://") && !emoji.StartsWith("http"))
                    {
                        emoji = $"https://{emoji}";
                    }

                    WebClient webClient = new WebClient();
                    byte[] imageBytes = webClient.DownloadData(new Uri(emoji));
                    MemoryStream ms = new MemoryStream(imageBytes);
                    GuildEmote ae = await Context.Guild.CreateEmoteAsync(name, new Discord.Image(ms));
                    await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Successfully copied emoji", $"Successfully added the emote {name} to the guild.", false, Color.Green).Build());

                    return;
                }

                else
                {
                    if (guildId != 0)
                    {
                        SocketGuild guild = _client.GetGuild(guildId);
                        GuildEmote emote = await GetEmote(emoji, guild);

                        if (emote == null)
                        {
                            await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", $"Please make sure the bot is in the guild where you're trying to copy the emote from and you've selected the right name.", false, Discord.Color.Red).Build());
                            return;
                        }

                        WebClient wc = new WebClient();
                        MemoryStream ms = new MemoryStream(await wc.DownloadDataTaskAsync(emote.Url));
                        GuildEmote ae = await Context.Guild.CreateEmoteAsync(emote.Name, new Discord.Image(ms));
                        await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Successfully copied emote", $"Successfully added the emote {name} to the guild.", false, Discord.Color.Green).Build());
                        await Task.Delay(200);
                        await ms.DisposeAsync();

                        return;
                    }
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
                        IconUrl = Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl(),
                        Text = $"{Context.User}"
                    },
                }.WithCurrentTimestamp().Build());
            }
        }

        /// <summary>
        /// Gets an emote from a guild.
        /// </summary>
        /// <param name="name">The name of the emote to find.</param>
        /// <param name="Guild">The guild to get the emote from.</param>
        /// <returns>A guild emote.</returns>
        public async Task<GuildEmote> GetEmote(string name, SocketGuild Guild = null)
        {
            name = name.Replace(":", "").Replace("<", "").Replace(">", "").Replace(":", "");

            if (Guild.Emotes.Any(x => string.Equals(name, x.Name, StringComparison.CurrentCultureIgnoreCase)))
            {
                return Guild.Emotes.First(x => string.Equals(name, x.Name, StringComparison.CurrentCultureIgnoreCase));
            }

            try
            {
                ulong resultString = ulong.Parse(Regex.Match(name, @"\d+").Value);

                if (resultString == 0 || await Guild.GetEmoteAsync(resultString) == null)
                {
                    return null;
                }

                return await Guild.GetEmoteAsync(resultString);
            }
            catch
            {
                return null;
            }
        }

        [Command("accept"), Summary("Accepts a suggestion"), Remarks("(PREFIX)accept <suggestion ID> <reason>"), Alias("approve")]
        public async Task Approve(/*ulong suggestionId = 0, [Remainder] string reason = "no reason provided"*/params string[] args)
        {
            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

            if (!UserCheck.GuildPermissions.ManageMessages && !Global.DevUIDs.Contains(Context.User.Id))
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "You do not have access to execute this command!", false, Color.Red).Build());
                return;
            }

            string suggestionschannelid = await Global.DetermineSuggestionChannel(Context);

            if (suggestionschannelid == "0")
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "There is no configured suggestions channel.", false, Color.Red).Build());
                return;
            }

            ulong suggestion;
            bool canConvert = ulong.TryParse(args[0], out suggestion);

            if (canConvert == false)
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "Please provide a Suggestion Id!", false, Color.Red).Build());
                return;
            }

            if (suggestion.ToString().Length != 18)
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "Please provide a valid message Id!", false, Color.Red).Build());
                return;
            }

            string reason = "No reason provided.";

            if (args.Length >= 1)
            {
                uint count = 0;
                reason = "";

                foreach (string s in args)
                {
                    if (count == 0)
                    {
                        count++;
                        continue;
                    }

                    reason += " " + s;
                }

                reason = reason.Remove(0, 1);
            }

            SocketTextChannel suggestionschannel = Context.Guild.GetTextChannel(Convert.ToUInt64(suggestionschannelid));
            IMessage msg = await suggestionschannel.GetMessageAsync(suggestion);
            IUserMessage getMessage = (IUserMessage)msg;
            IEmbed getEmbed = getMessage.Embeds.First();

            if (getEmbed.ToEmbedBuilder().Author.Name.ToString() == "Denied")
            {
                await Context.Message.Channel.SendMessageAsync("Sorry, but someone has already denied this suggestion.");
                return;
            }

            if (getEmbed.ToEmbedBuilder().Author.Name.ToString() == "Accepted")
            {
                await Context.Message.Channel.SendMessageAsync("Sorry, but someone has already accepted this suggestion.");
                return;
            }

            await Context.Message.Channel.SendMessageAsync($"Accepted suggestion {suggestion} with reason \"{reason}\"");
            Embed modifyEmbed = getEmbed.ToEmbedBuilder().WithAuthor("Accepted", "https://cdn.discordapp.com/emojis/787034785583333426.png?v=1").AddField("Reason", reason).WithColor(Color.Green).Build();
            await getMessage.ModifyAsync(x => x.Embed = modifyEmbed);
            EmbedBuilder embed = modifyEmbed.ToEmbedBuilder();
            await getMessage.RemoveAllReactionsAsync();
        }

        [Command("deny"), Summary("Denies a suggestion"), Remarks("(PREFIX)deny <suggestion ID> <reason>")]
        public async Task Deny(/*ulong suggestionId = 0, [Remainder] string reason = "No reason provided"*/ params string[] args)
        {
            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

            if (!UserCheck.GuildPermissions.ManageMessages && !Global.DevUIDs.Contains(Context.User.Id))
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "You do not have access to execute this command!", false, Color.Red).Build());
                return;
            }

            string suggestionschannelid = await Global.DetermineSuggestionChannel(Context);

            if (suggestionschannelid == "0")
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "There is no configured suggestions channel.", false, Color.Red).Build());
                return;
            }

            ulong suggestion;
            bool canConvert = ulong.TryParse(args[0], out suggestion);

            if (canConvert == false)
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "Please provide a Suggestion Id!", false, Color.Red).Build());
                return;
            }

            if (suggestion.ToString().Length != 18)
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "Please provide a valid message Id!", false, Color.Red).Build());
                return;
            }

            string reason = "No reason provided.";

            if (args.Length >= 1)
            {
                uint count = 0;
                reason = "";

                foreach (string s in args)
                {
                    if (count == 0)
                    {
                        count++;
                        continue;
                    }

                    reason += " " + s;
                }

                reason = reason.Remove(0, 1);
            }

            SocketTextChannel suggestionschannel = Context.Guild.GetTextChannel(Convert.ToUInt64(suggestionschannelid));
            IMessage msg = await suggestionschannel.GetMessageAsync(suggestion);
            IUserMessage getMessage = (IUserMessage)msg;
            IEmbed getEmbed = getMessage.Embeds.First();

            if (getEmbed.ToEmbedBuilder().Author.Name.ToString() == "Denied")
            {
                await Context.Message.Channel.SendMessageAsync("Sorry, but someone has already denied this suggestion.");
                return;
            }

            if (getEmbed.ToEmbedBuilder().Author.Name.ToString() == "Accepted")
            {
                await Context.Message.Channel.SendMessageAsync("Sorry, but someone has already accepted this suggestion.");
                return;
            }

            await Context.Message.Channel.SendMessageAsync($"Denied suggestion {suggestion} with reason \"{reason}\"");
            Embed modifyEmbed = getEmbed.ToEmbedBuilder().WithAuthor("Denied", "https://cdn.discordapp.com/emojis/787035973287542854.png?v=1").AddField("Reason", reason).WithColor(Color.Red).Build();
            await getMessage.ModifyAsync(x => x.Embed = modifyEmbed);
            EmbedBuilder embed = modifyEmbed.ToEmbedBuilder();
            await getMessage.RemoveAllReactionsAsync();
        }

        [Command("Nick"), Summary("Sets a users nickname."), Remarks("(PREFIX)nick <user <new nickname>/<reset>")]
        [RequireBotPermission(GuildPermission.ManageNicknames | GuildPermission.ChangeNickname)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Nickname(SocketGuildUser user, [Remainder] string nick)
        {
            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

            if (!UserCheck.GuildPermissions.ManageMessages && !Global.DevUIDs.Contains(Context.User.Id))
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "You do not have access to execute this command!", false, Color.Red).Build());
                return;
            }

            if(nick.Length > 32)
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "You can only set a nickname with a max length of 32 characters.", false, Color.Orange).Build());
                return;
            }

            if (user.Hierarchy >= Context.Guild.CurrentUser.Hierarchy && user != Context.Guild.CurrentUser)
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "This user sits at a higher or congruent hierarchy than me.", false, Color.Orange).Build());
                return;
            }

            try
            {
                string oldNick = user.Nickname == null ? user.Username : user.Nickname;
                await user.ModifyAsync(u =>
                {
                    if (nick.ToLower() == "reset")
                    {
                        u.Nickname = null;
                    }
                    
                    else
                    {
                        u.Nickname = nick;
                    }
                });

                if (user.Nickname != null)
                {
                    await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Success!", $"Successfully changed the users nickname from '{oldNick}' to '{user.Nickname}'.", false, Color.Green).Build());
                }

                else
                {
                    await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Success!", $"Successfully reset {user.Username}'s nickname.", false, Color.Green).Build());
                }
            }

            catch 
            {
                await Context.Message.ReplyAsync("", false, Global.EmbedMessage("Error", "The bot encountered an issue trying to change the users nickname. Please check the bots hierarchy/permissions.", false, Color.Red).Build());
                return;
            }
        }
    }
}   