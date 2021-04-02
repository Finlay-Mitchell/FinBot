﻿//using Discord;
//using Discord.Commands;
//using Discord.Rest;
//using Discord.WebSocket;
//using FinBot.Handlers;
//using FinBot.Services;
//using System;
//using System.Collections.Generic;
//using System.Data.SQLite;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace FinBot.Modules
//{
//    public class ModCommands : ModuleBase<SocketCommandContext>
//    {
//        [Command("clear"), Summary("clears a specified amount of messages from the chat"), Remarks("(PREFIX) clear<amount>"), Alias("purge", "clr")]
//        public async Task Purge(uint amount)
//        {
//            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

//            if (!UserCheck.GuildPermissions.ManageMessages)
//            {
//                await Context.Channel.TriggerTypingAsync();
//                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//                {
//                    Color = Color.LightOrange,
//                    Title = "You don't have Permission!",
//                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                        Url = Context.Message.GetJumpUrl()
//                    }
//                }.Build());
//            }

//            else
//            {
//                IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync((int)amount + 1).FlattenAsync();
//                await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
//                await Context.Channel.TriggerTypingAsync();
//                IUserMessage msg = await Context.Message.Channel.SendMessageAsync($"Purge completed!");
//                await Task.Delay(2000);
//                await msg.DeleteAsync();
//            }
//        }

//        [Command("slowmode"), Summary("sets the slowmode of the current chat"), Remarks("(PREFIX)slowmode <time in seconds | \"spam\" for 15 seconds | \"simp\" for 7 seconds>"), Alias("slow")]
//        public async Task Slowmode([Remainder] string value)
//        {
//            SocketGuildUser UserCheck = Context.Guild.GetUser(Context.User.Id);

//            if (UserCheck.GuildPermissions.ManageChannels)
//            {
//                try
//                {
//                    int val = 0;

//                    try
//                    {
//                        val = Convert.ToInt32(value);
//                    }

//                    catch
//                    {
//                        if (value == "spam")
//                        {
//                            value = "15";
//                            val = 15;
//                        }

//                        else
//                        {
//                            value = "0";
//                            val = 0;
//                        }
//                    }

//                    if (val > 21600)
//                    {
//                        await Context.Channel.TriggerTypingAsync();
//                        await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                        {
//                            Color = Color.LightOrange,
//                            Title = "Slowmode interval to large!",
//                            Description = $"Sorry, {Context.Message.Author.Mention} but the max slowmode you can have is 21600 seconds (6 hours).",
//                            Author = new EmbedAuthorBuilder()
//                            {
//                                Name = Context.Message.Author.ToString(),
//                                IconUrl = Context.Message.Author.GetAvatarUrl(),
//                                Url = Context.Message.GetJumpUrl()
//                            }
//                        }.Build());
//                    }

//                    SocketTextChannel chan = Context.Guild.GetTextChannel(Context.Channel.Id);
//                    await chan.ModifyAsync(x =>
//                    {
//                        x.SlowModeInterval = val;
//                    });
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.Green,
//                        Title = $"Set the slowmode to {value}!",
//                        Description = $"{Context.Message.Author.Mention} successfully modified the slowmode of <#{Context.Channel.Id}> to {value} seconds!",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());
//                }

//                catch (Exception ex)
//                {
//                    Console.WriteLine(ex);
//                }

//            }

//            else
//            {
//                await Context.Channel.TriggerTypingAsync();
//                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                {
//                    Color = Color.LightOrange,
//                    Title = "You don't have Permission!",
//                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                        Url = Context.Message.GetJumpUrl()
//                    }
//                }.Build());
//            }
//        }

//        [Command("ban"), Summary("bans user from the guild"), Remarks("(PREFIX)ban <user> (optional)prune days (optional)reason")]
//        public async Task BanUser(IGuildUser user, [Remainder] string reason = "No reason provided.")
//        {
//            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

//            if (!GuildUser.GuildPermissions.BanMembers)
//            {
//                await Context.Channel.TriggerTypingAsync();
//                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                {
//                    Color = Color.LightOrange,
//                    Title = "You don't have Permission!",
//                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                        Url = Context.Message.GetJumpUrl()
//                    }
//                }.Build());
//            }

//            else
//            {
//                if (user == Context.User)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "You don't have Permission!",
//                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to ban yourself.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                    return;
//                }

//                if (user.GuildPermissions.Administrator)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "I don't have Permission!",
//                        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to ban an administrator.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                    return;
//                }

//                await Context.Message.DeleteAsync();

//                try
//                {
//                    await user.SendMessageAsync($"You've been banned from {Context.Guild}. Reason: {reason}, Time of ban: {DateTime.Now}.");
//                }

//                catch (Exception)
//                {
//                    await Context.Message.Channel.SendMessageAsync($"Could not send message to {user}.");
//                }

