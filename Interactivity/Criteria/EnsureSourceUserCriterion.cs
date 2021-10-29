using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace FinBot.Interactivity
{
    public class EnsureSourceUserCriterion : ICriterion<SocketMessage>
    {
        public Task<bool> JudgeAsync(ShardedCommandContext sourceContext, SocketMessage parameter)
        {
            bool ok = sourceContext.User.Id == parameter.Author.Id;
            return Task.FromResult(ok);
        }
    }
}