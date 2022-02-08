using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Linq;
using ISlashResult = Discord.Interactions.IResult;
using IResult = Discord.Commands.IResult;
using System.Reflection;

namespace FinBot.Handlers
{
     class CommandHandler : ModuleBase<ShardedCommandContext>
    {
        private CommandService _commands;
        private DiscordShardedClient _client;
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private InteractionService _interactioncommands;

        public CommandHandler(IServiceProvider services)
        {
            _services = services;
            _client = services.GetRequiredService<DiscordShardedClient>();
            _commands = services.GetRequiredService<CommandService>();
            _logger = services.GetRequiredService<ILogger<CommandHandler>>();
            _interactioncommands = services.GetRequiredService<InteractionService>();
            _client.MessageReceived += HandleCommandAsync;
        }

        public async Task InitializeAsync()
        {
            await _interactioncommands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteractionAsync;

            _interactioncommands.SlashCommandExecuted += OnSlashCommandExecuted;
        }

        private async Task OnSlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext arg2, ISlashResult arg3)
        {
        }

        private async Task HandleInteractionAsync(SocketInteraction arg)
        {
            try
            {
                ShardedInteractionContext context = new ShardedInteractionContext(_client, arg);
                //await context.Interaction.DeferAsync();
                await _interactioncommands.ExecuteCommandAsync(context, _services);
            }

            catch(Exception ex)
            {
                if(arg.Type == InteractionType.ApplicationCommand)
                {
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                }
            }
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
                if (message.Content.Contains("@someone"))
                {
                    await message.ReplyAsync(context.Guild.Users.ToList()[new Random().Next(0, context.Guild.Users.Count())].Mention);
                    return;
                }

                if (execUser == currUser && message.HasStringPrefix(Global.clientPrefix, ref argPos) && Global.clientCommands == true) //If we've enabled Global.clientCommands, the current user is executing the commands & the prefix matches, run command from bot.
                {
                    IResult devres = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
                    await LogCommandUsage(context, devres);
                    return;
                }

                return;
            }

            IResult result = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
            await LogCommandUsage(context, result);
            Global.AppendPrefixes(context.Guild.Id, await Global.DeterminePrefix(context)); //Adds the prefix to the dictionary.

            if (!result.IsSuccess && !Global.ErorrsToIgnore.Contains(result.Error.Value))
            {
                if (result.Error.Value == CommandError.UnmetPrecondition)
                {
                    SocketGuildUser UserCheck = context.Guild.GetUser(_client.CurrentUser.Id);

                    if (UserCheck.GuildPermissions.EmbedLinks)
                    {
                        EmbedBuilder eb = new EmbedBuilder();
                        eb.Color = Color.Red;
                        eb.Title = "Error";
                        eb.Description = result.ErrorReason;
                        eb.WithCurrentTimestamp();
                        eb.Footer = new EmbedFooterBuilder()
                        {
                            IconUrl = context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl(),
                            Text = $"{context.User}"
                        };
                        await context.Message.ReplyAsync("", false, eb.Build());
                    }

                    else
                    {
                        await context.Message.ReplyAsync(result.ErrorReason);
                    }

                    return;
                }

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
}
