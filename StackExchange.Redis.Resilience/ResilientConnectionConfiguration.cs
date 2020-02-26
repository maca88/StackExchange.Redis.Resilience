using System;

namespace StackExchange.Redis.Resilience
{
    /// <summary>
    /// Configuration for configuring <see cref="ResilientConnectionMultiplexer.TryReconnect"/> method.
    /// </summary>
    public class ResilientConnectionConfiguration
    {
        /// <summary>
        /// The minimum time that has to pass between each reconnect. Default 60 seconds.
        /// </summary>
        public TimeSpan ReconnectMinFrequency { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The time that has to pass between the first and the last error before trying to reconnect. Default 30 seconds.
        /// </summary>
        public TimeSpan ReconnectErrorThreshold { get; set; } = TimeSpan.FromSeconds(30);
    }
}
