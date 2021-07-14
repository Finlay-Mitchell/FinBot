using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FinBot.Handlers
{
    class CommandHandler : ModuleBase<ShardedCommandContext>
    {
        private CommandService _commands;
        private DiscordShardedClient _client;
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services)
        {
            _services = services;
            _client = services.GetRequiredService<DiscordShardedClient>();
            _commands = services.GetRequiredService<CommandService>();
            _logger = services.GetRequiredService<ILogger<CommandHandler>>();

            _client.MessageReceived += HandleCommandAsync;
        }

        /// <summary>
        /// Handles command execution.
        /// </summary>
        /// <param name="s">User message</param>
        public async Task HandleCommandAsync(SocketMessage s)
        {
            SocketUserMessage message = s as SocketUserMessage;

            if (message == null)
            {
                return;
            }

            //These next two variables are so we can get the same type to compare the two users. I SocketSelfUser is not comparable to SocketGuildUser, stupidly. But this, whilst not ideal, works.
            SocketUser currUser = _client.GetUser(_client.CurrentUser.Id); 
            SocketUser execUser = _client.GetUser(message.Author.Id);

            if (message.Source != MessageSource.User || message.Channel.GetType() == typeof(SocketDMChannel))
            {
                if (execUser == currUser && message.Content.StartsWith(Global.clientPrefix) && Global.clientCommands == true)
                {
                    goto repos; //This is absolutely vile, but for now it'll do.
                }

                await CheckSupportAsync(s); //Checks whether the support feature has been called.

                return;
            }

            repos: //This isn't fantastic and I'll probably make a workaround soon, but it'll work for now.

            int argPos = 0;
            ShardedCommandContext context = new ShardedCommandContext(_client, message);

            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasStringPrefix(await Global.DeterminePrefix(context), ref argPos)))
            {
                if (execUser == currUser && message.HasStringPrefix(Global.clientPrefix, ref argPos) && Global.clientCommands == true) //If we've enabled Global.clientCommands, the current user is executing the commands & the prefix matches, run command from bot.
                {
                    IResult devres = await _commands.ExecuteAsync(context, argPos, null, MultiMatchHandling.Best);
                    await LogCommandUsage(context, devres);
                    return;
                }

                return;
            }

            IResult result = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
            await LogCommandUsage(context, result);

            if (!result.IsSuccess && !Global.ErorrsToIgnore.Contains(result.Error.Value))
            {
                EmbedBuilder b = new EmbedBuilder
                {
                    Color = Color.Red,
                    Description = $"The following info is the Command error info, `{message.Author.Username}#{message.Author.Discriminator}` tried to use the `{message}` Command in {context.Guild.Name}/{message.Channel}: \n \n **COMMAND ERROR**: ```{result.Error.Value}``` \n \n **COMMAND ERROR REASON**: ```{result.ErrorReason}```",
                    Author = new EmbedAuthorBuilder()
                };
                b.Author.Name = message.Author.Username + "#" + message.Author.Discriminator;
                b.Author.IconUrl = message.Author.GetAvatarUrl();
                b.Footer = new EmbedFooterBuilder
                {
                    Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };
                b.Title = "Bot Command Error!";

                try
                {
                    await _client.GetGuild(Global.SupportGuildId).GetTextChannel(Global.ErrorLogChannelId).SendMessageAsync("", false, b.Build());
                }

                catch { }
            }
        }

        /// <summary>
        /// Checks whether the user is trying to get support.
        /// </summary>
        /// <param name="s">Current user message.</param>
        private async Task CheckSupportAsync(SocketMessage s)
        {
            if (s.Source == MessageSource.User)
            {
                if (s.Channel.GetType() == typeof(SocketDMChannel))
                {
                    string msg = s.ToString();
                    SocketUserMessage message = s as SocketUserMessage;

                    if (msg.ToLower().StartsWith("support ") || msg.ToLower().StartsWith("support") || msg.ToLower().StartsWith($"&support") || msg.ToLower().StartsWith($"&support "))
                    {
                        msg = Regex.Replace(msg, "support", "");
                        IUserMessage reply = (IUserMessage)s;
                        EmbedBuilder b = Global.EmbedMessage("Support ticket submitted", "Thank you for submitting your support ticket. A developer will review it shortly.", true, Color.Magenta);
                        b.WithFooter($"Your ticked id is: {s.Id}");
                        b.WithCurrentTimestamp();
                        await reply.ReplyAsync("", false, b.Build());
                        EmbedBuilder eb = new EmbedBuilder();
                        eb.WithAuthor(s.Author);
                        eb.WithCurrentTimestamp();
                        eb.WithFooter($"DM channel Id: {s.Channel.Id}\nSupport ticket Id: {s.Id}");
                        eb.WithTitle("New support ticket");
                        eb.WithDescription($"```{msg}```");
                        eb.WithColor(Color.DarkPurple);
                        await _client.GetGuild(Global.SupportGuildId).GetTextChannel(Global.SupportChannelId).SendMessageAsync("", false, eb.Build());

                        return;
                    }

                    await message.ReplyAsync("Sorry, but commands are not enabled in DM's. Please try using bot commands in a server.");

                    return;
                }
            }
        }

        /// <summary>
        /// Logs when a command is used.
        /// </summary>
        /// <param name="context">The context for the command.</param>
        /// <param name="result">The result of the command executed.</param>
        private async Task LogCommandUsage(ShardedCommandContext context, IResult result)
        {
            await Task.Run(() =>
            {
                if (context.Channel is IGuildChannel)
                {
                    string logTxt = $"User: [{context.User.Username}]<->[{context.User.Id}] Discord Server: [{context.Guild.Name}] -> [{context.Message.Content}]";
                    _logger.LogInformation(logTxt);
                }

                else
                {
                    string logTxt = $"User: [{context.User.Username}]<->[{context.User.Id}] -> [{context.Message.Content}]";
                    _logger.LogInformation(logTxt);
                }

                if(!result.IsSuccess)
                {
                    string logTxt = $"Command: Failed to execute \"{context.Message.Content}\" for {context.User.Username} in {context.Guild.Name}/{context.Channel} with reason: {result.Error}/{result.ErrorReason}";
                    _logger.LogError(logTxt);
                }
            });
        }
    }





















    //    if (!(s is SocketUserMessage msg))
    //    {
    //        return;
    //    }

    //    SocketCommandContext context = new SocketCommandContext(_client, msg);

    //    if (msg.Author.IsBot)
    //    {
    //        return;
    //    }

    //    int argPos = 0;

    //    if (msg.HasStringPrefix(Global.Prefix, ref argPos))
    //    {
    //        if (msg.Channel.GetType() == typeof(SocketDMChannel) && msg.Author.Id != 305797476290527235)
    //        {
    //            try
    //            {
    //                await msg.Channel.SendMessageAsync("sorry but DM's do not accept bot commands.");
    //            }

    //            catch (Exception)
    //            {

    //            }
    //        }

    //        else
    //        {
    //            IResult result = await _service.ExecuteAsync(context, argPos, null, MultiMatchHandling.Best);

    //            if (!result.IsSuccess)
    //            {
    //                EmbedBuilder b = new EmbedBuilder
    //                {
    //                    Color = Color.Red,
    //                    Description = $"The following info is the Command error info, `{msg.Author.Username}#{msg.Author.Discriminator}` tried to use the `{msg}` Command in {msg.Channel}: \n \n **COMMAND ERROR**: ```{result.Error.Value}``` \n \n **COMMAND ERROR REASON**: ```{result.ErrorReason}```",
    //                    Author = new EmbedAuthorBuilder()
    //                };
    //                b.Author.Name = msg.Author.Username + "#" + msg.Author.Discriminator;
    //                b.Author.IconUrl = msg.Author.GetAvatarUrl();
    //                b.Footer = new EmbedFooterBuilder
    //                {
    //                    Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " ZULU"
    //                };
    //                b.Title = "Bot Command Error!";
    //                await _client.GetGuild(799431538814222406).GetTextChannel(810885351223984168).SendMessageAsync("", false, b.Build());
    //            }

    //            if (!result.IsSuccess && result.Error == CommandError.BadArgCount)
    //            {
    //                string msgwp = Regex.Replace(msg.ToString(), $"{Global.Prefix}", "");
    //                await msg.Channel.SendMessageAsync("", false, new EmbedBuilder()
    //                {
    //                    Color = Color.LightOrange,
    //                    Title = "Bad arg count",
    //                    Description = $"Sorry, {msg.Author.Mention} but the command {msg.ToString().Split(' ').First()} does not take those parameters. Use the help command {Global.Prefix}help {msgwp.Split(' ').First()}",
    //                    Author = new EmbedAuthorBuilder()
    //                    {
    //                        Name = msg.Author.ToString(),
    //                        IconUrl = msg.Author.GetAvatarUrl(),
    //                        Url = msg.GetJumpUrl()
    //                    }
    //                }
    //                .WithFooter($"{result.Error.Value}")
    //                .WithCurrentTimestamp()
    //                .Build());
    //            }
    //        }
    //    }
    //}

    //public async Task Init()
    //{
    //    try
    //    {
    //        Console.WriteLine("Starting handler loading...");
    //        await StartHandlers();
    //        Global.ConsoleLog("Finnished Init!", ConsoleColor.Black, ConsoleColor.DarkGreen);
    //        Console.WriteLine("[" + DateTime.Now.TimeOfDay + "] - " + "Command Handler ready");
    //    }

    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex);
    //    }
    //}

    //public bool FirstPass = false;

    //public Task StartHandlers()
    //{
    //    if (!FirstPass)
    //    {
    //        HelpHandler helpHandler = new HelpHandler(_service);
    //        ChatFilter chatFilter = new ChatFilter(_client);
    //        LevellingHandler levellingHandler = new LevellingHandler(_client);
    //        StatusService statusService = new StatusService(_client);                
    //        FirstPass = true;
    //    }

    //    return Task.CompletedTask;
    //}


}
