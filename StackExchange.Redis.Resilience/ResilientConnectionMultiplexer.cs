using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis.Profiling;

namespace StackExchange.Redis.Resilience
{
    /// <summary>
    /// Wraps <see cref="IConnectionMultiplexer"/> in order to recreate it when it fails to reconnect.
    /// Do note that this multiplexer has the following limitations:
    /// <para>- <see cref="StackExchange.Redis.ITransaction"/> methods may throw an <see cref="ObjectDisposedException"/> when
    /// a reconnect was done after the transaction was created.</para>
    /// <para>- <see cref="IBatch"/> methods may throw an <see cref="ObjectDisposedException"/> when a reconnect was
    /// done after the batch was created.</para>
    /// <para>- Enumerating a collection from IDatabase.SetScan, IDatabase.HashScan and IDatabase.SortedSetScan methods
    /// may throw an <see cref="ObjectDisposedException"/> when a reconnect was done while iterating the collection.</para>
    /// </summary>
    public partial class ResilientConnectionMultiplexer : IResilientConnectionMultiplexer
    {
        private readonly SemaphoreSlim _reconnectLock = new SemaphoreSlim(1, 1);
        private readonly Func<ConnectionMultiplexer> _connectionMultiplexerFactory;
        private readonly Func<Task<ConnectionMultiplexer>> _connectionMultiplexerAsyncFactory;
        private readonly object _eventHandlerLock = new object();
        private readonly TimeSpan _reconnectMinFrequency;
        private readonly TimeSpan _reconnectErrorThreshold;

        // Registered event handlers
        private readonly List<EventHandler<RedisErrorEventArgs>> _errorMessageHandlers = new List<EventHandler<RedisErrorEventArgs>>();
        private readonly List<EventHandler<ConnectionFailedEventArgs>> _connectionFailedHandlers = new List<EventHandler<ConnectionFailedEventArgs>>();
        private readonly List<EventHandler<InternalErrorEventArgs>> _internalErrorHandlers = new List<EventHandler<InternalErrorEventArgs>>();
        private readonly List<EventHandler<ConnectionFailedEventArgs>> _connectionRestoredHandlers = new List<EventHandler<ConnectionFailedEventArgs>>();
        private readonly List<EventHandler<EndPointEventArgs>> _configurationChangedHandlers = new List<EventHandler<EndPointEventArgs>>();
        private readonly List<EventHandler<EndPointEventArgs>> _configurationChangedBroadcastHandlers = new List<EventHandler<EndPointEventArgs>>();
        private readonly List<EventHandler<HashSlotMovedEventArgs>> _hashSlotMovedHandlers = new List<EventHandler<HashSlotMovedEventArgs>>();

        // Subscriptions
        private readonly Dictionary<RedisChannel, List<RedisSubscription>> _subscriptions = new Dictionary<RedisChannel, List<RedisSubscription>>();

        // Profiling session provider
        private Func<ProfilingSession> _profilingSessionProvider;

        private ConnectionMultiplexer _connectionMultiplexer;
        private long _lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
        private DateTimeOffset _firstErrorDate = DateTimeOffset.MinValue;
        private DateTimeOffset _previousErrorDate = DateTimeOffset.MinValue;
        private bool _isDisposed;

        /// <summary>
        /// Constructor for creating a <see cref="ResilientConnectionMultiplexer"/>.
        /// </summary>
        /// <param name="connectionMultiplexerFactory">A synchronous factory for creating a <see cref="ConnectionMultiplexer"/>.</param>
        /// <param name="configuration">The configuration for configuring the <see cref="TryReconnect"/> method.</param>
        public ResilientConnectionMultiplexer(
            Func<ConnectionMultiplexer> connectionMultiplexerFactory,
            ResilientConnectionConfiguration configuration = null)
            : this(
                connectionMultiplexerFactory,
                () => Task.FromResult(connectionMultiplexerFactory()),
                configuration)
        {
        }

