using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
///     Requires the command to be invoked by a listed developer of the bot.
/// </summary>
/// <remarks>
///     This precondition will restrict the access of the command or module to listed developers of the Discord application.
///     If the precondition fails to be met, an erroneous <see cref="PreconditionResult"/> will be returned with the "Command can only be run by a listed developer of the bot."
///     <note>
///     This precondition will only work if the account has a <see cref="TokenType"/> of <see cref="TokenType.Bot"/> otherwise, this precondition will always fail.
///     </note>
/// </remarks>
/// <example>
///     The following example restricts the command to a set of sensitive commands that only listed developers of the bot application are able to access.
///     <code language="cs">
///     [RequireDeveloper]
///     [Group("Developers")]
///     public class DeveloperModule : ModuleBase
///     {
///         [SlashCommand("devcommandexample", "Description of the slash command example.")]
///         public async Task DoSomething()
///         {
///             //do stuff
///         }
///     }
///     </code>
/// </example>
/// 
namespace FinBot.Attributes.Interactivity.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireDeveloperAttribute : PreconditionAttribute
    {
        /// <inheritdoc/>
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo command, IServiceProvider services)
        {
            switch(context.Client.TokenType)
            {
                case TokenType.Bot:
                    RestApplication application = (RestApplication)await context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

                    if (!Global.IsDev((SocketUser)context.User))
                    {
                        return PreconditionResult.FromError(ErrorMessage ?? "Command can only be run by a listed developer of the bot.");
                    }

                    return PreconditionResult.FromSuccess();

                default:
                    return PreconditionResult.FromError($"{nameof(RequireDeveloperAttribute)} is not supported by this {nameof(TokenType)}.");
            }
        }
    }
}
