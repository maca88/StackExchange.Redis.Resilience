using System;

namespace StackExchange.Redis.Resilience
{
    /// <summary>
    /// An event argument that contains information about the old and the new <see cref="ConnectionMultiplexer"/> after a successful reconnect.
    /// </summary>
    public class ReconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor for creating <see cref="ReconnectedEventArgs"/>.
        /// </summary>
        /// <param name="newMultiplexer">The new connection multiplexer.</param>
        /// <param name="oldMultiplexer">The old connection multiplexer.</param>
        public ReconnectedEventArgs(IConnectionMultiplexer newMultiplexer, IConnectionMultiplexer oldMultiplexer)
        {
            NewConnectionMultiplexer = newMultiplexer;
            OldConnectionMultiplexer = oldMultiplexer;
        }

        /// <summary>
        /// The new connection multiplexer.
        /// </summary>
        public IConnectionMultiplexer NewConnectionMultiplexer { get; }

        /// <summary>
        /// The old connection multiplexer.
        /// </summary>
        public IConnectionMultiplexer OldConnectionMultiplexer { get; }
    }
}