        /// <summary>
        /// Constructor for creating a <see cref="ResilientConnectionMultiplexer"/>.
        /// </summary>
        /// <param name="connectionMultiplexerFactory">A synchronous factory for creating a <see cref="ConnectionMultiplexer"/>.</param>
        /// <param name="connectionMultiplexerAsyncFactory">A asynchronous factory for creating a <see cref="ConnectionMultiplexer"/>.</param>
        /// <param name="configuration">The configuration for configuring the <see cref="TryReconnect"/> method.</param>
        public ResilientConnectionMultiplexer(
            Func<ConnectionMultiplexer> connectionMultiplexerFactory,
            Func<Task<ConnectionMultiplexer>> connectionMultiplexerAsyncFactory,
            ResilientConnectionConfiguration configuration = null)
        {
            _connectionMultiplexerFactory = connectionMultiplexerFactory;
            _connectionMultiplexerAsyncFactory = connectionMultiplexerAsyncFactory;
            configuration = configuration ?? new ResilientConnectionConfiguration();
            _reconnectMinFrequency = configuration.ReconnectMinFrequency;
            _reconnectErrorThreshold = configuration.ReconnectErrorThreshold;
            _connectionMultiplexer = _connectionMultiplexerFactory();
        }

        /// <summary>
        /// The current wrapped connection multiplexer
        /// </summary>
        public IConnectionMultiplexer ConnectionMultiplexer => _connectionMultiplexer;

        /// <inheritdoc />
        public event EventHandler<ReconnectedEventArgs> Reconnected;

        /// <inheritdoc />
        public event EventHandler<ReconnectErrorEventArgs> ReconnectError;

        /// <inheritdoc />
        public long LastReconnectTicks => Interlocked.Read(ref _lastReconnectTicks);

        #region IConnectionMultiplexer implementation

        /// <inheritdoc />
        public string ClientName => _connectionMultiplexer.ClientName;

        /// <inheritdoc />
        public string Configuration => _connectionMultiplexer.Configuration;

        /// <inheritdoc />
        public int TimeoutMilliseconds => _connectionMultiplexer.TimeoutMilliseconds;

        /// <inheritdoc />
        public long OperationCount => _connectionMultiplexer.OperationCount;

        /// <inheritdoc />
        [Obsolete]
        public bool PreserveAsyncOrder
        {
            get => _connectionMultiplexer.PreserveAsyncOrder;
            set => _connectionMultiplexer.PreserveAsyncOrder = value;
        }

        /// <inheritdoc />
        public bool IsConnected => _connectionMultiplexer.IsConnected;

        /// <inheritdoc />
        public bool IsConnecting => _connectionMultiplexer.IsConnecting;

        /// <inheritdoc />
        public bool IncludeDetailInExceptions
        {
            get => _connectionMultiplexer.IncludeDetailInExceptions;
            set => _connectionMultiplexer.IncludeDetailInExceptions = value;
        }

        /// <inheritdoc />
        public int StormLogThreshold
        {
            get => _connectionMultiplexer.StormLogThreshold;
            set => _connectionMultiplexer.StormLogThreshold = value;
        }

        /// <inheritdoc />
        public event EventHandler<RedisErrorEventArgs> ErrorMessage
        {
            add => _connectionMultiplexer.ErrorMessage += AddEventHandler(value, _errorMessageHandlers);
            remove => _connectionMultiplexer.ErrorMessage -= RemoveEventHandler(value, _errorMessageHandlers);
        }

        /// <inheritdoc />
        public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed
        {
            add => _connectionMultiplexer.ConnectionFailed += AddEventHandler(value, _connectionFailedHandlers);
            remove => _connectionMultiplexer.ConnectionFailed -= RemoveEventHandler(value, _connectionFailedHandlers);
        }

        /// <inheritdoc />
        public event EventHandler<InternalErrorEventArgs> InternalError
        {
            add => _connectionMultiplexer.InternalError += AddEventHandler(value, _internalErrorHandlers);
            remove => _connectionMultiplexer.InternalError -= RemoveEventHandler(value, _internalErrorHandlers);
        }

        /// <inheritdoc />
        public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored
        {
            add => _connectionMultiplexer.ConnectionRestored += AddEventHandler(value, _connectionRestoredHandlers);
            remove => _connectionMultiplexer.ConnectionRestored -= RemoveEventHandler(value, _connectionRestoredHandlers);
        }

        /// <inheritdoc />
        public event EventHandler<EndPointEventArgs> ConfigurationChanged
        {
            add => _connectionMultiplexer.ConfigurationChanged += AddEventHandler(value, _configurationChangedHandlers);
            remove => _connectionMultiplexer.ConfigurationChanged -= RemoveEventHandler(value, _configurationChangedHandlers);
        }

