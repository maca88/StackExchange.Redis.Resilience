using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace StackExchange.Redis.Resilience.Tests
{
    [TestFixture]
    public partial class ResilientConnectionMultiplexerFixture
    {
        private readonly int _timeout = 5000;

        [TestCase(true, false, false)]
        [TestCase(false, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        public void ReconnectSubscribers(bool unsubscribe, bool unsubscribeAll, bool unsubscribeAfterReconnect)
        {
            using var mux = CreateMultiplexer();
            var taskCount = 6;
            var taskValues = new List<KeyValuePair<TaskCompletionSource<int>, int>>();
            var tasks = CreateTaskList(taskValues, taskCount); // In order to avoid collection was modified exception
            var syncHandlers = CreateSyncSubscriptionHandlers(taskValues, 0, 2);
            var asyncHandlers = CreateAsyncSubscriptionHandlers(taskValues, 2, 2);
            var handlers = CreateSubscriptionHandlers(taskValues, 4, 2);

            var sub = mux.GetSubscriber();
            RedisChannel channel = "test";

            var queue1 = sub.Subscribe(channel);
            queue1.OnMessage(syncHandlers[0]);
            var queue2 = sub.Subscribe(channel);
            queue2.OnMessage(syncHandlers[1]);

            var queue3 = sub.Subscribe(channel);
            queue3.OnMessage(asyncHandlers[0]);
            var queue4 = sub.Subscribe(channel);
            queue4.OnMessage(asyncHandlers[1]);

            sub.Subscribe(channel, handlers[0]);
            sub.Subscribe(channel, handlers[1]);

            sub.Publish(channel, true);
            AssertAllSubscribed();

            if (!unsubscribeAfterReconnect)
            {
                Unsubscribe();
                tasks = CreateTaskList(taskValues, taskCount);
                sub.Publish(channel, true);
                AssertTasks();
            }

            Reconnect(mux);
            if (unsubscribeAfterReconnect)
            {
                Unsubscribe();
            }

            tasks = CreateTaskList(taskValues, taskCount);
            sub.Publish(channel, true);
            AssertTasks();

            void AssertTasks()
            {
                if (unsubscribeAll)
                {
                    AssertUnsubscribedAll();
                }
                else if (unsubscribe)
                {
                    AssertUnsubscribedTasks();
                }
                else
                {
                    AssertAllSubscribed();
                }
            }

            void AssertUnsubscribedTasks()
            {
                Assert.That(Task.WaitAll(new[] {tasks[0], tasks[3], tasks[4]}, _timeout), Is.True);
                Assert.That(tasks[1].IsCompleted, Is.False);
                Assert.That(tasks[2].IsCompleted, Is.False);
                Assert.That(tasks[5].IsCompleted, Is.False);
                Assert.That(taskValues.Select(o => o.Value), Is.EquivalentTo(new List<int> {1, 0, 0, 1, 1, 0}));
            }

            void AssertAllSubscribed()
            {
                Assert.That(Task.WaitAll(tasks, _timeout), Is.True);
                Assert.That(taskValues.Select(o => o.Value), Is.EquivalentTo(new List<int> {1, 1, 1, 1, 1, 1}));
            }

            void AssertUnsubscribedAll()
            {
                Thread.Sleep(100);
                Assert.That(tasks.Select(o => o.IsCompleted), Is.EqualTo(new[] {false, false, false, false, false, false}));
            }

            void Unsubscribe()
            {
                if (unsubscribe)
                {
                    queue2.Unsubscribe();
                    queue3.Unsubscribe();
                    sub.Unsubscribe(channel, handlers[1]);
                }

                if (unsubscribeAll)
                {
                    sub.UnsubscribeAll();
                }
            }
        }

        [Test]
        public void ReconnectEvents()
        {
            using var mux = CreateMultiplexer();
            var arguments = new List<EventArgs>();
            var totalReconnects = 0;

            EventHandler<InternalErrorEventArgs> internalError = (sender, args) => arguments.Add(args);
            EventHandler<EndPointEventArgs> configurationChanged = (sender, args) => arguments.Add(args);
            EventHandler<EndPointEventArgs> configurationChangedBroadcast = (sender, args) => arguments.Add(args);
            EventHandler<ConnectionFailedEventArgs> connectionFailed = (sender, args) => arguments.Add(args);
            EventHandler<ConnectionFailedEventArgs> connectionRestored = (sender, args) => arguments.Add(args);
            EventHandler<RedisErrorEventArgs> errorMessage = (sender, args) => arguments.Add(args);
            EventHandler<HashSlotMovedEventArgs> hashSlotMoved = (sender, args) => arguments.Add(args);

            mux.InternalError += internalError;
            mux.ConfigurationChanged += configurationChanged;
            mux.ConfigurationChangedBroadcast += configurationChangedBroadcast;
            mux.ConnectionFailed += connectionFailed;
            mux.ConnectionRestored += connectionRestored;
            mux.ErrorMessage += errorMessage;
            mux.HashSlotMoved += hashSlotMoved;
            mux.Reconnected += (sender, args) => totalReconnects++;

            AssertCalls();
            Assert.That(totalReconnects, Is.EqualTo(0));
            Reconnect(mux);
            Assert.That(totalReconnects, Is.EqualTo(1));
            AssertCalls();

            void AssertCalls()
            {
                arguments.Clear();
                CallHandlers();
                Assert.That(arguments.Where(o => o == null).ToList(), Has.Count.EqualTo(7));
            }

            void CallHandlers()
            {
                Raise<InternalErrorEventArgs>(mux, nameof(IConnectionMultiplexer.InternalError), null);
                Raise<EndPointEventArgs>(mux, nameof(IConnectionMultiplexer.ConfigurationChanged), null);
                Raise<EndPointEventArgs>(mux, nameof(IConnectionMultiplexer.ConfigurationChangedBroadcast), null);
                Raise<ConnectionFailedEventArgs>(mux, nameof(IConnectionMultiplexer.ConnectionFailed), null);
                Raise<ConnectionFailedEventArgs>(mux, nameof(IConnectionMultiplexer.ConnectionRestored), null);
                Raise<RedisErrorEventArgs>(mux, nameof(IConnectionMultiplexer.ErrorMessage), null);
                Raise<HashSlotMovedEventArgs>(mux, nameof(IConnectionMultiplexer.HashSlotMoved), null);
            }
        }

        [Test]
        public void ReconnectServerByFailure()
        {
            using var mux = CreateMultiplexer();
            var server = mux.GetServer(mux.GetEndPoints()[0]);
            server.DatabaseSize();

            // Simulate RedisConnectionException
            mux.ConnectionMultiplexer.Dispose();
            ResetDisposeField(mux.ConnectionMultiplexer);

            Assert.Throws<RedisConnectionException>(() => server.DatabaseSize()); // set first error
            Thread.Sleep(100); // (NOW - First error date) must be greater than ReconnectErrorThreshold
            Assert.Throws<RedisConnectionException>(() => server.DatabaseSize()); // set previous error
            Thread.Sleep(10); // (NOW - Last error date) must be lower than ReconnectErrorThreshold

            server.DatabaseSize(); // set last error
        }

        [Test]
        public void ReconnectDatabaseByFailure()
        {
            using var mux = CreateMultiplexer();
            var db = mux.GetDatabase();
            var key = "test";
            db.StringGet(key);

            // Simulate RedisConnectionException
            mux.ConnectionMultiplexer.Dispose();
            ResetDisposeField(mux.ConnectionMultiplexer);

            Assert.Throws<RedisConnectionException>(() => db.StringGet(key)); // set first error
            Thread.Sleep(100); // (NOW - First error date) must be greater than ReconnectErrorThreshold
            Assert.Throws<RedisConnectionException>(() => db.StringGet(key)); // set previous error
            Thread.Sleep(10); // (NOW - Last error date) must be lower than ReconnectErrorThreshold

            db.StringGet(key); // set last error
        }

        [Test]
        public void ReconnectSubscriberByFailure()
        {
            using var mux = CreateMultiplexer();
            var subscriber = mux.GetSubscriber();
            RedisChannel channel = "test";
            subscriber.Publish(channel, true);

            // Simulate RedisConnectionException
            mux.ConnectionMultiplexer.Dispose();
            ResetDisposeField(mux.ConnectionMultiplexer);

            Assert.Throws<RedisConnectionException>(() => subscriber.Publish(channel, true)); // set first error
            Thread.Sleep(100); // (NOW - First error date) must be greater than ReconnectErrorThreshold
            Assert.Throws<RedisConnectionException>(() => subscriber.Publish(channel, true)); // set previous error
            Thread.Sleep(10); // (NOW - Last error date) must be lower than ReconnectErrorThreshold

            subscriber.Publish(channel, true); // set last error
        }

        [Test]
        public void ReconnectDatabase()
        {
            using var mux = CreateMultiplexer();
            var db = mux.GetDatabase();
            db.StringSet("test", "test");

            Assert.That(db.StringGet("test").ToString(), Is.EqualTo("test"));
            Reconnect(mux);
            Assert.That(db.StringGet("test").ToString(), Is.EqualTo("test"));
        }

        [Test]
        public void ReconnectServer()
        {
            using var mux = CreateMultiplexer();
            var endpoint = mux.GetEndPoints()[0];
            var server = mux.GetServer(endpoint);
            var dbSize = server.DatabaseSize();
            Reconnect(mux);
            Assert.That(dbSize, Is.EqualTo(server.DatabaseSize()));
        }

        private static ResilientConnectionMultiplexer CreateMultiplexer()
        {
            var configuration = "127.0.0.1";
            return new ResilientConnectionMultiplexer(
                () => ConnectionMultiplexer.Connect(configuration),
                () => ConnectionMultiplexer.ConnectAsync(configuration),
                new ResilientConnectionConfiguration()
                {
                    ReconnectErrorThreshold = TimeSpan.FromMilliseconds(100),
                    ReconnectMinFrequency = TimeSpan.Zero
                });
        }

        private void Reconnect(ResilientConnectionMultiplexer mux)
        {
            var oldMux = mux.ConnectionMultiplexer;
            Assert.That(mux.TryReconnect(), Is.False); // set first error
            Thread.Sleep(100); // (NOW - First error date) must be greater than ReconnectErrorThreshold
            Assert.That(mux.TryReconnect(), Is.False); // set previous error
            Thread.Sleep(10); // (NOW - Last error date) must be lower than ReconnectErrorThreshold
            Assert.That(mux.TryReconnect(), Is.True); // set last error
            Assert.That(mux.LastReconnectTicks, Is.Not.EqualTo(0L));
            Assert.That(oldMux, Is.Not.EqualTo(mux.ConnectionMultiplexer));
        }

        private static Task[] CreateTaskList(List<KeyValuePair<TaskCompletionSource<int>, int>> tasks, int subCount)
        {
            tasks.Clear();
            for (var i = 0; i < subCount; i++)
            {
                tasks.Add(new KeyValuePair<TaskCompletionSource<int>, int>(new TaskCompletionSource<int>(), 0));
            }

            return tasks.Select(o => o.Key.Task).ToArray();
        }

        private static List<Action<RedisChannel, RedisValue>> CreateSubscriptionHandlers(
            List<KeyValuePair<TaskCompletionSource<int>, int>> tasks, int startIndex, int count)
        {
            var subHandlers = new List<Action<RedisChannel, RedisValue>>();
            for (var i = startIndex; i < startIndex + count; i++)
            {
                var index = i;
                subHandlers.Add((c, v) =>
                {
                    tasks[index] = new KeyValuePair<TaskCompletionSource<int>, int>(tasks[index].Key, tasks[index].Value + 1);
                    tasks[index].Key.SetResult(index);
                });
            }

            return subHandlers;
        }

        private static List<Action<ChannelMessage>> CreateSyncSubscriptionHandlers(
            List<KeyValuePair<TaskCompletionSource<int>, int>> tasks, int startIndex, int count)
        {
            var subHandlers = new List<Action<ChannelMessage>>();
            for (var i = startIndex; i < startIndex + count; i++)
            {
                var index = i;
                subHandlers.Add(msg =>
                {
                    tasks[index] = new KeyValuePair<TaskCompletionSource<int>, int>(tasks[index].Key, tasks[index].Value + 1);
                    tasks[index].Key.SetResult(index);
                });
            }

            return subHandlers;
        }

        private static List<Func<ChannelMessage, Task>> CreateAsyncSubscriptionHandlers(
            List<KeyValuePair<TaskCompletionSource<int>, int>> tasks, int startIndex, int count)
        {
            var subHandlers = new List<Func<ChannelMessage, Task>>();
            for (var i = startIndex; i < startIndex + count; i++)
            {
                var index = i;
                subHandlers.Add(msg =>
                {
                    tasks[index] = new KeyValuePair<TaskCompletionSource<int>, int>(tasks[index].Key, tasks[index].Value + 1);
                    tasks[index].Key.SetResult(index);
                    return Task.CompletedTask;
                });
            }

            return subHandlers;
        }

        private static void Raise<TEventArgs>(ResilientConnectionMultiplexer source, string eventName,
            TEventArgs eventArgs) where TEventArgs : EventArgs
        {
            var eventDelegate = (Delegate) source.ConnectionMultiplexer.GetType()
                .GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(source.ConnectionMultiplexer);
            eventDelegate.DynamicInvoke(source.ConnectionMultiplexer, eventArgs);
        }

        private static void ResetDisposeField(IConnectionMultiplexer multiplexer)
        {
            typeof(ConnectionMultiplexer).GetField("_isDisposed", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(multiplexer, false);
        }
    }
}
