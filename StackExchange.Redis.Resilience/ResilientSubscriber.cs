using System;
using System.Threading.Tasks;

namespace StackExchange.Redis.Resilience
{
    internal partial class ResilientSubscriber : ISubscriber
    {
        #region ISubscriber implementation

        /// <inheritdoc />
        public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() =>
            {
                _instance.Value.Subscribe(channel, handler, flags);
                _resilientConnectionMultiplexer.AddSubscription(new RedisSubscription(channel, handler, flags));
            });
        }

        /// <inheritdoc />
        public ChannelMessageQueue Subscribe(RedisChannel channel, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() =>
            {
                var queue = _instance.Value.Subscribe(channel, flags);
                _resilientConnectionMultiplexer.AddSubscription(new RedisSubscription(channel, queue, flags));
                return queue;
            });
        }

        /// <inheritdoc />
        public Task SubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(async () =>
            {
                await _instance.Value.SubscribeAsync(channel, handler, flags).ConfigureAwait(false);
                _resilientConnectionMultiplexer.AddSubscription(new RedisSubscription(channel, handler, flags));
            });
        }

        /// <inheritdoc />
        public Task<ChannelMessageQueue> SubscribeAsync(RedisChannel channel, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(async () =>
            {
                var queue = await _instance.Value.SubscribeAsync(channel, flags).ConfigureAwait(false);
                _resilientConnectionMultiplexer.AddSubscription(new RedisSubscription(channel, queue, flags));
                return queue;
            });
        }

        /// <inheritdoc />
        public void Unsubscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler = null, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() =>
            {
                _instance.Value.Unsubscribe(channel, handler, flags);
                _resilientConnectionMultiplexer.Unsubscribe(channel, handler);
            });
        }

        /// <inheritdoc />
        public void UnsubscribeAll(CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() =>
            {
                _instance.Value.UnsubscribeAll(flags);
                _resilientConnectionMultiplexer.UnsubscribeAll();
            });
        }

        /// <inheritdoc />
        public Task UnsubscribeAllAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(async () =>
            {
                await _instance.Value.UnsubscribeAllAsync(flags).ConfigureAwait(false);
                _resilientConnectionMultiplexer.UnsubscribeAll();
            });
        }

        /// <inheritdoc />
        public Task UnsubscribeAsync(RedisChannel channel, Action<RedisChannel, RedisValue> handler = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(async () =>
            {
                await _instance.Value.UnsubscribeAsync(channel, handler, flags).ConfigureAwait(false);
                _resilientConnectionMultiplexer.Unsubscribe(channel, handler);
            });
        }

        #endregion
    }
}
