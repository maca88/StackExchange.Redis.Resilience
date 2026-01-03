using System;
using System.Threading;

namespace StackExchange.Redis.Resilience
{
    /// <summary>
    /// Similar to <see cref="Lazy{T}"/> except it does not cache exceptions.
    /// Copied from: https://github.com/dotnet/runtime/issues/27421#issuecomment-498962755
    /// </summary>
    internal class AtomicLazy<T>
    {
        private readonly Func<T> _factory = null!;
        private T _value = default!;
        private bool _initialized;
        private object _lock = null!;

        public AtomicLazy(Func<T> factory)
        {
            _factory = factory;
        }

        public AtomicLazy(T value)
        {
            _value = value;
            _initialized = true;
        }

        public T Value => LazyInitializer.EnsureInitialized(ref _value, ref _initialized, ref _lock, _factory);
    }
}
