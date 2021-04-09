using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinBot.Handlers
{
    public class HelpHandler : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;

        public HelpHandler(CommandService service)
        {
            _service = service;
        }

        [Command("help"), Summary("gives you information on each command"), Remarks("(PREFIX)help(all commands)/(PREFIX)help <command>")]
        public async Task help(params string[] arg)
        {
            if (arg.Length == 1)
            {
                await HelpAsync(arg.First());
            }

            else if (arg.Length > 1)
            {
                await Context.Message.ReplyAsync("Please only enter one parameter");
            }

            else
            {
                await HelpAsync();
            }
        }

        public async Task HelpAsync()
        {
            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Author = new EmbedAuthorBuilder()
                {
                    Name = Context.Message.Author.ToString(),
                    IconUrl = Context.Message.Author.GetAvatarUrl(),
                    Url = Context.Message.GetJumpUrl()
                }
            };

            foreach (ModuleInfo module in _service.Modules)
            {
                string description = null;

                foreach (CommandInfo cmd in module.Commands)
                {
                    PreconditionResult result = await cmd.CheckPreconditionsAsync(Context);

                    if (Global.hiddenCommands.Contains(cmd.Name.ToString()))
                    {
                        continue;
                    }

                    if (result.IsSuccess)
                    {
                        description += $"{Global.Prefix}{cmd.Aliases.First()}, \t";
                    }
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    switch (module.Name)
                    {
                        case "ModCommands":
                            builder.AddField(x =>
                            {
                                x.Name = "Administrative commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        case "HelpHandler":
                            builder.AddField(x =>
                            {
                                x.Name = "Help commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        case "TTSCommands":
                            builder.AddField(x =>
                            {
                                x.Name = "TTS commands";
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;

                        default:
                            builder.AddField(x =>
                            {
                                x.Name = module.Name;
                                x.Value = description.Remove(description.LastIndexOf(','));
                                x.IsInline = false;
                            });
                            break;
                    }
                }
            }

            builder.WithCurrentTimestamp();
            builder.AddField("Help support me", "[To support the development & hosting fees of building FinBot, please help out by donating!](http://ec2-35-176-187-24.eu-west-2.compute.amazonaws.com/donate.html)");
            await Context.Message.ReplyAsync("", false, builder.Build());
        }

        public async Task HelpAsync(string command)
        {
            SearchResult result = _service.Search(Context, command);

            if (!result.IsSuccess || Global.hiddenCommands.Contains(command))
            {
                await Context.Message.ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (CommandMatch match in result.Commands)
            {
                CommandInfo cmd = match.Command;
                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Summary: {cmd.Summary.Replace("(PREFIX)", ($"{Global.Prefix}"))}\nSyntax: {cmd.Remarks.Replace("(PREFIX)", $"{Global.Prefix}")}";
                    x.IsInline = true;
                });
            }

            await Context.Message.ReplyAsync("", false, builder.Build());
        }
    }
}