//                await user.BanAsync(0, $"{reason} by {Context.Message.Author}");
//                await Context.Guild.AddBanAsync(user, 0, reason);
//                AddModlogs(user.Id, Action.Banned, Context.Message.Author.Id, reason, Context.Guild.Id);
//                SocketGuildUser arg = Context.Guild.GetUser(Context.Message.Author.Id);
//                EmbedBuilder eb = new EmbedBuilder()
//                {
//                    Title = $"***{user.Username} has been banned***",
//                    Footer = new EmbedFooterBuilder()
//                    {
//                        IconUrl = arg.GetAvatarUrl(),
//                        Text = $"{arg.Username}#{arg.Discriminator}"
//                    },
//                    Description = $"{user} has been banned at {DateTime.Now}\n Reason: {reason}.",
//                    ThumbnailUrl = Global.BanMessageURL,
//                    Color = Color.DarkRed
//                };
//                eb.WithCurrentTimestamp();
//                await Context.Channel.TriggerTypingAsync();
//                await Context.Channel.SendMessageAsync("", false, eb.Build());
//            }
//        }

//        [Command("kick"), Summary("kicks member from the guild"), Remarks("(PREFIX)kick <user> (optional)<reason>")]
//        public async Task KickUser(IGuildUser user, [Remainder] string reason = "no reason provided.")
//        {
//            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

//            if (!GuildUser.GuildPermissions.KickMembers)
//            {
//                await Context.Channel.TriggerTypingAsync();
//                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//                {
//                    Color = Color.LightOrange,
//                    Title = "You don't have Permission!",
//                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                        Url = Context.Message.GetJumpUrl()
//                    }
//                }.Build());
//            }

//            else
//            {
//                if (user == Context.User)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "You don't have Permission!",
//                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to kick yourself.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                    return;
//                }

//                if (user.GuildPermissions.Administrator)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "I don't have Permission!",
//                        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to kick an administrator.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                    return;
//                }

//                try
//                {
//                    await user.SendMessageAsync($"You've been kicked from {Context.Guild} by {GuildUser} for {reason} at {DateTime.Now}.");
//                }

//                catch (Exception)
//                {
//                    await Context.Message.Channel.SendMessageAsync($"Could not send kick message to {user}.");
//                }

//                await Context.Message.DeleteAsync();
//                await user.KickAsync($"{reason} by {Context.Message.Author}");
//                AddModlogs(user.Id, Action.Kicked, Context.Message.Author.Id, reason, Context.Guild.Id);
//                await Context.Channel.TriggerTypingAsync();
//                SocketGuildUser arg = Context.Guild.GetUser(Context.Message.Author.Id);
//                EmbedBuilder eb = new EmbedBuilder()
//                {
//                    Title = $"***{user.Username} has been kicked***",
//                    Footer = new EmbedFooterBuilder()
//                    {
//                        IconUrl = arg.GetAvatarUrl(),
//                        Text = $"{arg.Username}#{arg.Discriminator}"
//                    },
//                    Description = $"{user} has been kicked by {GuildUser} at {DateTime.Now}\n Reason: {reason}.",
//                    ThumbnailUrl = Global.KickMessageURL,
//                    Color = Color.Orange
//                };
//                eb.WithCurrentTimestamp()
//               .Build();
//                await Context.Channel.SendMessageAsync("", false, eb.Build());
//            }
//        }

//        [Command("vcmute"), Summary("Mutes a user from voice channels"), Remarks("(PREFIX)vcmute <user> (optional) <user>"), Alias("voicechatmute")]
//        public async Task vcMute(SocketUser user, [Remainder] string reason = "No reason provided")
//        {
//            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);
//            SocketVoiceChannel vc = Context.Guild.GetUser(user.Id).VoiceChannel;
//            SocketGuildUser User = Context.Guild.GetUser(user.Id);

//            if (GuildUser.GuildPermissions.DeafenMembers)
//            {
//                if (user == Context.User)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "You don't have Permission!",
//                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to vcmute yourself.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                    return;
//                }

//                if (User.GuildPermissions.Administrator)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "I don't have Permission!",
//                        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to vcmute an administrator.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                    return;
//                }

//                if (vc == null)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "User not in voice channel!",
//                        Description = $"User needs to be in a voice channel.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());
//                }

//                else
//                {
//                    if (vc.GetUser(user.Id).IsMuted)
//                    {
//                        await Context.Channel.TriggerTypingAsync();
//                        await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                        {
//                            Color = Color.LightOrange,
//                            Title = "User already muted!",
//                            Description = $"This user is already muted.",
//                            Author = new EmbedAuthorBuilder()
//                            {
//                                Name = Context.Message.Author.ToString(),
//                                IconUrl = Context.Message.Author.GetAvatarUrl(),
//                                Url = Context.Message.GetJumpUrl()
//                            }
//                        }.Build());
//                    }

