using System;

namespace StackExchange.Redis.Resilience
{
    /// <summary>
    /// An event argument that contains information about an error that occurred while trying to reconnect.
    /// </summary>
    public class ReconnectErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor for creating <see cref="ReconnectErrorEventArgs"/>.
        /// </summary>
        /// <param name="exception">The exception that was thrown.</param>
        /// <param name="message"> The message describing where the error occurred.</param>
        public ReconnectErrorEventArgs(Exception exception, string message)
        {
            Exception = exception;
            Message = message;
        }

        /// <summary>
        /// The exception that was thrown.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// The message describing where the error occurred. 
        /// </summary>
        public string Message { get; }
    }
}
