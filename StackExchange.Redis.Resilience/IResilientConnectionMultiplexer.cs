using System;

namespace StackExchange.Redis.Resilience
{
    /// <summary>
    /// A definition of a resilient <see cref="IConnectionMultiplexer"/>, that is able to do a reconnect.
    /// </summary>
    public partial interface IResilientConnectionMultiplexer : IConnectionMultiplexer
    {
        /// <summary>
        /// An event that is triggered after a successful reconnect.
        /// </summary>
        event EventHandler<ReconnectedEventArgs> Reconnected;

        /// <summary>
        /// An event that is triggered when an error occurs while trying to reconnect.
        /// </summary>
        event EventHandler<ReconnectErrorEventArgs> ReconnectError;

        /// <summary>
        /// Last reconnect occurrence which is set by <see cref="DateTimeOffset.UtcTicks"/> or zero when no reconnect occurred.
        /// </summary>
        long LastReconnectTicks { get; }

        /// <summary>
        /// Tries to reconnect.
        /// </summary>
        /// <returns>Whether the reconnect was successful.</returns>
        bool TryReconnect();
    }
}