//                    else
//                    {
//                        await vc.GetUser(user.Id).ModifyAsync(x => x.Mute = true);
//                        AddModlogs(user.Id, Action.VoiceMuted, Context.Message.Author.Id, reason, Context.Guild.Id);
//                        EmbedBuilder eb = new EmbedBuilder()
//                        {
//                            Title = $"***{user.Username} has been voice chat muted***",
//                            Footer = new EmbedFooterBuilder()
//                            {
//                                IconUrl = user.GetAvatarUrl(),
//                                Text = $"{user.Username}#{user.Discriminator}"
//                            },
//                            Description = $"{user} has been muted by {Context.User} at {DateTime.Now}",
//                            ThumbnailUrl = Global.KickMessageURL,
//                            Color = Color.Orange
//                        };
//                        eb.WithCurrentTimestamp();
//                        await Context.Channel.TriggerTypingAsync();
//                        await Context.Message.ReplyAsync("", false, eb.Build());
//                    }
//                }
//            }

//            else
//            {
//                await Context.Channel.TriggerTypingAsync();
//                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                {
//                    Color = Color.LightOrange,
//                    Title = "You don't have Permission!",
//                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                        Url = Context.Message.GetJumpUrl()
//                    }
//                }.Build());
//            }
//        }

//        [Command("vcunmute"), Summary("Unmutes a user from voice channels"), Remarks("(PREFIX)vcunmute <user>"), Alias("(PREFIX)vcunmute")]
//        public async Task vcUnMute(SocketUser user)
//        {
//            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

//            if (GuildUser.GuildPermissions.MuteMembers)
//            {
//                SocketVoiceChannel vc = Context.Guild.GetUser(user.Id).VoiceChannel;

//                if (vc.GetUser(user.Id).IsMuted)
//                {
//                    if (vc == null)
//                    {
//                        await Context.Channel.TriggerTypingAsync();
//                        await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                        {
//                            Color = Color.LightOrange,
//                            Title = "User not in voice channel!",
//                            Description = $"User needs to be in a voice channel.",
//                            Author = new EmbedAuthorBuilder()
//                            {
//                                Name = Context.Message.Author.ToString(),
//                                IconUrl = Context.Message.Author.GetAvatarUrl(),
//                                Url = Context.Message.GetJumpUrl()
//                            }
//                        }.Build());
//                    }

//                    else
//                    {
//                        await vc.GetUser(user.Id).ModifyAsync(x => x.Mute = false);
//                        EmbedBuilder eb = new EmbedBuilder()
//                        {
//                            Title = $"***{user.Username} has been voice chat unmuted***",
//                            Footer = new EmbedFooterBuilder()
//                            {
//                                IconUrl = user.GetAvatarUrl(),
//                                Text = $"{user.Username}#{user.Discriminator}"
//                            },
//                            Description = $"{user} has been unmuted by {Context.User} at {DateTime.Now}",
//                            ThumbnailUrl = Global.KickMessageURL,
//                            Color = Color.Orange
//                        };
//                        eb.WithCurrentTimestamp();
//                        await Context.Channel.TriggerTypingAsync();
//                        await Context.Channel.SendMessageAsync("", false, eb.Build());
//                    }
//                }

//                else
//                {
//                    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "User not muted!",
//                        Description = $"This user is not currently muted.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                }
//            }

//            else
//            {
//                await Context.Channel.TriggerTypingAsync();
//                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                {
//                    Color = Color.LightOrange,
//                    Title = "You don't have Permission!",
//                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                        Url = Context.Message.GetJumpUrl()
//                    }
//                }.Build());
//            }
//        }

//        [Command("Warn"), Summary("Warns a user"), Remarks("(PREFIX)warn <user> (optional)<reason>")]
//        public async Task Warn(SocketUser user, [Remainder] string reason = "No reason provded")
//        {
//            SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);
//            SocketGuildUser User = Context.Guild.GetUser(user.Id);

//            if (GuildUser.GuildPermissions.ManageChannels)
//            {
//                if (user == Context.User)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "You don't have Permission!",
//                        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to warn yourself.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                    return;
//                }

