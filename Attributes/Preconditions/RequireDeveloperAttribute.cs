using Discord;
using Discord.Commands;
using System;
using Discord.Rest;
using Discord.WebSocket;
using PreconditionResult = Discord.Commands.PreconditionResult;

namespace FinBot.Attributes.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireDeveloperAttribute : PreconditionAttribute
    {
        public override string ErrorMessage { get; set; }

        public override async System.Threading.Tasks.Task<Discord.Commands.PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            switch (context.Client.TokenType)
            {
                case TokenType.Bot:
                    RestApplication application = (RestApplication)await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
                    
                    if (!Global.IsDev((SocketUser)context.User))
                    {
                        if(Global.clientCommands == true && context.User.Id == context.Client.CurrentUser.Id)
                        {
                            return PreconditionResult.FromSuccess();
                        }

                        if(Global.hiddenCommands.Contains(command.Name))
                        {
                            return PreconditionResult.FromError("");
                        }

                        return PreconditionResult.FromError(ErrorMessage ?? "Command can only be run by a listed developer of the bot.");
                    }

                    return PreconditionResult.FromSuccess();

                default:
                    return PreconditionResult.FromError($"{nameof(RequireDeveloperAttribute)} is not supported by this {nameof(TokenType)}.");
            }
        }
    }
}
