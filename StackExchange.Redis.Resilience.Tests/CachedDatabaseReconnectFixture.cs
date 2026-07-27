using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StackExchange.Redis;

namespace StackExchange.Redis.Resilience.Tests
{
    /// <summary>
    /// Verifies that an <see cref="IDatabase"/> resolved once from <see cref="ResilientConnectionMultiplexer.GetDatabase"/>
    /// and cached for the lifetime of the process (the usage pattern of
    /// Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache, the FusionCache Redis backplane and the SignalR
    /// Redis backplane) keeps working across reconnects.
    /// Ported from https://github.com/AaronLeanage/StackExchange.Redis.Resilience.Repro
    /// </summary>
    [TestFixture]
    public class CachedDatabaseReconnectFixture
    {
        private const string Key = "cached-database-repro";
        private const int SubscriptionCount = 30;
        private const int WorkerCount = 32;
        private const int ReconnectCount = 10;

        private static readonly TimeSpan ErrorThreshold = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// A cached <see cref="IDatabase"/> is hammered from many concurrent tasks while reconnects are driven
        /// underneath it. Before the fix, every reconnect produces a burst of <see cref="ObjectDisposedException"/>,
        /// because the old connection is disposed at the start of the reconnect but <see cref="ResilientConnectionMultiplexer.LastReconnectTicks"/>
        /// is only advanced once the subscribers have been restored. The cached database keeps using the disposed
        /// connection for the whole of that window.
        /// </summary>
        [Test]
        public async Task CachedDatabaseSurvivesConcurrentReconnects()
        {
            var configuration = RedisServerFixture.ConnectionString;
            using var mux = new ResilientConnectionMultiplexer(
                () => ConnectionMultiplexer.Connect(configuration),
                () => ConnectionMultiplexer.ConnectAsync(configuration),
                NewConfiguration());

            var db = mux.GetDatabase(); // resolved once and cached, as RedisCache does
            await db.StringSetAsync(Key, "value");
            await SubscribeAsync(mux);

            var counters = new Counters();
            using var cts = new CancellationTokenSource();
            var workers = Enumerable.Range(0, WorkerCount)
                .Select(_ => Task.Run(() => HammerAsync(db, counters, cts.Token)))
                .ToArray();

            var reconnects = 0;
            for (var i = 0; i < ReconnectCount; i++)
            {
                if (await ForceReconnectAsync(mux))
                {
                    reconnects++;
                }
            }

            cts.Cancel();
            await Task.WhenAll(workers);

            Assert.That(reconnects, Is.EqualTo(ReconnectCount));
            Assert.That(counters.Other, Is.EqualTo(0), counters.DescribeOther());
            Assert.That(counters.ObjectDisposed, Is.EqualTo(0),
                "the cached database was left bound to a disposed connection during a reconnect");
        }

        /// <summary>
        /// The subscriber restoration step of a reconnect throws. Before the fix, <see cref="ResilientConnectionMultiplexer.LastReconnectTicks"/>
        /// is never advanced because the throw happens first, so nothing that already holds a cached <see cref="IDatabase"/>
        /// can ever rebind to the new connection.
        /// </summary>
        [Test]
        public async Task CachedDatabaseHealsAfterSubscriberRestoreFailure()
        {
            var configuration = RedisServerFixture.ConnectionString;
            var faults = new FaultInjector();
            using var mux = new ResilientConnectionMultiplexer(
                () => FaultyMultiplexer.Wrap(ConnectionMultiplexer.Connect(configuration), faults),
                async () => FaultyMultiplexer.Wrap(await ConnectionMultiplexer.ConnectAsync(configuration), faults),
                NewConfiguration());

            var db = mux.GetDatabase(); // resolved once and cached, as RedisCache does
            await db.StringSetAsync(Key, "value");
            await SubscribeAsync(mux);

            var ticksBefore = mux.LastReconnectTicks;

            bool reconnected;
            faults.FailSubscribe = true;
            try
            {
                reconnected = await ForceReconnectAsync(mux);
            }
            finally
            {
                faults.FailSubscribe = false;
            }

            Assert.That(reconnected, Is.True,
                "a reconnect that successfully swaps the connection must report success even when restoring subscribers fails, otherwise callers relying on the boolean return value are misled");
            Assert.That(mux.LastReconnectTicks, Is.Not.EqualTo(ticksBefore),
                "LastReconnectTicks must advance even when restoring subscribers fails, otherwise callers holding a cached IDatabase never rebind to the new connection");

            for (var attempt = 0; attempt < 5; attempt++)
            {
                var value = await db.StringGetAsync(Key);
                Assert.That(value.ToString(), Is.EqualTo("value"));
                await Task.Delay(50);
            }
        }

