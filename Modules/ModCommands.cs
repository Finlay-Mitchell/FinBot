using Discord;
using Discord.Commands;
using Discord.Rest;
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

namespace FinBot.Modules
{
    public class ModCommands : ModuleBase<SocketCommandContext>
    {
        public readonly DiscordShardedClient _client;

        public ModCommands(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();
        }

        [Command("clear"), Summary("clears a specified amount of messages from the chat"), Remarks("(PREFIX) clear<amount>"), Alias("purge", "clr")]
        public async Task Purge(uint amount)
        {
            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

            if (!UserCheck.GuildPermissions.ManageMessages)
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

            else
            {
                IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync((int)amount + 1).FlattenAsync();
                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
                await Context.Channel.TriggerTypingAsync();
                IUserMessage msg = await Context.Message.Channel.SendMessageAsync($"Purge completed!");
                await Task.Delay(2000);
                await msg.DeleteAsync();
            }
        }

        [Command("slowmode"), Summary("sets the slowmode of the current chat"), Remarks("(PREFIX)slowmode <time in seconds | \"spam\" for 15 seconds | \"simp\" for 7 seconds>"), Alias("slow")]
        public async Task Slowmode([Remainder] string value)
        {
            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

            if (UserCheck.GuildPermissions.ManageChannels)
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
                            Author = new EmbedAuthorBuilder()
                            {
                                Name = Context.Message.Author.ToString(),
                                IconUrl = Context.Message.Author.GetAvatarUrl(),
                                Url = Context.Message.GetJumpUrl()
                            }
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
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

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

        public async void AddModlogs(ulong userID, Action action, ulong ModeratorID, string reason, ulong GuildId)
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);

                try
                {
                    MySqlConnection queryConn = new MySqlConnection(Global.MySQL.ConnStr);
                    conn.Open();
                    MySqlCommand query = new MySqlCommand($"SELECT * FROM modlogs WHERE guildId = {GuildId} AND userId = {userID}", conn);
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

//                            SocketUser user = guild.GetUser(userID);
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
                    queryConn.Open();
                    AddToModlogs(queryConn, userID, action, ModeratorID, reason, GuildId, indx);
                    queryConn.Close();
                }

                catch (Exception ex)
                {
                    Global.ConsoleLog(ex.Message);

                    //do stuffs
                }

                finally
                {
                    conn.Close();
                }
            }

