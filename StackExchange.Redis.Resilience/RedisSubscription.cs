using System;
using System.Linq.Expressions;
using System.Reflection;

namespace StackExchange.Redis.Resilience
{
    internal class RedisSubscription
    {
        private static readonly Func<ChannelMessageQueue, Delegate> GetMessageQueueDelegate;

        static RedisSubscription()
        {
            var field = typeof(ChannelMessageQueue).GetField("_onMessageHandler",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException($"{typeof(ChannelMessageQueue).FullName} does not have _onMessageHandler field.");
            }

            var p = Expression.Parameter(typeof(ChannelMessageQueue));
            GetMessageQueueDelegate = Expression.Lambda<Func<ChannelMessageQueue, Delegate>>(Expression.Field(p, field), p).Compile();
        }

        public RedisSubscription(RedisChannel channel, Action<RedisChannel, RedisValue> handler, CommandFlags flags)
        {
            Channel = channel;
            Handler = handler;
            Flags = flags;
        }

        public RedisSubscription(RedisChannel channel, ChannelMessageQueue messageQueue, CommandFlags flags)
        {
            Channel = channel;
            MessageQueue = messageQueue;
            Flags = flags;
        }

        public RedisChannel Channel { get; }

        public ChannelMessageQueue? MessageQueue { get; }

        public Action<RedisChannel, RedisValue>? Handler { get; }

        public CommandFlags Flags { get; }

        public Delegate GetHandler()
        {
            if (MessageQueue != null)
            {
                return GetMessageQueueDelegate(MessageQueue);
            }

            return Handler!;
        }
    }
}