        /// <summary>
        /// One channel failing to restore during a reconnect must not prevent the other channels from being
        /// restored on the new connection, and the failure must be reported through <see cref="ResilientConnectionMultiplexer.ReconnectError"/>
        /// rather than aborting the whole reconnect.
        /// </summary>
        [Test]
        public async Task OneFailingSubscriptionDoesNotPreventOthersFromBeingRestored()
        {
            var configuration = RedisServerFixture.ConnectionString;
            var faults = new FaultInjector();
            using var mux = new ResilientConnectionMultiplexer(
                () => FaultyMultiplexer.Wrap(ConnectionMultiplexer.Connect(configuration), faults),
                async () => FaultyMultiplexer.Wrap(await ConnectionMultiplexer.ConnectAsync(configuration), faults),
                NewConfiguration());

            var healthyChannel = RedisChannel.Literal("cached-database-repro-isolation-healthy");
            var failingChannel = RedisChannel.Literal("cached-database-repro-isolation-failing");

            var healthyReceived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var subscriber = mux.GetSubscriber();
            await subscriber.SubscribeAsync(healthyChannel, (channel, value) => healthyReceived.TrySetResult(true));
            await subscriber.SubscribeAsync(failingChannel, (channel, value) => { });

            var reconnectErrors = new List<ReconnectErrorEventArgs>();
            mux.ReconnectError += (sender, args) => reconnectErrors.Add(args);

            bool reconnected;
            faults.FailSubscribe = true;
            faults.FailSubscribeChannel = failingChannel.ToString();
            try
            {
                reconnected = await ForceReconnectAsync(mux);
            }
            finally
            {
                faults.FailSubscribe = false;
                faults.FailSubscribeChannel = null;
            }

            Assert.That(reconnected, Is.True);
            Assert.That(reconnectErrors, Has.Count.EqualTo(1),
                "the failure to restore the one bad channel should be reported, not silently swallowed or left to abort the whole reconnect");
            Assert.That(reconnectErrors[0].Exception, Is.TypeOf<RedisConnectionException>());

            // The healthy channel must have been restored on the new connection despite the other channel's failure.
            await subscriber.PublishAsync(healthyChannel, true);
            var received = await healthyReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(received, Is.True,
                "the healthy channel's subscription should have survived the other channel's restoration failure");
        }

        private static ResilientConnectionConfiguration NewConfiguration()
        {
            // The 60s/30s defaults cannot be driven from a short running test
            return new ResilientConnectionConfiguration
            {
                ReconnectMinFrequency = TimeSpan.Zero,
                ReconnectErrorThreshold = ErrorThreshold
            };
        }

        /// <summary>
        /// Registers enough subscriptions that restoring them on reconnect is real work rather than a no-op.
        /// </summary>
        private static async Task SubscribeAsync(ResilientConnectionMultiplexer mux)
        {
            var subscriber = mux.GetSubscriber();
            for (var i = 0; i < SubscriptionCount; i++)
            {
                await subscriber.SubscribeAsync(RedisChannel.Literal($"cached-database-repro-{i}"), (channel, value) => { });
            }
        }

        /// <summary>
        /// Walks TryReconnectAsync through the error window it requires: one call to record the first error, one
        /// after the threshold has elapsed to record the most recent error, then a third that actually reconnects.
        /// </summary>
        private static async Task<bool> ForceReconnectAsync(ResilientConnectionMultiplexer mux)
        {
            await mux.TryReconnectAsync();
            await Task.Delay(ErrorThreshold + TimeSpan.FromMilliseconds(50));
            await mux.TryReconnectAsync();
            await Task.Delay(10);
            return await mux.TryReconnectAsync();
        }

        private static async Task HammerAsync(IDatabase db, Counters counters, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await db.StringGetAsync(Key);
                    Interlocked.Increment(ref counters.Success);
                }
                catch (ObjectDisposedException)
                {
                    Interlocked.Increment(ref counters.ObjectDisposed);
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref counters.Other);
                    counters.OtherKinds.AddOrUpdate(e.GetType().Name, 1, (_, count) => count + 1);
                }
            }
        }

        private sealed class Counters
        {
            public long Success;
            public long ObjectDisposed;
            public long Other;

            public ConcurrentDictionary<string, int> OtherKinds { get; } = new ConcurrentDictionary<string, int>();

            public string DescribeOther()
            {
                return OtherKinds.IsEmpty
                    ? string.Empty
                    : $" ({string.Join(", ", OtherKinds.Select(pair => $"{pair.Key} x{pair.Value}"))})";
            }
        }
    }
}