            catch
            {
                // do more stuffs
            }
        }

        public enum Action
        {
            Warned,
            Kicked,
            Banned,
            Muted,
            VoiceMuted,
            TempMuted
        }

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
        public async Task Clearwarn(string user1 = null, int number = 999)
        {
            SocketGuildUser user = Context.User as SocketGuildUser;
            IReadOnlyCollection<SocketUser> mentions = Context.Message.MentionedUsers;

            if (!user.GuildPermissions.Administrator)
            {
                await Context.Message.ReplyAsync("", false, new Discord.EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
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
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                        },
                        Title = $"Successfully cleared log for **{u.Username}**",
                        Color = Color.DarkMagenta,
                        Description = $"Log for {u.Username} cleared.",
                        Fields = new List<EmbedFieldBuilder>()
                    };
                    await Context.Message.ReplyAsync("", false, b.Build());
                }

                catch (Exception ex)
                {
                    Global.ConsoleLog(ex.Message);

                    // do stuff
                }

                finally
                {
                    conn.Close();
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);

                // do stuff
            }
        }

        [Command("ClearAllModLogs"), Summary("Clears all logs for a user"), Remarks("(PREFIX)ClearAllModLogs <user>"), Alias("clearalllogs", "cal", "caml")]
        public async Task ClearAllModLogs(string user1 = null)
        {
            SocketGuildUser user = Context.User as SocketGuildUser;
            IReadOnlyCollection<SocketUser> mentions = Context.Message.MentionedUsers;

            if (!user.GuildPermissions.Administrator)
            {
                await Context.Message.ReplyAsync("", false, new Discord.EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
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
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
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

                    //do stuffs
                }

                finally
                {
                    conn.Close();
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);

                //do stuffs
            }
        }

        [Command("ban"), Summary("bans user from the guild"), Remarks("(PREFIX)ban <user> (optional)prune days (optional)reason")]
        public async Task BanUser(IGuildUser user, [Remainder] string reason = "No reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.BanMembers)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());

                return;
            }

            if (user.GuildPermissions.Administrator)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "I don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to ban an administrator.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());

                return;
            }

            await Context.Message.DeleteAsync();

            try
            {
                await user.SendMessageAsync($"You've been banned from {Context.Guild}.\nReason: {reason}\nTime of ban: {DateTime.Now}.");
            }

            catch (Exception)
            {
                await Context.Message.Channel.SendMessageAsync($"Could not send message to {user}.");
            }

            await user.BanAsync(0, $"{reason} by {Context.Message.Author}");
            await Context.Guild.AddBanAsync(user, 0, reason);
            AddModlogs(user.Id, Action.Banned, Context.Message.Author.Id, reason, Context.Guild.Id);
            SocketGuildUser arg = Context.Guild.GetUser(Context.Message.Author.Id);
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = $"***{user.Username} has been banned***",
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = arg.GetAvatarUrl(),
                    Text = $"{arg.Username}#{arg.Discriminator}"
                },
                Description = $"{user} has been banned at {DateTime.Now}\n Reason: {reason}.",
                ThumbnailUrl = Global.BanMessageURL,
                Color = Color.DarkRed
            };
            eb.WithCurrentTimestamp();
            await Context.Channel.TriggerTypingAsync();
            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("kick"), Summary("kicks member from the guild"), Remarks("(PREFIX)kick <user> (optional)<reason>")]
        public async Task KickUser(IGuildUser user, [Remainder] string reason = "no reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.KickMembers)
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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());

                return;
            }

            if (user.GuildPermissions.Administrator)
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.LightOrange,
                    Title = "I don't have Permission!",
                    Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to kick an administrator.",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());

                return;
            }

            try
            {
                await user.SendMessageAsync($"You've been kicked from {Context.Guild}.\nReason: {reason}\nTime of kick: {DateTime.Now}.");
            }

            catch (Exception)
            {
                await Context.Message.Channel.SendMessageAsync($"Could not send kick message to {user}.");
            }

            await Context.Message.DeleteAsync();
            await user.KickAsync($"{reason} by {Context.Message.Author}");
            AddModlogs(user.Id, Action.Kicked, Context.Message.Author.Id, reason, Context.Guild.Id);
            await Context.Channel.TriggerTypingAsync();
            SocketGuildUser arg = Context.Guild.GetUser(Context.Message.Author.Id);
            EmbedBuilder eb = new EmbedBuilder()
            {
                Title = $"***{user.Username} has been kicked***",
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = arg.GetAvatarUrl(),
                    Text = $"{arg.Username}#{arg.Discriminator}"
                },
                Description = $"{user} has been kicked by {GuildUser} at {DateTime.Now}\n Reason: {reason}.",
                ThumbnailUrl = Global.KickMessageURL,
                Color = Color.Orange
            };
            eb.WithCurrentTimestamp()
           .Build();
            await Context.Channel.SendMessageAsync("", false, eb.Build());
        }

        [Command("vcmute"), Summary("Mutes a user from voice channels"), Remarks("(PREFIX)vcmute <user> (optional) <user>"), Alias("voicechatmute")]
        public async Task VcMute(SocketUser user, [Remainder] string reason = "No reason provided")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);
            SocketVoiceChannel vc = Context.Guild.GetUser(user.Id).VoiceChannel;
            SocketGuildUser User = Context.Guild.GetUser(user.Id);

            if (GuildUser.GuildPermissions.DeafenMembers)
            {
                if (user == Context.User)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "You don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to vcmute yourself.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                if (User.GuildPermissions.Administrator)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to vcmute an administrator.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                if (vc == null)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "User not in voice channel!",
                        Description = $"User needs to be in a voice channel.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                else
                {
                    if (vc.GetUser(user.Id).IsMuted)
                    {
                        await Context.Channel.TriggerTypingAsync();
                        await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                        {
                            Color = Color.LightOrange,
                            Title = "User already muted!",
                            Description = $"This user is already muted.",
                            Author = new EmbedAuthorBuilder()
                            {
                                Name = Context.Message.Author.ToString(),
                                IconUrl = Context.Message.Author.GetAvatarUrl(),
                                Url = Context.Message.GetJumpUrl()
                            }
                        }.Build());

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
                                IconUrl = user.GetAvatarUrl(),
                                Text = $"{user.Username}#{user.Discriminator}"
                            },
                            Description = $"{user} has been muted by {Context.User} at {DateTime.Now}",
                            ThumbnailUrl = Global.KickMessageURL,
                            Color = Color.Orange
                        };
                        eb.WithCurrentTimestamp();
                        await Context.Channel.TriggerTypingAsync();
                        await Context.Message.ReplyAsync("", false, eb.Build());

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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());

                return;
            }
        }

        [Command("vcunmute"), Summary("Unmutes a user from voice channels"), Remarks("(PREFIX)vcunmute <user>"), Alias("(PREFIX)vcunmute")]
        public async Task VcUnMute(SocketUser user)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.MuteMembers)
            {
                SocketVoiceChannel vc = Context.Guild.GetUser(user.Id).VoiceChannel;

                if (vc.GetUser(user.Id).IsMuted)
                {
                    if (vc == null)
                    {
                        await Context.Channel.TriggerTypingAsync();
                        await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                        {
                            Color = Color.LightOrange,
                            Title = "User not in voice channel!",
                            Description = $"User needs to be in a voice channel.",
                            Author = new EmbedAuthorBuilder()
                            {
                                Name = Context.Message.Author.ToString(),
                                IconUrl = Context.Message.Author.GetAvatarUrl(),
                                Url = Context.Message.GetJumpUrl()
                            }
                        }.Build());

                        return;
                    }

                    else
                    {
                        await vc.GetUser(user.Id).ModifyAsync(x => x.Mute = false);
                        EmbedBuilder eb = new EmbedBuilder()
                        {
                            Title = $"***{user.Username} has been voice chat unmuted***",
                            Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = user.GetAvatarUrl(),
                                Text = $"{user.Username}#{user.Discriminator}"
                            },
                            Description = $"{user} has been unmuted by {Context.User} at {DateTime.Now}",
                            ThumbnailUrl = Global.KickMessageURL,
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
                    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "User not muted!",
                        Description = $"This user is not currently muted.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());

                return;
            }
        }

        [Command("Warn"), Summary("Warns a user"), Remarks("(PREFIX)warn <user> (optional)<reason>")]
        public async Task Warn(SocketUser user, [Remainder] string reason = "No reason provded")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);
            SocketGuildUser User = Context.Guild.GetUser(user.Id);

            if (GuildUser.GuildPermissions.ManageChannels)
            {
                if (user == Context.User)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "You don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to warn yourself.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                else if (User.GuildPermissions.Administrator)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to warn an administrator.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                else
                {
                    AddModlogs(user.Id, Action.Warned, Context.Message.Author.Id, reason, Context.Guild.Id);
                    EmbedBuilder eb = new EmbedBuilder()
                    {
                        Title = $"***{user.Username} has been warned***",
                        Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = user.GetAvatarUrl(),
                            Text = $"{user.Username}#{user.Discriminator}"
                        },
                        Description = $"{user} has been warned by {Context.User} at {DateTime.Now}\n Reason: {reason}.",
                        ThumbnailUrl = Global.KickMessageURL,
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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());

                return;
            }
        }

        [Command("mute"), Summary("Mutes a user and stops them from talking in text channels"), Remarks("(PREFIX)mute <user> (optional)<reason>")]
        public async Task Mute(SocketGuildUser user, [Remainder] string reason = null)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.ManageRoles)
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
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                if (user.GuildPermissions.Administrator)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to mute an administrator.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

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
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but the role has a higher hierarchy than me.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                if (user.Roles.Contains(role))
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "User is already muted!",
                        Description = $"{user} is already muted.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

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

                await user.AddRoleAsync(role);
                AddModlogs(user.Id, Action.Muted, Context.Message.Author.Id, reason, Context.Guild.Id);
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.Green,
                    Title = $"Muted user {user}!",
                    Description = $"{user} has been successfully muted for reason: {reason}",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("unmute"), Summary("Unmutes a muted user"), Remarks("(PREFIX)unmute <user>")]
        public async Task unmute([Remainder] SocketGuildUser user)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.ManageRoles)
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

            else
            {
                IRole role = (Context.Guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");

                if (role == null)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "Role does not exist!",
                        Description = $"The muted role does not exist to unmute a member.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                if (role.Position > Context.Guild.CurrentUser.Hierarchy)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but the role has a higher hierarchy than me.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                if (user.Roles.Contains(role))
                {
                    await user.RemoveRoleAsync(role);
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.Green,
                        Title = $"Unmuted user {user}!",
                        Description = $"{user} has been successfully unmuted.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                else
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = $"{user} is not muted!",
                        Description = $"{user} is not currently muted.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());
                }
            }
        }

        [Command("censor"), Summary("Adds a word to the guild blacklist, meaning members can't say it."), Remarks("(PREFIX)blacklist <phrase>"), Alias("bl", "blacklist")]
        public async Task Blacklist([Remainder] string phrase)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageMessages)
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
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("clearcensored"), Summary("Clears the guild word blacklist"), Remarks("(PREFIX)clearblacklist"), Alias("blclear", "clearblacklist", "clearcensor")]
        public async Task Blclear()
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageMessages)
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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        [Command("uncensor"), Summary("Removes a word from the guild censor list."), Remarks("(PREFIX)blacklist <phrase>")]
        public async Task Whitelist([Remainder] string phrase)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageMessages)
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
                    Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

                    if (re.IsMatch(phrase))
                    {
                        collection.UpdateMany(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Pull("blacklistedterms", phrase));
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

        [Command("disablelinks"), Summary("enables and disables users being able to send links"), Remarks("(PREFIX)disablelinks <on/true/false/off>")]
        public async Task DisableLinks(string onoroff)
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (GuildUser.GuildPermissions.ManageChannels)
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
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }
        }

        //This is boilerplaate code for python module
        [Command("ModLogs"), Summary("Gets the modlogs for the current user"), Remarks("(PREFIX)modlogs <user>")]
        public Task Modlogs(params string[] arg)
        {
            return Task.CompletedTask;
        }

        [Command("remind", RunMode = RunMode.Async), Summary("Reminds you with a custom message (In Seconds)"), Remarks("(PREFIX)remain <seconds> <message>"), Alias("Timer")]
        public async Task Remind(SocketGuildUser user, string duration, [Remainder] string reason = "No reason provided.")
        {
            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

            if (!GuildUser.GuildPermissions.ManageRoles)
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
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                if (user.GuildPermissions.Administrator)
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to mute an administrator.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

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
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "I don't have Permission!",
                        Description = $"Sorry, {Context.Message.Author.Mention} but the role has a higher hierarchy than me.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

                    return;
                }

                if (user.Roles.Contains(role))
                {
                    await Context.Channel.TriggerTypingAsync();
                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                    {
                        Color = Color.LightOrange,
                        Title = "User is already muted!",
                        Description = $"{user} is already muted.",
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = Context.Message.Author.ToString(),
                            IconUrl = Context.Message.Author.GetAvatarUrl(),
                            Url = Context.Message.GetJumpUrl()
                        }
                    }.Build());

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

                await user.AddRoleAsync(role);
                AddModlogs(user.Id, Action.TempMuted, Context.Message.Author.Id, reason, Context.Guild.Id);
                await MuteService.SetMute(Context.Guild, user, (SocketTextChannel)Context.Channel, DateTime.Now, duration, reason, (ShardedCommandContext)Context);
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
                {
                    Color = Color.Green,
                    Title = $"Muted user {user}!",
                    Description = $"{user} has been successfully muted for {duration}\nreason: {reason}",
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = Context.Message.Author.ToString(),
                        IconUrl = Context.Message.Author.GetAvatarUrl(),
                        Url = Context.Message.GetJumpUrl()
                    }
                }.Build());
            }









































            await Context.Channel.TriggerTypingAsync();
        }
    }
}   