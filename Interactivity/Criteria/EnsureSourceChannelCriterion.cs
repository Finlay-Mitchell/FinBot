using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace FinBot.Interactivity
{
    public class EnsureSourceChannelCriterion : ICriterion<SocketMessage>
    {
        public Task<bool> JudgeAsync(ShardedCommandContext sourceContext, SocketMessage parameter)
        {
            bool ok = sourceContext.Channel.Id == parameter.Channel.Id;
            return Task.FromResult(ok);
        }
    }
}