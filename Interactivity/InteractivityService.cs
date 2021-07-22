using System;
using System.Threading.Tasks;
using System.Threading;
using Discord.Commands;
using Discord.WebSocket;

namespace FinBot.Interactivity
{
    public class InteractiveService : IDisposable
    {
        public BaseSocketClient Discord { get; }
        private TimeSpan _defaultTimeout;

        public InteractiveService(DiscordShardedClient discord, InteractiveServiceConfig config = null) : this((BaseSocketClient)discord, config) { }

        public InteractiveService(BaseSocketClient discord, InteractiveServiceConfig config = null)
        {
            Discord = discord;
            config = config ?? new InteractiveServiceConfig();
            _defaultTimeout = config.DefaultTimeout;
        }

        public Task<SocketMessage> NextMessageAsync(ShardedCommandContext context, bool fromSourceUser = true, bool inSourceChannel = true, TimeSpan? timeout = null, CancellationToken token = default(CancellationToken))
        {
            Criteria<SocketMessage> criterion = new Criteria<SocketMessage>();
            
            if (fromSourceUser)
            {
                criterion.AddCriterion(new EnsureSourceUserCriterion());
            }

            if (inSourceChannel)
            {
                criterion.AddCriterion(new EnsureSourceChannelCriterion());
            }

            return NextMessageAsync(context, criterion, timeout, token);
        }

        public async Task<SocketMessage> NextMessageAsync(ShardedCommandContext context, ICriterion<SocketMessage> criterion, TimeSpan? timeout = null, CancellationToken token = default(CancellationToken))
        {
            timeout = timeout ?? _defaultTimeout;
            TaskCompletionSource<SocketMessage> eventTrigger = new TaskCompletionSource<SocketMessage>();
            TaskCompletionSource<bool> cancelTrigger = new TaskCompletionSource<bool>();
            token.Register(() => cancelTrigger.SetResult(true));

            async Task Handler(SocketMessage message)
            {
                bool result = await criterion.JudgeAsync(context, message).ConfigureAwait(false);

                if (result)
                {
                    eventTrigger.SetResult(message);
                }
            }

            context.Client.MessageReceived += Handler;
            Task<SocketMessage> trigger = eventTrigger.Task;
            Task<bool> cancel = cancelTrigger.Task;
            Task delay = Task.Delay(timeout.Value);
            Task task = await Task.WhenAny(trigger, delay, cancel).ConfigureAwait(false);
            context.Client.MessageReceived -= Handler;

            if (task == trigger)
            {
                return await trigger.ConfigureAwait(false);
            }

            else
            {
                return null;
            }
        }

        public void Dispose() { }
    }
}