//                else if (User.GuildPermissions.Administrator)
//                {
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                    {
//                        Color = Color.LightOrange,
//                        Title = "I don't have Permission!",
//                        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to warn an administrator.",
//                        Author = new EmbedAuthorBuilder()
//                        {
//                            Name = Context.Message.Author.ToString(),
//                            IconUrl = Context.Message.Author.GetAvatarUrl(),
//                            Url = Context.Message.GetJumpUrl()
//                        }
//                    }.Build());

//                    return;
//                }

//                else
//                {
//                    AddModlogs(user.Id, Action.Warned, Context.Message.Author.Id, reason, Context.Guild.Id);
//                    EmbedBuilder eb = new EmbedBuilder()
//                    {
//                        Title = $"***{user.Username} has been warned***",
//                        Footer = new EmbedFooterBuilder()
//                        {
//                            IconUrl = user.GetAvatarUrl(),
//                            Text = $"{user.Username}#{user.Discriminator}"
//                        },
//                        Description = $"{user} has been warned by {Context.User} at {DateTime.Now}\n Reason: {reason}.",
//                        ThumbnailUrl = Global.KickMessageURL,
//                        Color = Color.Orange
//                    };
//                    eb.WithCurrentTimestamp();
//                    await Context.Channel.TriggerTypingAsync();
//                    await Context.Channel.SendMessageAsync("", false, eb.Build());
//                }
//            }

//            else
//            {
//                await Context.Channel.TriggerTypingAsync();
//                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                {
//                    Color = Color.LightOrange,
//                    Title = "You don't have Permission!",
//                    Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                        Url = Context.Message.GetJumpUrl()
//                    }
//                }.Build());
//            }
//        }

//        public static void AddModlogs(ulong userID, Action action, ulong ModeratorID, string reason, ulong GuildId)
//        {
//            DateTime dt = DateTime.Now;
//            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.ModLogsPath}");
//            conn.Open();
//            using var cmd = new SQLiteCommand(conn);
//            using var cmd2 = new SQLiteCommand(conn);
//            cmd2.CommandText = $"SELECT * FROM modlogs WHERE guildId = '{GuildId}' AND userId = '{userID}'";
//            using SQLiteDataReader rdr = cmd2.ExecuteReader();
//            int indx = 0;

//            while (rdr.Read())
//            {
//                indx++;
//                if (indx == 5)
//                {
//                    //    var modlogs = Global.Client.GetGuild(Global.GuildId).GetTextChannel(Global.ModLogChannel);
//                    //    var embed = new EmbedBuilder();
//                    //    embed.WithTitle("5 Modlogs Reached!");
//                    //    embed.WithDescription($"<@{userID}> has reached 5 infractions!");
//                    //    embed.WithColor(Color.Red);
//                    //    embed.WithCurrentTimestamp();
//                    //    await modlogs.SendMessageAsync($"", false, embed.Build());
//                    //    SocketGuildUser U = Global.Client.GetGuild(Global.GuildId).GetUser(userID);
//                    //    SocketRole mutedrole = Global.Client.GetGuild(Global.GuildId).GetRole(Global.MuteRoleId);
//                    //    await U.AddRoleAsync(mutedrole);
//                    //    AddModlogs(user.Id, Action.Muted, 730015197980262424, "automute - too many infractions", user.Username);
//                    //    string[] formats = { @"h\h", @"s\s", @"m\m\ s\s", @"h\h\ m\m\ s\s", @"m\m", @"h\h\ m\m", @"d\d h\h\ m\m\ s\s", @"d\d", @"d\d h\h", @"d\d h\h m\m", @"d\d h\h m\m s\s" };
//                    //    TimeSpan t = TimeSpan.ParseExact("1h", formats, null);
//                    //    await MuteService.MuteAsyncSeconds((SocketUser)user, Global.Client.GetGuild(Global.GuildId), t, Global.Client.GetGuild(Global.GuildId).GetTextChannel(Global.ModLogChannel));
//                }

//                else
//                {
//                    break;
//                }
//            }

//            indx += 1;
//            cmd.CommandText = $"INSERT INTO modlogs(userId, action, moderatorId, reason, guildId, dateTime, indx) VALUES ('{userID}', '{action}', '{ModeratorID}', '{reason}', '{GuildId}', '{dt}', {indx})";
//            cmd.ExecuteNonQuery();
//            conn.Close();
//        }

//        public enum Action
//        {
//            Warned,
//            Kicked,
//            Banned,
//            Muted,
//            VoiceMuted
//        }

//        [Command("clearlogs"), Summary("Clears users logs"), Remarks("(PREFIX)clearlogs <user> <amount>"), Alias("clearlog", "cl")]
//        public async Task Clearwarn(string user1 = null, int number = 999)
//        {
//            SocketGuildUser user = Context.User as SocketGuildUser;
//            IReadOnlyCollection<SocketUser> mentions = Context.Message.MentionedUsers;

//            if (!user.GuildPermissions.ManageMessages)
//            {
//                await Context.Message.ReplyAsync("", false, new Discord.EmbedBuilder()
//                {
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                    },
//                    Title = "You do not have permission to execute this command",
//                    Description = "You do not have the valid permission to execute this command",
//                    Color = Color.Red
//                }.Build());
//                return;
//            }

//            if (mentions.Count == 0)
//            {
//                EmbedBuilder noUser = new EmbedBuilder();
//                noUser.WithTitle("Error");
//                noUser.WithDescription("Please mention a user!");
//                noUser.WithColor(Color.Red);
//                noUser.WithAuthor(Context.Message.Author);
//                await Context.Message.ReplyAsync("", false, noUser.Build());

//                return;
//            }

//            SocketUser u = mentions.First();
//            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.ModLogsPath}");
//            conn.Open();
//            string stm = $"DELETE FROM modlogs WHERE guildId = '{Context.Guild.Id}' AND userId = '{u.Id}' AND indx = {number}";
//            using var cmd = new SQLiteCommand(stm, conn);
//            cmd.ExecuteNonQuery();
//            EmbedBuilder b = new EmbedBuilder()
//            {
//                Author = new EmbedAuthorBuilder()
//                {
//                    Name = Context.Message.Author.ToString(),
//                    IconUrl = Context.Message.Author.GetAvatarUrl(),
//                },
//                Title = $"Successfully cleared log for **{u.Username}**",
//                Color = Color.DarkMagenta,
//                Description = $"Log for {u.Username} cleared.",
//                Fields = new List<EmbedFieldBuilder>()
//            };
//            await Context.Message.ReplyAsync("", false, b.Build());
//        }

