using System;
using System.Threading.Tasks;

#nullable enable

namespace StackExchange.Redis.Resilience
{
    internal partial class ResilientSubscriber
    {
        private readonly ResilientConnectionMultiplexer _resilientConnectionMultiplexer;
        private readonly Func<StackExchange.Redis.ISubscriber> _instanceProvider;
        private readonly object _resetLock = new object();
        private AtomicLazy<StackExchange.Redis.ISubscriber>? _instance;
        private long _lastReconnectTicks;

        public ResilientSubscriber(ResilientConnectionMultiplexer resilientConnectionMultiplexer, Func<StackExchange.Redis.ISubscriber> instanceProvider)
        {
            _resilientConnectionMultiplexer = resilientConnectionMultiplexer;
            _instanceProvider = instanceProvider;
            _lastReconnectTicks = resilientConnectionMultiplexer.LastReconnectTicks;
            ResetInstance();
        }

        /// <inheritdoc />
        public IConnectionMultiplexer Multiplexer => _resilientConnectionMultiplexer;


        /// <inheritdoc />
        public System.Net.EndPoint? IdentifyEndpoint(StackExchange.Redis.RedisChannel channel, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.IdentifyEndpoint(channel, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Net.EndPoint?> IdentifyEndpointAsync(StackExchange.Redis.RedisChannel channel, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.IdentifyEndpointAsync(channel, flags));
        }

        /// <inheritdoc />
        public bool IsConnected(StackExchange.Redis.RedisChannel channel = default)
        {
            return ExecuteAction(() => _instance!.Value.IsConnected(channel));
        }

        /// <inheritdoc />
        public long Publish(StackExchange.Redis.RedisChannel channel, StackExchange.Redis.RedisValue message, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Publish(channel, message, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> PublishAsync(StackExchange.Redis.RedisChannel channel, StackExchange.Redis.RedisValue message, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.PublishAsync(channel, message, flags));
        }

        /// <inheritdoc />
        public System.Net.EndPoint? SubscribedEndpoint(StackExchange.Redis.RedisChannel channel)
        {
            return ExecuteAction(() => _instance!.Value.SubscribedEndpoint(channel));
        }

        /// <inheritdoc />
        public System.TimeSpan Ping(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Ping(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.TimeSpan> PingAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.PingAsync(flags));
        }

        /// <inheritdoc />
        public bool TryWait(System.Threading.Tasks.Task task)
        {
            return ExecuteAction(() => _instance!.Value.TryWait(task));
        }

        /// <inheritdoc />
        public void Wait(System.Threading.Tasks.Task task)
        {
            ExecuteAction(() => _instance!.Value.Wait(task));
        }

        /// <inheritdoc />
        public T Wait<T>(System.Threading.Tasks.Task<T> task)
        {
            return ExecuteAction(() => _instance!.Value.Wait<T>(task));
        }

        /// <inheritdoc />
        public void WaitAll(System.Threading.Tasks.Task[] tasks)
        {
            ExecuteAction(() => _instance!.Value.WaitAll(tasks));
        }

        private void ResetInstance()
        {
            _instance = new AtomicLazy<StackExchange.Redis.ISubscriber>(_instanceProvider);
        }

        private void CheckAndReset()
        {
            ResilientConnectionMultiplexer.CheckAndReset(
                _resilientConnectionMultiplexer.LastReconnectTicks,
                ref _lastReconnectTicks,
                _resetLock,
                ResetInstance);
        }

        private T ExecuteAction<T>(Func<T> action)
        {
            return ResilientConnectionMultiplexer.ExecuteAction(_resilientConnectionMultiplexer, () =>
            {
                CheckAndReset();
                return action();
            });
        }

        private Task<T> ExecuteActionAsync<T>(Func<Task<T>> action)
        {
            return ResilientConnectionMultiplexer.ExecuteActionAsync(_resilientConnectionMultiplexer, () =>
            {
                CheckAndReset();
                return action();
            });
        }

        private Task ExecuteActionAsync(Func<Task> action)
        {
            return ResilientConnectionMultiplexer.ExecuteActionAsync(_resilientConnectionMultiplexer, () =>
            {
                CheckAndReset();
                return action();
            });
        }

        private void ExecuteAction(Action action)
        {
            ResilientConnectionMultiplexer.ExecuteAction(_resilientConnectionMultiplexer, () =>
            {
                CheckAndReset();
                action();
            });
        }
    }
}
