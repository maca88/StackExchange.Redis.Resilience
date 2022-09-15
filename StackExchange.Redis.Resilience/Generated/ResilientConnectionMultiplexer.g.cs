using System;
using System.Threading.Tasks;

#nullable enable

namespace StackExchange.Redis.Resilience
{
    public partial class ResilientConnectionMultiplexer
    {


        /// <inheritdoc />
        public StackExchange.Redis.ServerCounters GetCounters()
        {
            return ExecuteAction(() => _connectionMultiplexer.GetCounters());
        }

        /// <inheritdoc />
        public System.Net.EndPoint[] GetEndPoints(bool configuredOnly = false)
        {
            return ExecuteAction(() => _connectionMultiplexer.GetEndPoints(configuredOnly));
        }

        /// <inheritdoc />
        public void Wait(System.Threading.Tasks.Task task)
        {
            ExecuteAction(() => _connectionMultiplexer.Wait(task));
        }

        /// <inheritdoc />
        public T Wait<T>(System.Threading.Tasks.Task<T> task)
        {
            return ExecuteAction(() => _connectionMultiplexer.Wait<T>(task));
        }

        /// <inheritdoc />
        public void WaitAll(System.Threading.Tasks.Task[] tasks)
        {
            ExecuteAction(() => _connectionMultiplexer.WaitAll(tasks));
        }

        /// <inheritdoc />
        public int HashSlot(StackExchange.Redis.RedisKey key)
        {
            return ExecuteAction(() => _connectionMultiplexer.HashSlot(key));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> ConfigureAsync(System.IO.TextWriter? log = default)
        {
            return ExecuteActionAsync(() => _connectionMultiplexer.ConfigureAsync(log));
        }

        /// <inheritdoc />
        public bool Configure(System.IO.TextWriter? log = default)
        {
            return ExecuteAction(() => _connectionMultiplexer.Configure(log));
        }

        /// <inheritdoc />
        public string GetStatus()
        {
            return ExecuteAction(() => _connectionMultiplexer.GetStatus());
        }

        /// <inheritdoc />
        public void GetStatus(System.IO.TextWriter log)
        {
            ExecuteAction(() => _connectionMultiplexer.GetStatus(log));
        }

        /// <inheritdoc />
        public void Close(bool allowCommandsToComplete = true)
        {
            ExecuteAction(() => _connectionMultiplexer.Close(allowCommandsToComplete));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task CloseAsync(bool allowCommandsToComplete = true)
        {
            return ExecuteActionAsync(() => _connectionMultiplexer.CloseAsync(allowCommandsToComplete));
        }

        /// <inheritdoc />
        public string? GetStormLog()
        {
            return ExecuteAction(() => _connectionMultiplexer.GetStormLog());
        }

        /// <inheritdoc />
        public void ResetStormLog()
        {
            ExecuteAction(() => _connectionMultiplexer.ResetStormLog());
        }

        /// <inheritdoc />
        public long PublishReconfigure(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _connectionMultiplexer.PublishReconfigure(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> PublishReconfigureAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _connectionMultiplexer.PublishReconfigureAsync(flags));
        }

        /// <inheritdoc />
        public int GetHashSlot(StackExchange.Redis.RedisKey key)
        {
            return ExecuteAction(() => _connectionMultiplexer.GetHashSlot(key));
        }

        /// <inheritdoc />
        public void ExportConfiguration(System.IO.Stream destination, StackExchange.Redis.ExportOptions options = StackExchange.Redis.ExportOptions.All)
        {
            ExecuteAction(() => _connectionMultiplexer.ExportConfiguration(destination, options));
        }

        /// <inheritdoc />
        public string ClientName => _connectionMultiplexer.ClientName;

        /// <inheritdoc />
        public string Configuration => _connectionMultiplexer.Configuration;

        /// <inheritdoc />
        public int TimeoutMilliseconds => _connectionMultiplexer.TimeoutMilliseconds;

        /// <inheritdoc />
        public long OperationCount => _connectionMultiplexer.OperationCount;

        /// <inheritdoc />
        [System.ObsoleteAttribute("Not supported; if you require ordered pub/sub, please see ChannelMessageQueue", false)]
        public bool PreserveAsyncOrder
        {
            get => _connectionMultiplexer.PreserveAsyncOrder;
            set => _connectionMultiplexer.PreserveAsyncOrder = value;
        }

        /// <inheritdoc />
        public bool IsConnected => _connectionMultiplexer.IsConnected;

        /// <inheritdoc />
        public bool IsConnecting => _connectionMultiplexer.IsConnecting;

        /// <inheritdoc />
        [System.ObsoleteAttribute("Please use ConfigurationOptions.IncludeDetailInExceptions instead - this will be removed in 3.0.")]
        public bool IncludeDetailInExceptions
        {
            get => _connectionMultiplexer.IncludeDetailInExceptions;
            set => _connectionMultiplexer.IncludeDetailInExceptions = value;
        }

        /// <inheritdoc />
        public int StormLogThreshold
        {
            get => _connectionMultiplexer.StormLogThreshold;
            set => _connectionMultiplexer.StormLogThreshold = value;
        }
    }
}