//        [Command("ClearAllModLogs"), Summary("Clears all logs for a user"), Remarks("(PREFIX)ClearAllModLogs <user>"), Alias("clearalllogs", "cal", "caml")]
//        public async Task ClearAllModLogs(string user1 = null)
//        {
//            SocketGuildUser user = Context.User as SocketGuildUser;
//            IReadOnlyCollection<SocketUser> mentions = Context.Message.MentionedUsers;

//            if (!user.GuildPermissions.ManageMessages)
//            {
//                await Context.Message.ReplyAsync("", false, new Discord.EmbedBuilder()
//                {
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                    },
//                    Title = "You do not have permission to execute this command",
//                    Description = "You do not have the valid permission to execute this command",
//                    Color = Color.Red
//                }.Build());
//                return;
//            }

//            if (mentions.Count == 0)
//            {
//                EmbedBuilder noUser = new EmbedBuilder();
//                noUser.WithTitle("Error");
//                noUser.WithDescription("Please mention a user!");
//                noUser.WithColor(Color.Red);
//                noUser.WithAuthor(Context.Message.Author);
//                await Context.Message.ReplyAsync("", false, noUser.Build());

//                return;
//            }

//            SocketUser u = mentions.First();
//            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.ModLogsPath}");
//            conn.Open();
//            string stm = $"DELETE FROM modlogs WHERE guildId = '{Context.Guild.Id}' AND userId = '{u.Id}'";
//            using var cmd = new SQLiteCommand(stm, conn);
//            cmd.ExecuteNonQuery();
//            EmbedBuilder b = new EmbedBuilder()
//            {
//                Author = new EmbedAuthorBuilder()
//                {
//                    Name = Context.Message.Author.ToString(),
//                    IconUrl = Context.Message.Author.GetAvatarUrl(),
//                },
//                Title = $"Successfully cleared all logs for **{u.Username}**",
//                Color = Color.DarkMagenta,
//                Description = $"Modlogs for {u.Username} have been cleared",
//                Fields = new List<EmbedFieldBuilder>()
//            };
//            await Context.Message.ReplyAsync("", false, b.Build());
//        }

//        [Command("modlogs"), Summary("Shows infractions of a user"), Remarks("(PREFIX)modlogs <user>"), Alias("logs", "modlog", "mod-logs")]
//        public async Task Modlogs(string mention = null)
//        {
//            SocketGuildUser user = Context.User as SocketGuildUser;
//            IReadOnlyCollection<SocketUser> mentions = Context.Message.MentionedUsers;

//            if (!user.GuildPermissions.ManageMessages)
//            {
//                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                {
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                    },
//                    Title = "You do not have permission to execute this command",
//                    Description = "You do not have the valid permission to execute this command",
//                    Color = Color.Red
//                }.Build());

//                return;
//            }

//            if (mentions.Count == 0)
//            {
//                EmbedBuilder noUser = new EmbedBuilder();
//                noUser.WithTitle("Error");
//                noUser.WithDescription("Please mention a user!");
//                noUser.WithColor(Color.Red);
//                noUser.WithAuthor(Context.Message.Author);
//                await Context.Message.ReplyAsync("", false, noUser.Build());

//                return;
//            }

//            SocketUser user1 = mentions.First();
//            SQLiteConnection conn = new SQLiteConnection($"data source = {Global.ModLogsPath}");
//            conn.Open();
//            string stm = $"SELECT * FROM modlogs WHERE guildId = '{Context.Guild.Id}' AND userId = '{user1.Id}'";
//            using var cmd = new SQLiteCommand(stm, conn);
//            SQLiteDataReader rdr = cmd.ExecuteReader();
//            string usrnm = Context.Guild.GetUser(user1.Id) == null ? user1.Username : Context.Guild.GetUser(user1.Id).ToString();
//            EmbedBuilder b = new EmbedBuilder()
//            {
//                Author = new EmbedAuthorBuilder()
//                {
//                    Name = Context.Message.Author.ToString(),
//                    IconUrl = Context.Message.Author.GetAvatarUrl(),
//                },
//                Title = $"Modlogs for **{usrnm}** ({user1.Id})",
//                Description = $"To remove a log type `{Global.Prefix}clearlog <user> <log number>`",
//                Color = Color.Green,
//                Fields = new List<EmbedFieldBuilder>()
//            };

//            while (rdr.Read())
//            {
//                b.Fields.Add(new EmbedFieldBuilder()
//                {
//                    IsInline = false,
//                    Name = $"**{rdr.GetString(2)}** \nReason: {rdr.GetString(4)}\nModerator: <@{rdr.GetString(3)}>\nDate: {rdr.GetString(6)}\nIndex number: **{rdr.GetInt32(7)}**",
//                    Value = $"Reason: {rdr.GetString(4)}\nModerator: <@{rdr.GetString(3)}>\nDate: {rdr.GetString(6)}\nIndex number: **{rdr.GetInt32(7)}**"
//                });
//            }

