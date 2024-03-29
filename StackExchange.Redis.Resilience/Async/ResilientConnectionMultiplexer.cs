﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis.Maintenance;
using StackExchange.Redis.Profiling;

namespace StackExchange.Redis.Resilience
{
    public partial class ResilientConnectionMultiplexer : IResilientConnectionMultiplexer
    {

        // Code based from: https://gist.github.com/JonCole/925630df72be1351b21440625ff2671f#file-redis-lazyreconnect-cs
        /// <inheritdoc />
        public Task<bool> TryReconnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(_connectionMultiplexer.ToString());
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<bool>(cancellationToken);
            }
            return InternalTryReconnectAsync();
            async Task<bool> InternalTryReconnectAsync()
            {

                var utcNow = DateTimeOffset.UtcNow;
                var previousTicks = Interlocked.Read(ref _lastReconnectTicks);
                var previousReconnect = new DateTimeOffset(previousTicks, TimeSpan.Zero);
                var elapsedSinceLastReconnect = utcNow - previousReconnect;

                // If multiple threads call ForceReconnect at the same time, we only want to honor one of them.
                if (elapsedSinceLastReconnect < _reconnectMinFrequency)
                {
                    return false;
                }

                await (_reconnectLock.WaitAsync(cancellationToken)).ConfigureAwait(false);
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
                    cancellationToken.ThrowIfCancellationRequested();

                    // Try to create a new multiplexer, do not dispose the current one if we fail to create a new one in order to prevent ObjectDisposedException
                    var newMultiplexer = await (TryCreateMultiplexerAsync()).ConfigureAwait(false);
                    if (newMultiplexer == null)
                    {
                        return false;
                    }

                    await (SetupMultiplexerAsync(newMultiplexer, _connectionMultiplexer, cancellationToken)).ConfigureAwait(false);
                    Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);

                    return true;
                }
                finally
                {
                    _reconnectLock.Release();
                }
            }
        }

        private async Task SetupMultiplexerAsync(IConnectionMultiplexer newMultiplexer, IConnectionMultiplexer oldMultiplexer, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _connectionMultiplexer = newMultiplexer;
            CloseMultiplexer(oldMultiplexer);

            // Copy properties that have a setter
#pragma warning disable CS0618
            _connectionMultiplexer.IncludeDetailInExceptions = oldMultiplexer.IncludeDetailInExceptions;
            _connectionMultiplexer.StormLogThreshold = oldMultiplexer.StormLogThreshold;
            if (_connectionMultiplexer is ConnectionMultiplexer connectionMultiplexer && oldMultiplexer is ConnectionMultiplexer oldConnectionMultiplexer)
            {
                connectionMultiplexer.IncludePerformanceCountersInExceptions = oldConnectionMultiplexer.IncludePerformanceCountersInExceptions;
            }
#pragma warning restore CS0618

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
            PopulateEventHandlers(_serverMaintenanceHandlers, handler => newMultiplexer.ServerMaintenanceEvent += handler);
            cancellationToken.ThrowIfCancellationRequested();

            await (PopulateSubscribersAsync()).ConfigureAwait(false);

            InvokeReconnectedEventHandler(new ReconnectedEventArgs(newMultiplexer, oldMultiplexer));
        }

        private async Task PopulateSubscribersAsync()
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
                        await (subscriber.SubscribeAsync(channel, subscription.Handler, subscription.Flags)).ConfigureAwait(false);
                        continue;
                    }

                    if (subscription.MessageQueue.Completion.IsCompleted)
                    {
                        pair.Value.Remove(subscription);
                        continue; // The queue was unsubscribed
                    }

                    var messageQueue = await (subscriber.SubscribeAsync(channel, subscription.Flags)).ConfigureAwait(false);
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
    }
}
