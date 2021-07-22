using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace FinBot.Interactivity
{
    public class InteractiveBase : InteractiveBase<ShardedCommandContext> { }

    public abstract class InteractiveBase<T> : ModuleBase<T> where T : ShardedCommandContext
    {
        public InteractiveService Interactive { get; set; }
        public Task<SocketMessage> NextMessageAsync(ICriterion<SocketMessage> criterion, TimeSpan? timeout = null, CancellationToken token = default(CancellationToken)) 
            => Interactive.NextMessageAsync(Context, criterion, timeout, token);
        public Task<SocketMessage> NextMessageAsync(bool fromSourceUser = true, bool inSourceChannel = true, TimeSpan? timeout = null, CancellationToken token = default(CancellationToken))
            => Interactive.NextMessageAsync(Context, fromSourceUser, inSourceChannel, timeout, token);
    }
}