//            if (b.Fields.Count != 0)
//            {
//                await SendToLogger(b, Context.Guild.Id);
//            }

//            else
//            {
//                await Context.Message.ReplyAsync("", false, new EmbedBuilder()
//                {
//                    Author = new EmbedAuthorBuilder()
//                    {
//                        Name = Context.Message.Author.ToString(),
//                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//                    },
//                    Title = $"Modlogs for ({user1.Id})",
//                    Description = "This user has no logs! :D",
//                    Color = Color.Green
//                }.Build());

//                return;
//            }
//        }

//        public async Task SendToLogger(EmbedBuilder b, ulong GuildId)
//        {
//            InfractionMessageHandler infractionMessageHandler = new InfractionMessageHandler(Global.Client, GuildId, b.Build());
//            var msg = await Context.Message.ReplyAsync("", false, infractionMessageHandler.InfractionEmbedBuilder(1, InfractionMessageHandler.CalcInfractionPage(Context.Guild.GetUser(Context.Message.Author.Id))));
//            var emote1 = new Emoji("\U000027A1");
//            var emote2 = new Emoji("\U00002B05");
//            await msg.AddReactionAsync(emote2);
//            await msg.AddReactionAsync(emote1);
//            InfractionMessageHandler.CurrentInfractionMessages.Add(msg.Id, Context.Message.Author.Id);
//            Global.SaveInfractionMessageCards();
//        }

//        //[Command("mute"), Summary("mutes a user from the Guild)"), Remarks("(PREFIX)mute <user> (optional)<reason>")]
//        //public async Task mute(IGuildUser user, [Remainder] string reason = "No reason provided")
//        //{
//        //    SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

//        //    if (GuildUser.GuildPermissions.MuteMembers)
//        //    {
//        //        //if (user == Context.User)
//        //        //{
//        //        //    await Context.Channel.TriggerTypingAsync();
//        //        //    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//        //        //    {
//        //        //        Color = Color.LightOrange,
//        //        //        Title = "You don't have Permission!",
//        //        //        Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to mute yourself.",
//        //        //        Author = new EmbedAuthorBuilder()
//        //        //        {
//        //        //            Name = Context.Message.Author.ToString(),
//        //        //            IconUrl = Context.Message.Author.GetAvatarUrl(),
//        //        //            Url = Context.Message.GetJumpUrl()
//        //        //        }
//        //        //    }.Build());

//        //        //    return;
//        //        //}

//        //        //if (user.GuildPermissions.Administrator)
//        //        //{
//        //        //    await Context.Channel.TriggerTypingAsync();
//        //        //    await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//        //        //    {
//        //        //        Color = Color.LightOrange,
//        //        //        Title = "I don't have Permission!",
//        //        //        Description = $"Sorry, {Context.Message.Author.Mention} but I do not have permission to mute an administrator.",
//        //        //        Author = new EmbedAuthorBuilder()
//        //        //        {
//        //        //            Name = Context.Message.Author.ToString(),
//        //        //            IconUrl = Context.Message.Author.GetAvatarUrl(),
//        //        //            Url = Context.Message.GetJumpUrl()
//        //        //        }
//        //        //    }.Build());

//        //        //    return;
//        //        //}

//        //        SQLiteConnection conn = new SQLiteConnection($"data source = {Global.muteRoleFilepath}");
//        //        using var cmd = new SQLiteCommand(conn);
//        //        conn.Open();
//        //        cmd.CommandText = $"SELECT * FROM muteRole WHERE guildId = '{Context.Guild.Id}'";
//        //        using SQLiteDataReader rdr = cmd.ExecuteReader();
//        //        bool ran = false;

//        //        while (rdr.Read())
//        //        {
//        //            ran = true;
//        //            ulong role = Convert.ToUInt64(rdr.GetString(2));

//        //            if (!Context.Guild.Roles.Any(gR => gR.Id == role))
//        //            {
//        //                if (Context.Guild.Roles.Any(xx => xx.Name == "Muted"))
//        //                {
//        //                    SocketRole r = Context.Guild.Roles.First(x => x.Name == "Muted");
//        //                    using var cmd2 = new SQLiteCommand(conn);
//        //                    cmd2.CommandText = $"UPDATE muteRole SET roleId = '{r.Id}' WHERE guildId = '{Context.Guild.Id}'";
//        //                    cmd2.ExecuteNonQuery();

//        //                    if (!user.RoleIds.Contains(r.Id))
//        //                    {
//        //                        await user.AddRoleAsync(Context.Guild.GetRole(role));
//        //                        AddModlogs(user.Id, Action.Muted, Context.Message.Author.Id, reason, Context.Guild.Id);
//        //                        await Context.Channel.TriggerTypingAsync();
//        //                        EmbedBuilder eb2 = new EmbedBuilder()
//        //                        {
//        //                            Title = $"***{user.Username} has been muted***",
//        //                            Footer = new EmbedFooterBuilder()
//        //                            {
//        //                                IconUrl = user.GetAvatarUrl(),
//        //                                Text = $"{user.Username}#{user.Discriminator}"
//        //                            },
//        //                            Description = $"{user} has been muted at {DateTime.Now}\n Reason: {reason}.",
//        //                            ThumbnailUrl = Global.KickMessageURL,
//        //                            Color = Color.Orange
//        //                        };
//        //                        eb2.WithCurrentTimestamp()
//        //                       .Build();
//        //                        await Context.Channel.SendMessageAsync("", false, eb2.Build());
//        //                    }