        /// <inheritdoc />
        public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast
        {
            add => _connectionMultiplexer.ConfigurationChangedBroadcast += AddEventHandler(value, _configurationChangedBroadcastHandlers);
            remove => _connectionMultiplexer.ConfigurationChangedBroadcast -= RemoveEventHandler(value, _configurationChangedBroadcastHandlers);
        }

        /// <inheritdoc />
        public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved
        {
            add => _connectionMultiplexer.HashSlotMoved += AddEventHandler(value, _hashSlotMovedHandlers);
            remove => _connectionMultiplexer.HashSlotMoved -= RemoveEventHandler(value, _hashSlotMovedHandlers);
        }

        /// <inheritdoc />
        public void Close(bool allowCommandsToComplete = true)
        {
            ExecuteAction(() => _connectionMultiplexer.Close(allowCommandsToComplete));
        }

        /// <inheritdoc />
        public Task CloseAsync(bool allowCommandsToComplete = true)
        {
            return ExecuteActionAsync(() => _connectionMultiplexer.CloseAsync(allowCommandsToComplete));
        }

        /// <inheritdoc />
        public bool Configure(TextWriter log = null)
        {
            return ExecuteAction(() => _connectionMultiplexer.Configure(log));
        }

        /// <inheritdoc />
        public Task<bool> ConfigureAsync(TextWriter log = null)
        {
            return ExecuteActionAsync(() => _connectionMultiplexer.ConfigureAsync(log));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            // Clear handlers
            _errorMessageHandlers.Clear();
            _connectionFailedHandlers.Clear();
            _internalErrorHandlers.Clear();
            _connectionRestoredHandlers.Clear();
            _configurationChangedHandlers.Clear();
            _configurationChangedBroadcastHandlers.Clear();
            _hashSlotMovedHandlers.Clear();
            Reconnected = null;

            _profilingSessionProvider = null;

            _connectionMultiplexer.Dispose();
        }

        /// <inheritdoc />
        public void ExportConfiguration(Stream destination, ExportOptions options = (ExportOptions) (-1))
        {
            ExecuteAction(() => _connectionMultiplexer.ExportConfiguration(destination, options));
        }

        /// <inheritdoc />
        public ServerCounters GetCounters()
        {
            return ExecuteAction(() => _connectionMultiplexer.GetCounters());
        }

        /// <inheritdoc />
        public IDatabase GetDatabase(int db = -1, object asyncState = null)
        {
            return new ResilientDatabase(this, () => _connectionMultiplexer.GetDatabase(db, asyncState));
        }

        /// <inheritdoc />
        public EndPoint[] GetEndPoints(bool configuredOnly = false)
        {
            return ExecuteAction(() => _connectionMultiplexer.GetEndPoints(configuredOnly));
        }

        /// <inheritdoc />
        public int GetHashSlot(RedisKey key)
        {
            return ExecuteAction(() => _connectionMultiplexer.GetHashSlot(key));
        }

        /// <inheritdoc />
        public IServer GetServer(string host, int port, object asyncState = null)
        {
            return GetResilientServer(() => _connectionMultiplexer.GetServer(host, port, asyncState));
        }

        /// <inheritdoc />
        public IServer GetServer(string hostAndPort, object asyncState = null)
        {
            return GetResilientServer(() => _connectionMultiplexer.GetServer(hostAndPort, asyncState));
        }

        /// <inheritdoc />
        public IServer GetServer(IPAddress host, int port)
        {
            return GetResilientServer(() => _connectionMultiplexer.GetServer(host, port));
        }

        /// <inheritdoc />
        public IServer GetServer(EndPoint endpoint, object asyncState = null)
        {
            return GetResilientServer(() => _connectionMultiplexer.GetServer(endpoint, asyncState));
        }

        private IServer GetResilientServer(Func<IServer> serverProvider)
        {
            return new ResilientServer(this, serverProvider);
        }

        /// <inheritdoc />
        public string GetStatus()
        {
            return ExecuteAction(() => _connectionMultiplexer.GetStatus());
        }

        /// <inheritdoc />
        public void GetStatus(TextWriter log)
        {
            ExecuteAction(() => _connectionMultiplexer.GetStatus(log));
        }

        /// <inheritdoc />
        public string GetStormLog()
        {
            return ExecuteAction(() => _connectionMultiplexer.GetStormLog());
        }

        /// <inheritdoc />
        public ISubscriber GetSubscriber(object asyncState = null)
        {
            return new ResilientSubscriber(this, () => _connectionMultiplexer.GetSubscriber(asyncState));
        }

