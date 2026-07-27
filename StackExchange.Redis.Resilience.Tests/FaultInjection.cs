using System.Reflection;
using System.Runtime.ExceptionServices;
using StackExchange.Redis;

namespace StackExchange.Redis.Resilience.Tests
{
    /// <summary>
    /// Shared switch used to make the subscriber restoration step of a reconnect fail on demand.
    /// Ported from https://github.com/AaronLeanage/StackExchange.Redis.Resilience.Repro/blob/main/FaultInjection.cs
    /// </summary>
    internal sealed class FaultInjector
    {
        public volatile bool FailSubscribe;

        /// <summary>
        /// When set, only the channel with this name fails to (re)subscribe; every other channel succeeds.
        /// When null while <see cref="FailSubscribe"/> is set, every channel fails.
        /// </summary>
        public volatile string FailSubscribeChannel;
    }

    /// <summary>
    /// Transparent decorator over a real <see cref="IConnectionMultiplexer"/>. Everything is forwarded to the inner
    /// connection, except that <see cref="IConnectionMultiplexer.GetSubscriber"/> hands out a subscriber that fails to
    /// subscribe while <see cref="FaultInjector.FailSubscribe"/> is set. That models a reconnect where the data path is
    /// healthy but re-subscribing throws, e.g. against a cluster that is still flapping.
    /// </summary>
    internal class FaultyMultiplexer : DispatchProxy
    {
        private IConnectionMultiplexer _inner;
        private FaultInjector _faults;

        public static IConnectionMultiplexer Wrap(IConnectionMultiplexer inner, FaultInjector faults)
        {
            var proxy = Create<IConnectionMultiplexer, FaultyMultiplexer>();
            var self = (FaultyMultiplexer) (object) proxy;
            self._inner = inner;
            self._faults = faults;
            return proxy;
        }

        protected override object Invoke(MethodInfo method, object[] args)
        {
            var result = Forward(_inner, method, args);
            if (method.Name == nameof(IConnectionMultiplexer.GetSubscriber) && _faults.FailSubscribe)
            {
                return FaultySubscriber.Wrap((ISubscriber) result, _faults);
            }

            return result;
        }

        internal static object Forward(object target, MethodInfo method, object[] args)
        {
            try
            {
                return method.Invoke(target, args);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                // Preserve the original exception type, the library filters on it
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }
    }

    /// <summary>
    /// Forwards every call to the real subscriber except Subscribe/SubscribeAsync, which throw for the channel(s)
    /// selected by <see cref="FaultInjector.FailSubscribeChannel"/> (or every channel, when that is null).
    /// </summary>
    internal class FaultySubscriber : DispatchProxy
    {
        private ISubscriber _inner;
        private FaultInjector _faults;

        public static ISubscriber Wrap(ISubscriber inner, FaultInjector faults)
        {
            var proxy = Create<ISubscriber, FaultySubscriber>();
            var self = (FaultySubscriber) (object) proxy;
            self._inner = inner;
            self._faults = faults;
            return proxy;
        }

        protected override object Invoke(MethodInfo method, object[] args)
        {
            if (method.Name == nameof(ISubscriber.Subscribe) || method.Name == nameof(ISubscriber.SubscribeAsync))
            {
                var channel = (RedisChannel) args[0];
                if (_faults.FailSubscribeChannel == null || _faults.FailSubscribeChannel == channel.ToString())
                {
                    throw new RedisConnectionException(
                        ConnectionFailureType.UnableToConnect,
                        $"Injected failure while restoring the subscription for channel '{channel}'");
                }
            }

            return FaultyMultiplexer.Forward(_inner, method, args);
        }
    }
}