//        //                    else
//        //                    {
//        //                        await Context.Channel.TriggerTypingAsync();
//        //                        await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//        //                        {
//        //                            Color = Color.LightOrange,
//        //                            Title = "User already muted!",
//        //                            Description = $"This user is already muted.",
//        //                            Author = new EmbedAuthorBuilder()
//        //                            {
//        //                                Name = Context.Message.Author.ToString(),
//        //                                IconUrl = Context.Message.Author.GetAvatarUrl(),
//        //                                Url = Context.Message.GetJumpUrl()
//        //                            }
//        //                        }.Build());

//        //                        return;
//        //                    }
//        //                }

//        //                else
//        //                {
//        //                    IRole mR = await Context.Guild.CreateRoleAsync("Muted", GuildPermissions.None, Color.DarkerGrey, false, null);
//        //                    using var cmd3 = new SQLiteCommand(conn);
//        //                    cmd3.CommandText = $"UPDATE muteRole SET roleId = '{mR.Id}' WHERE guildId = '{Context.Guild.Id}'";
//        //                    cmd3.ExecuteNonQuery();
//        //                    await user.AddRoleAsync(Context.Guild.GetRole(role));
//        //                    AddModlogs(user.Id, Action.Muted, Context.Message.Author.Id, reason, Context.Guild.Id);
//        //                    await Context.Channel.TriggerTypingAsync();
//        //                    EmbedBuilder eb = new EmbedBuilder()
//        //                    {
//        //                        Title = $"***{user.Username} has been muted***",
//        //                        Footer = new EmbedFooterBuilder()
//        //                        {
//        //                            IconUrl = user.GetAvatarUrl(),
//        //                            Text = $"{user.Username}#{user.Discriminator}"
//        //                        },
//        //                        Description = $"{user} has been muted at {DateTime.Now}\n Reason: {reason}.",
//        //                        ThumbnailUrl = Global.KickMessageURL,
//        //                        Color = Color.Orange
//        //                    };
//        //                    eb.WithCurrentTimestamp()
//        //                   .Build();
//        //                    await Context.Channel.SendMessageAsync("", false, eb.Build());
//        //                }
//        //            }

//        //            else
//        //            {
//        //                await user.AddRoleAsync(Context.Guild.GetRole(role));
//        //                AddModlogs(user.Id, Action.Muted, Context.Message.Author.Id, reason, Context.Guild.Id);
//        //                await Context.Channel.TriggerTypingAsync();
//        //                EmbedBuilder eb = new EmbedBuilder()
//        //                {
//        //                    Title = $"***{user.Username} has been muted***",
//        //                    Footer = new EmbedFooterBuilder()
//        //                    {
//        //                        IconUrl = user.GetAvatarUrl(),
//        //                        Text = $"{user.Username}#{user.Discriminator}"
//        //                    },
//        //                    Description = $"{user} has been muted at {DateTime.Now}\n Reason: {reason}.",
//        //                    ThumbnailUrl = Global.KickMessageURL,
//        //                    Color = Color.Orange
//        //                };
//        //                eb.WithCurrentTimestamp()
//        //               .Build();
//        //                await Context.Channel.SendMessageAsync("", false, eb.Build());
//        //            }
//        //        }

//        //            if (!ran)
//        //            {
//        //                IRole mR = await Context.Guild.CreateRoleAsync("Muted", GuildPermissions.None, Color.DarkerGrey, false, null);
//        //                using var cmd3 = new SQLiteCommand(conn);
//        //                cmd3.CommandText = $"INSERT INTO muteRole(guildId, roleId) VALUES ('{Context.Guild.Id}', '{mR.Id}')";
//        //                cmd3.ExecuteNonQuery();
//        //                await user.AddRoleAsync(mR);
//        //                AddModlogs(user.Id, Action.Muted, Context.Message.Author.Id, reason, Context.Guild.Id);
//        //                await Context.Channel.TriggerTypingAsync();
//        //                EmbedBuilder eb = new EmbedBuilder()
//        //                {
//        //                    Title = $"***{user.Username} has been muted***",
//        //                    Footer = new EmbedFooterBuilder()
//        //                    {
//        //                        IconUrl = user.GetAvatarUrl(),
//        //                        Text = $"{user.Username}#{user.Discriminator}"
//        //                    },
//        //                    Description = $"{user} has been muted at {DateTime.Now}\n Reason: {reason}.",
//        //                    ThumbnailUrl = Global.KickMessageURL,
//        //                    Color = Color.Orange
//        //                };
//        //                eb.WithCurrentTimestamp()
//        //               .Build();
//        //                await Context.Channel.SendMessageAsync("", false, eb.Build());
//        //            }

//        //        conn.Close();
//        //    }