        /// <inheritdoc />
        public int HashSlot(RedisKey key)
        {
            return ExecuteAction(() => _connectionMultiplexer.HashSlot(key));
        }

        /// <inheritdoc />
        public long PublishReconfigure(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _connectionMultiplexer.PublishReconfigure(flags));
        }

        /// <inheritdoc />
        public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _connectionMultiplexer.PublishReconfigureAsync(flags));
        }

        /// <inheritdoc />
        public void RegisterProfiler(Func<ProfilingSession> profilingSessionProvider)
        {
            _profilingSessionProvider = profilingSessionProvider;
            ExecuteAction(() => _connectionMultiplexer.RegisterProfiler(profilingSessionProvider));
        }

        /// <inheritdoc />
        public void ResetStormLog()
        {
            ExecuteAction(() => _connectionMultiplexer.ResetStormLog());
        }

        /// <inheritdoc />
        public void Wait(Task task)
        {
            ExecuteAction(() => _connectionMultiplexer.Wait(task));
        }

        /// <inheritdoc />
        public T Wait<T>(Task<T> task)
        {
            return ExecuteAction(() => _connectionMultiplexer.Wait(task));
        }

        /// <inheritdoc />
        public void WaitAll(params Task[] tasks)
        {
            ExecuteAction(() => _connectionMultiplexer.WaitAll(tasks));
        }

        #endregion

        // Code based from: https://gist.github.com/JonCole/925630df72be1351b21440625ff2671f#file-redis-lazyreconnect-cs
        /// <inheritdoc />
        public bool TryReconnect()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(_connectionMultiplexer.ToString());
            }

            var utcNow = DateTimeOffset.UtcNow;
            var previousTicks = Interlocked.Read(ref _lastReconnectTicks);
            var previousReconnect = new DateTimeOffset(previousTicks, TimeSpan.Zero);
            var elapsedSinceLastReconnect = utcNow - previousReconnect;

            // If multiple threads call ForceReconnect at the same time, we only want to honor one of them.
            if (elapsedSinceLastReconnect < _reconnectMinFrequency)
            {
                return false;
            }

            _reconnectLock.Wait();
            try
            {
                utcNow = DateTimeOffset.UtcNow;
                elapsedSinceLastReconnect = utcNow - previousReconnect;
                if (_firstErrorDate == DateTimeOffset.MinValue)
                {
                    // We haven't seen an error since last reconnect, so set initial values.
                    _firstErrorDate = utcNow;
                    _previousErrorDate = utcNow;
                    return false;
                }

                if (elapsedSinceLastReconnect < _reconnectMinFrequency)
                {
                    return false; // Some other thread made it through the check and the lock, so nothing to do.
                }

                var elapsedSinceFirstError = utcNow - _firstErrorDate;
                var elapsedSinceMostRecentError = utcNow - _previousErrorDate;
                var shouldReconnect =
                    elapsedSinceFirstError >= _reconnectErrorThreshold // make sure we gave the multiplexer enough time to reconnect on its own if it can
                    && elapsedSinceMostRecentError <= _reconnectErrorThreshold; //make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

                // Update the previousError timestamp to be now (e.g. this reconnect request)
                _previousErrorDate = utcNow;
                if (!shouldReconnect)
                {
                    return false;
                }

                _firstErrorDate = DateTimeOffset.MinValue;
                _previousErrorDate = DateTimeOffset.MinValue;

                // Try to create a new multiplexer, do not dispose the current one if we fail to create a new one in order to prevent ObjectDisposedException
                var newMultiplexer = TryCreateMultiplexer();
                if (newMultiplexer == null)
                {
                    return false;
                }

                SetupMultiplexer(newMultiplexer, _connectionMultiplexer);
                Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);

                return true;
            }
            finally
            {
                _reconnectLock.Release();
            }
        }

        internal void AddSubscription(RedisSubscription subscription)
        {
            if (!_subscriptions.TryGetValue(subscription.Channel, out var subs))
            {
                subs = new List<RedisSubscription>();
                _subscriptions.Add(subscription.Channel, subs);
            }

            subs.Add(subscription);
        }

        internal void UnsubscribeAll()
        {
            _subscriptions.Clear();
        }

        internal bool Unsubscribe(RedisChannel channel, Delegate handler)
        {
            if (!_subscriptions.TryGetValue(channel, out var subs))
            {
                return false;
            }

            for (var i = 0; i < subs.Count; i++)
            {
                if (ReferenceEquals(subs[i].GetHandler(), handler))
                {
                    subs.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        private void SetupMultiplexer(ConnectionMultiplexer newMultiplexer, ConnectionMultiplexer oldMultiplexer)
        {
            _connectionMultiplexer = newMultiplexer;
            CloseMultiplexer(oldMultiplexer);

            // Copy properties that have a setter
            _connectionMultiplexer.IncludeDetailInExceptions = oldMultiplexer.IncludeDetailInExceptions;
            _connectionMultiplexer.StormLogThreshold = oldMultiplexer.StormLogThreshold;
            _connectionMultiplexer.IncludePerformanceCountersInExceptions = oldMultiplexer.IncludePerformanceCountersInExceptions;
            if (_profilingSessionProvider != null)
            {
                _connectionMultiplexer.RegisterProfiler(_profilingSessionProvider);
            }

            PopulateEventHandlers(_errorMessageHandlers, handler => newMultiplexer.ErrorMessage += handler);
            PopulateEventHandlers(_connectionFailedHandlers, handler => newMultiplexer.ConnectionFailed += handler);
            PopulateEventHandlers(_internalErrorHandlers, handler => newMultiplexer.InternalError += handler);
            PopulateEventHandlers(_connectionRestoredHandlers, handler => newMultiplexer.ConnectionRestored += handler);
            PopulateEventHandlers(_configurationChangedHandlers, handler => newMultiplexer.ConfigurationChanged += handler);
            PopulateEventHandlers(_configurationChangedBroadcastHandlers, handler => newMultiplexer.ConfigurationChangedBroadcast += handler);
            PopulateEventHandlers(_hashSlotMovedHandlers, handler => newMultiplexer.HashSlotMoved += handler);

            PopulateSubscribers();

            InvokeReconnectedEventHandler(new ReconnectedEventArgs(newMultiplexer, oldMultiplexer));
        }

        private void PopulateSubscribers()
        {
            var subscriber = _connectionMultiplexer.GetSubscriber();
            foreach (var pair in _subscriptions)
            {
                var channel = pair.Key;
                var subscriptions = pair.Value.ToList();
                foreach (var subscription in subscriptions)
                {
                    if (subscription.MessageQueue == null)
                    {
                        subscriber.Subscribe(channel, subscription.Handler, subscription.Flags);
                        continue;
                    }

                    if (subscription.MessageQueue.Completion.IsCompleted)
                    {
                        pair.Value.Remove(subscription);
                        continue; // The queue was unsubscribed
                    }

                    var messageQueue = subscriber.Subscribe(channel, subscription.Flags);
                    var handler = subscription.GetHandler();
                    if (handler is Action<ChannelMessage> syncHandler)
                    {
                        messageQueue.OnMessage(syncHandler);
                    }
                    else if (handler is Func<ChannelMessage, Task> asyncHandler)
                    {
                        messageQueue.OnMessage(asyncHandler);
                    }

                    ThreadPool.QueueUserWorkItem(state => UnsubscribeObserver(subscription, messageQueue), null);
                }
            }
        }

        private async void UnsubscribeObserver(RedisSubscription subscription, ChannelMessageQueue newMessageQueue)
        {
            try
            {
                await subscription.MessageQueue.Completion.ConfigureAwait(false);
            }
            catch { }
            finally
            {
                newMessageQueue.Unsubscribe(CommandFlags.FireAndForget);
                Unsubscribe(subscription.Channel, subscription.GetHandler());
            }
        }

        private void InvokeReconnectedEventHandler(ReconnectedEventArgs args)
        {
            try
            {
                Reconnected?.Invoke(this, args);
            }
            catch (Exception e)
            {
                InvokeReconnectErrorHandler(new ReconnectErrorEventArgs(e, "An error occurred while invoking Reconnected event handler."));
            }
        }

        private void InvokeReconnectErrorHandler(ReconnectErrorEventArgs args)
        {
            try
            {
                ReconnectError?.Invoke(this, args);
            }
            catch
            {
            }
        }

        private ConnectionMultiplexer TryCreateMultiplexer()
        {
            try
            {
                return _connectionMultiplexerFactory();
            }
            catch (Exception e)
            {
                InvokeReconnectErrorHandler(new ReconnectErrorEventArgs(
                    e,
                    "An error occurred while creating a ConnectionMultiplexer by using the provided factory function."));
                return null;
            }
        }

        private Task<ConnectionMultiplexer> TryCreateMultiplexerAsync()
        {
            try
            {
                return _connectionMultiplexerAsyncFactory();
            }
            catch (Exception e)
            {
                InvokeReconnectErrorHandler(new ReconnectErrorEventArgs(
                    e,
                    "An error occurred while creating a ConnectionMultiplexer by using the provided factory async function."));
                return null;
            }
        }

        private void CloseMultiplexer(ConnectionMultiplexer oldMultiplexer)
        {
            try
            {
                oldMultiplexer.Dispose();
            }
            catch (Exception e)
            {
                InvokeReconnectErrorHandler(new ReconnectErrorEventArgs(
                    e,
                    "An error occurred while trying to dispose the old multiplexer."));
            }
        }

        private EventHandler<T> AddEventHandler<T>(EventHandler<T> handler, List<EventHandler<T>> list)
        {
            lock (_eventHandlerLock)
            {
                list.Add(handler);
            }

            return handler;
        }

        private EventHandler<T> RemoveEventHandler<T>(EventHandler<T> handler, List<EventHandler<T>> list)
        {
            lock (_eventHandlerLock)
            {
                list.Remove(handler);
            }

            return handler;
        }

        private T ExecuteAction<T>(Func<T> action)
        {
            return ExecuteAction(this, action);
        }

        private Task<T> ExecuteActionAsync<T>(Func<Task<T>> action)
        {
            return ExecuteActionAsync(this, action);
        }

        private Task ExecuteActionAsync(Func<Task> action)
        {
            return ExecuteActionAsync(this, action);
        }

        private void ExecuteAction(System.Action action)
        {
            ExecuteAction(this, action);
        }

        internal static void ExecuteAction(IResilientConnectionMultiplexer resilientConnectionMultiplexer, System.Action action)
        {
            try
            {
                action();
            }
            catch (Exception e) when (e is RedisConnectionException || e is SocketException)
            {
                // ObjectDisposedException will happen on reconnecting, retry when reconnection is completed
                if (!resilientConnectionMultiplexer.TryReconnect())
                {
                    throw;
                }

                action();
            }
        }

        internal static T ExecuteAction<T>(IResilientConnectionMultiplexer resilientConnectionMultiplexer, Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception e) when (e is RedisConnectionException || e is SocketException)
            {
                // ObjectDisposedException will happen on reconnecting, retry when reconnection is completed
                if (!resilientConnectionMultiplexer.TryReconnect())
                {
                    throw;
                }

                return action();
            }
        }

        internal static async Task ExecuteActionAsync(IResilientConnectionMultiplexer resilientConnectionMultiplexer, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception e) when (e is RedisConnectionException || e is SocketException)
            {
                // ObjectDisposedException will happen on reconnecting, retry when reconnection is completed
                if (!await resilientConnectionMultiplexer.TryReconnectAsync().ConfigureAwait(false))
                {
                    throw;
                }

                await action().ConfigureAwait(false);
            }
        }

        internal static async Task<T> ExecuteActionAsync<T>(IResilientConnectionMultiplexer resilientConnectionMultiplexer, Func<Task<T>> action)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception e) when (e is RedisConnectionException || e is SocketException)
            {
                // ObjectDisposedException will happen on reconnecting, retry when reconnection is completed
                if (!await resilientConnectionMultiplexer.TryReconnectAsync().ConfigureAwait(false))
                {
                    throw;
                }

                return await action().ConfigureAwait(false);
            }
        }

        internal static void CheckAndReset(long muxLastReconnectTicks, ref long lastReconnectTicks, object lockValue, Action resetAction)
        {
            if (muxLastReconnectTicks <= Interlocked.Read(ref lastReconnectTicks))
            {
                return;
            }

            lock (lockValue)
            {
                if (muxLastReconnectTicks <= Interlocked.Read(ref lastReconnectTicks))
                {
                    return; // Some other thread made it through the check and the lock, so nothing to do.
                }

                Interlocked.Exchange(ref lastReconnectTicks, muxLastReconnectTicks);
                resetAction();
            }
        }

        private static void PopulateEventHandlers<T>(List<EventHandler<T>> handlers, Action<EventHandler<T>> addAction)
        {
            foreach (var handler in handlers)
            {
                addAction(handler);
            }
        }
    }
}