//        //    else
//        //    {
//        //        await Context.Channel.TriggerTypingAsync();
//        //        await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//        //        {
//        //            Color = Color.LightOrange,
//        //            Title = "You don't have Permission!",
//        //            Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//        //            Author = new EmbedAuthorBuilder()
//        //            {
//        //                Name = Context.Message.Author.ToString(),
//        //                IconUrl = Context.Message.Author.GetAvatarUrl(),
//        //                Url = Context.Message.GetJumpUrl()
//        //            }
//        //        }.Build());
//        //    }
//        //}

//        ////public static async Task RemoveMutedRole(IGuildUser user, SocketGuild guild, SocketTextChannel channel, DateTime timesince)
//        ////{
//        ////    SQLiteConnection conn = new SQLiteConnection($"data source = {Global.muteRoleFilepath}");
//        ////    using var cmd = new SQLiteCommand(conn);
//        ////    conn.Open();
//        ////    cmd.CommandText = $"SELECT * FROM muteRole WHERE guildId = '{guild.Id}'";
//        ////    using SQLiteDataReader rdr = cmd.ExecuteReader();

//        ////    while (rdr.Read())
//        ////    {
//        ////        await user.RemoveRoleAsync(guild.GetRole(Convert.ToUInt64(rdr.GetString(2))));
//        ////    }

//        ////    conn.Close();
//        ////}

//        //[Command("unmute"), Summary("Unmutes a user"), Remarks("(PREFIX)unmute <user>")]
//        //public async Task Unmute(IGuildUser user)
//        //{
//        //    SocketGuildUser GuildUser = Context.Guild.GetUser(Context.User.Id);

//        //    if (GuildUser.GuildPermissions.MuteMembers)
//        //    {
//        //        SQLiteConnection conn = new SQLiteConnection($"data source = {Global.muteRoleFilepath}");
//        //        using var cmd = new SQLiteCommand(conn);
//        //        conn.Open();
//        //        cmd.CommandText = $"SELECT * FROM muteRole WHERE guildId = '{Context.Guild.Id}'";
//        //        using SQLiteDataReader rdr = cmd.ExecuteReader();

//        //        while (rdr.Read())
//        //        {
//        //            ulong role = Convert.ToUInt64(rdr.GetString(2));
//        //            SocketRole mutedrole = Context.Guild.GetRole(role);

//        //            if (!user.RoleIds.Contains(mutedrole.Id))
//        //            {
//        //                await Context.Channel.TriggerTypingAsync();
//        //                await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//        //                {
//        //                    Color = Color.LightOrange,
//        //                    Title = "User is not muted!",
//        //                    Description = $"This user is not currently muted.",
//        //                    Author = new EmbedAuthorBuilder()
//        //                    {
//        //                        Name = Context.Message.Author.ToString(),
//        //                        IconUrl = Context.Message.Author.GetAvatarUrl(),
//        //                        Url = Context.Message.GetJumpUrl()
//        //                    }
//        //                }.Build());
//        //            }

//        //            else
//        //            {
//        //                await user.RemoveRoleAsync(mutedrole);
//        //                EmbedBuilder eb = new EmbedBuilder()
//        //                {
//        //                    Title = $"***{user.Username} has been ummuted***",
//        //                    Footer = new EmbedFooterBuilder()
//        //                    {
//        //                        IconUrl = user.GetAvatarUrl(),
//        //                        Text = $"{user.Username}#{user.Discriminator}"
//        //                    },
//        //                    Description = $"{user} has been unmuted by {Context.User} at {DateTime.Now}",
//        //                    ThumbnailUrl = Global.KickMessageURL,
//        //                    Color = Color.Orange
//        //                };
//        //                eb.WithCurrentTimestamp();
//        //                await Context.Channel.TriggerTypingAsync();
//        //                await Context.Channel.SendMessageAsync("", false, eb.Build());
//        //            }
//        //        }

//        //        conn.Close();
//        //    }

//        //    else
//        //    {
//        //        await Context.Channel.TriggerTypingAsync();
//        //        await Context.Message.Channel.SendMessageAsync("", false, new EmbedBuilder()
//        //        {
//        //            Color = Color.LightOrange,
//        //            Title = "You don't have Permission!",
//        //            Description = $"Sorry, {Context.Message.Author.Mention} but you do not have permission to use this command.",
//        //            Author = new EmbedAuthorBuilder()
//        //            {
//        //                Name = Context.Message.Author.ToString(),
//        //                IconUrl = Context.Message.Author.GetAvatarUrl(),
//        //                Url = Context.Message.GetJumpUrl()
//        //            }
//        //        }.Build());
//        //    }
//        //}


//        //This is just boilerplate code so my bot A.) doesn't complain when I execute functions from the Python module and b.) to include it in the help command
//        [Command("audit"), Summary("Gets audit log info on user/channel/guild"), Remarks("(PREFIX)audit [roles] <user> | (PREFIX)audit [overrides] <channel>")]
//        public Task audit(params string[] args)
//        {
//            return Task.CompletedTask;
//        }
//    }
//}
////✅❌