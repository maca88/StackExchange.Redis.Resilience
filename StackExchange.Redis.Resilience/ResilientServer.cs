using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace StackExchange.Redis.Resilience
{
    internal class ResilientServer : IServer
    {
        private readonly IResilientConnectionMultiplexer _resilientConnectionMultiplexer;
        private readonly Func<IServer> _serverProvider;
        private readonly object _resetLock = new object();
        private AtomicLazy<IServer> _server;
        private long _lastReconnectTicks;

        public ResilientServer(IResilientConnectionMultiplexer resilientConnectionMultiplexer,
            Func<IServer> serverProvider)
        {
            _resilientConnectionMultiplexer = resilientConnectionMultiplexer;
            _serverProvider = serverProvider;
            _lastReconnectTicks = resilientConnectionMultiplexer.LastReconnectTicks;
            ResetServer();
        }

        #region IServer implementation

        /// <inheritdoc />
        public ClusterConfiguration ClusterConfiguration => _server.Value.ClusterConfiguration;

        /// <inheritdoc />
        public EndPoint EndPoint => _server.Value.EndPoint;

        /// <inheritdoc />
        public RedisFeatures Features => _server.Value.Features;

        /// <inheritdoc />
        public bool IsConnected => _server.Value.IsConnected;

        /// <inheritdoc />
        [Obsolete("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use IsReplica instead.")]
        public bool IsSlave => _server.Value.IsSlave;

        public bool IsReplica
        {
            get => _server.Value.IsReplica;
        }

        /// <inheritdoc />
        [Obsolete("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use IsReplica instead.")]
        public bool AllowSlaveWrites
        {
            get => _server.Value.AllowSlaveWrites;
            set => _server.Value.AllowSlaveWrites = value;
        }

        public bool AllowReplicaWrites
        {
            get => _server.Value.AllowReplicaWrites;
            set => _server.Value.AllowReplicaWrites = value;
        }

        /// <inheritdoc />
        public ServerType ServerType => _server.Value.ServerType;

        /// <inheritdoc />
        public Version Version => _server.Value.Version;

        /// <inheritdoc />
        public int DatabaseCount => _server.Value.DatabaseCount;

        /// <inheritdoc />
        public IConnectionMultiplexer Multiplexer => _resilientConnectionMultiplexer;

        /// <inheritdoc />
        public void ClientKill(EndPoint endpoint, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.ClientKill(endpoint, flags));
        }

        /// <inheritdoc />
        public long ClientKill(long? id = null, ClientType? clientType = null, EndPoint endpoint = null,
            bool skipMe = true, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ClientKill(id, clientType, endpoint, skipMe, flags));
        }

        /// <inheritdoc />
        public Task ClientKillAsync(EndPoint endpoint, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ClientKillAsync(endpoint, flags));
        }

        /// <inheritdoc />
        public Task<long> ClientKillAsync(long? id = null, ClientType? clientType = null, EndPoint endpoint = null,
            bool skipMe = true, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ClientKillAsync(id, clientType, endpoint, skipMe, flags));
        }

        /// <inheritdoc />
        public ClientInfo[] ClientList(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ClientList(flags));
        }

        /// <inheritdoc />
        public Task<ClientInfo[]> ClientListAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ClientListAsync(flags));
        }

        /// <inheritdoc />
        public ClusterConfiguration ClusterNodes(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ClusterNodes(flags));
        }

        /// <inheritdoc />
        public Task<ClusterConfiguration> ClusterNodesAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ClusterNodesAsync(flags));
        }

        /// <inheritdoc />
        public string ClusterNodesRaw(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ClusterNodesRaw(flags));
        }

        /// <inheritdoc />
        public Task<string> ClusterNodesRawAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ClusterNodesRawAsync(flags));
        }

        /// <inheritdoc />
        public KeyValuePair<string, string>[] ConfigGet(RedisValue pattern = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ConfigGet(pattern, flags));
        }

        /// <inheritdoc />
        public Task<KeyValuePair<string, string>[]> ConfigGetAsync(RedisValue pattern = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ConfigGetAsync(pattern, flags));
        }

        /// <inheritdoc />
        public void ConfigResetStatistics(CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.ConfigResetStatistics(flags));
        }

        /// <inheritdoc />
        public Task ConfigResetStatisticsAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ConfigResetStatisticsAsync(flags));
        }

        /// <inheritdoc />
        public void ConfigRewrite(CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.ConfigRewrite(flags));
        }

        /// <inheritdoc />
        public Task ConfigRewriteAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ConfigRewriteAsync(flags));
        }

        /// <inheritdoc />
        public void ConfigSet(RedisValue setting, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.ConfigSet(setting, value, flags));
        }

        /// <inheritdoc />
        public Task ConfigSetAsync(RedisValue setting, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ConfigSetAsync(setting, value, flags));
        }

        /// <inheritdoc />
        public long DatabaseSize(int database = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.DatabaseSize(database, flags));
        }

        /// <inheritdoc />
        public Task<long> DatabaseSizeAsync(int database = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.DatabaseSizeAsync(database, flags));
        }

        /// <inheritdoc />
        public RedisValue Echo(RedisValue message, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.Echo(message, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> EchoAsync(RedisValue message, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.EchoAsync(message, flags));
        }

        /// <inheritdoc />
        public RedisResult Execute(string command, params object[] args)
        {
            return ExecuteAction(() => _server.Value.Execute(command, args));
        }

        /// <inheritdoc />
        public RedisResult Execute(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.Execute(command, args, flags));
        }

        /// <inheritdoc />
        public Task<RedisResult> ExecuteAsync(string command, params object[] args)
        {
            return ExecuteActionAsync(() => _server.Value.ExecuteAsync(command, args));
        }

        /// <inheritdoc />
        public Task<RedisResult> ExecuteAsync(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ExecuteAsync(command, args, flags));
        }

        /// <inheritdoc />
        public void FlushAllDatabases(CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.FlushAllDatabases(flags));
        }

        /// <inheritdoc />
        public Task FlushAllDatabasesAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.FlushAllDatabasesAsync(flags));
        }

        /// <inheritdoc />
        public void FlushDatabase(int database = 0, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.FlushDatabase(database, flags));
        }

        /// <inheritdoc />
        public Task FlushDatabaseAsync(int database = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.FlushDatabaseAsync(database, flags));
        }

        /// <inheritdoc />
        public ServerCounters GetCounters()
        {
            return ExecuteAction(() => _server.Value.GetCounters());
        }

        /// <inheritdoc />
        public IGrouping<string, KeyValuePair<string, string>>[] Info(RedisValue section = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.Info(section, flags));
        }

        /// <inheritdoc />
        public Task<IGrouping<string, KeyValuePair<string, string>>[]> InfoAsync(RedisValue section = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.InfoAsync(section, flags));
        }

        /// <inheritdoc />
        public string InfoRaw(RedisValue section = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.InfoRaw(section, flags));
        }

        /// <inheritdoc />
        public Task<string> InfoRawAsync(RedisValue section = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.InfoRawAsync(section, flags));
        }

        /// <inheritdoc />
        public IEnumerable<RedisKey> Keys(int database, RedisValue pattern, int pageSize, CommandFlags flags)
        {
            return ExecuteAction(() => _server.Value.Keys(database, pattern, pageSize, flags));
        }

        /// <inheritdoc />
        public IEnumerable<RedisKey> Keys(int database = 0, RedisValue pattern = default, int pageSize = 10,
            long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.Keys(database, pattern, pageSize, cursor, pageOffset, flags));
        }

        public IAsyncEnumerable<RedisKey> KeysAsync(int database = -1, RedisValue pattern = new RedisValue(), int pageSize = 250,
            long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.KeysAsync(database, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public DateTime LastSave(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.LastSave(flags));
        }

        /// <inheritdoc />
        public Task<DateTime> LastSaveAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.LastSaveAsync(flags));
        }

        /// <inheritdoc />
        public void MakeMaster(ReplicationChangeOptions options, TextWriter log = null)
        {
            ExecuteAction(() => _server.Value.MakeMaster(options, log));
        }

        public Role Role(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.Role(flags));
        }

        public Task<Role> RoleAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.RoleAsync(flags));
        }

        /// <inheritdoc />
        public TimeSpan Ping(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.Ping(flags));
        }

        /// <inheritdoc />
        public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.PingAsync(flags));
        }

        /// <inheritdoc />
        public void Save(SaveType type, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.Save(type, flags));
        }

        /// <inheritdoc />
        public Task SaveAsync(SaveType type, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SaveAsync(type, flags));
        }

        /// <inheritdoc />
        public bool ScriptExists(string script, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ScriptExists(script, flags));
        }

        /// <inheritdoc />
        public bool ScriptExists(byte[] sha1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ScriptExists(sha1, flags));
        }

        /// <inheritdoc />
        public Task<bool> ScriptExistsAsync(string script, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ScriptExistsAsync(script, flags));
        }

        /// <inheritdoc />
        public Task<bool> ScriptExistsAsync(byte[] sha1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ScriptExistsAsync(sha1, flags));
        }

        /// <inheritdoc />
        public void ScriptFlush(CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.ScriptFlush(flags));
        }

        /// <inheritdoc />
        public Task ScriptFlushAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ScriptFlushAsync(flags));
        }

        /// <inheritdoc />
        public byte[] ScriptLoad(string script, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ScriptLoad(script, flags));
        }

        /// <inheritdoc />
        public LoadedLuaScript ScriptLoad(LuaScript script, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.ScriptLoad(script, flags));
        }

        /// <inheritdoc />
        public Task<byte[]> ScriptLoadAsync(string script, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ScriptLoadAsync(script, flags));
        }

        /// <inheritdoc />
        public Task<LoadedLuaScript> ScriptLoadAsync(LuaScript script, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ScriptLoadAsync(script, flags));
        }

        public Task<KeyValuePair<string, string>[][]> SentinelReplicasAsync(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelReplicasAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public void SentinelFailover(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.SentinelFailover(serviceName, flags));
        }

        /// <inheritdoc />
        public Task SentinelFailoverAsync(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelFailoverAsync(serviceName, flags));
        }

        public string MemoryAllocatorStats(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.MemoryAllocatorStats(flags));
        }

        /// <inheritdoc />
        public EndPoint SentinelGetMasterAddressByName(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SentinelGetMasterAddressByName(serviceName, flags));
        }

        /// <inheritdoc />
        public Task<EndPoint> SentinelGetMasterAddressByNameAsync(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelGetMasterAddressByNameAsync(serviceName, flags));
        }

        public EndPoint[] SentinelGetSentinelAddresses(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SentinelGetSentinelAddresses(serviceName, flags));
        }

        public Task<EndPoint[]> SentinelGetSentinelAddressesAsync(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelGetSentinelAddressesAsync(serviceName, flags));
        }

        public EndPoint[] SentinelGetReplicaAddresses(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SentinelGetReplicaAddresses(serviceName, flags));
        }

        public Task<EndPoint[]> SentinelGetReplicaAddressesAsync(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelGetReplicaAddressesAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public KeyValuePair<string, string>[] SentinelMaster(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SentinelMaster(serviceName, flags));
        }

        /// <inheritdoc />
        public Task<KeyValuePair<string, string>[]> SentinelMasterAsync(string serviceName,
            CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelMasterAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public KeyValuePair<string, string>[][] SentinelMasters(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SentinelMasters(flags));
        }

        /// <inheritdoc />
        public Task<KeyValuePair<string, string>[][]> SentinelMastersAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelMastersAsync(flags));
        }

        /// <inheritdoc />
        public KeyValuePair<string, string>[][] SentinelSentinels(string serviceName,
            CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SentinelSentinels(serviceName, flags));
        }

        /// <inheritdoc />
        public Task<KeyValuePair<string, string>[][]> SentinelSentinelsAsync(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelSentinelsAsync(serviceName, flags));
        }

        /// <inheritdoc />
        [Obsolete("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use IsReplica instead.")]
        public KeyValuePair<string, string>[][] SentinelSlaves(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SentinelSlaves(serviceName, flags));
        }

        public KeyValuePair<string, string>[][] SentinelReplicas(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SentinelReplicas(serviceName, flags));
        }

        /// <inheritdoc />
        [Obsolete("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use IsReplica instead.")]
        public Task<KeyValuePair<string, string>[][]> SentinelSlavesAsync(string serviceName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SentinelSlavesAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public void Shutdown(ShutdownMode shutdownMode = ShutdownMode.Default, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.Shutdown(shutdownMode, flags));
        }

        /// <inheritdoc />
        [Obsolete("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use IsReplica instead.")]
        public void SlaveOf(EndPoint master, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.SlaveOf(master, flags));
        }

        public void ReplicaOf(EndPoint master, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.ReplicaOf(master, flags));
        }

        /// <inheritdoc />
        [Obsolete("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use IsReplica instead.")]
        public Task SlaveOfAsync(EndPoint master, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SlaveOfAsync(master, flags));
        }

        public Task ReplicaOfAsync(EndPoint master, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.ReplicaOfAsync(master, flags));
        }

        /// <inheritdoc />
        public CommandTrace[] SlowlogGet(int count = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SlowlogGet(count, flags));
        }

        /// <inheritdoc />
        public Task<CommandTrace[]> SlowlogGetAsync(int count = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SlowlogGetAsync(count, flags));
        }

        /// <inheritdoc />
        public void SlowlogReset(CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.SlowlogReset(flags));
        }

        /// <inheritdoc />
        public Task SlowlogResetAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SlowlogResetAsync(flags));
        }

        /// <inheritdoc />
        public RedisChannel[] SubscriptionChannels(RedisChannel pattern = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SubscriptionChannels(pattern, flags));
        }

        /// <inheritdoc />
        public Task<RedisChannel[]> SubscriptionChannelsAsync(RedisChannel pattern = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SubscriptionChannelsAsync(pattern, flags));
        }

        /// <inheritdoc />
        public long SubscriptionPatternCount(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SubscriptionPatternCount(flags));
        }

        /// <inheritdoc />
        public Task<long> SubscriptionPatternCountAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SubscriptionPatternCountAsync(flags));
        }

        /// <inheritdoc />
        public long SubscriptionSubscriberCount(RedisChannel channel, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.SubscriptionSubscriberCount(channel, flags));
        }

        /// <inheritdoc />
        public Task<long> SubscriptionSubscriberCountAsync(RedisChannel channel, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SubscriptionSubscriberCountAsync(channel, flags));
        }

        /// <inheritdoc />
        public void SwapDatabases(int first, int second, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.SwapDatabases(first, second, flags));
        }

        /// <inheritdoc />
        public Task SwapDatabasesAsync(int first, int second, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.SwapDatabasesAsync(first, second, flags));
        }

        /// <inheritdoc />
        public DateTime Time(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.Time(flags));
        }

        /// <inheritdoc />
        public Task<DateTime> TimeAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.TimeAsync(flags));
        }

        public Task<string> LatencyDoctorAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.LatencyDoctorAsync(flags));
        }

        public string LatencyDoctor(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.LatencyDoctor(flags));
        }

        public Task<long> LatencyResetAsync(string[] eventNames = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.LatencyResetAsync(eventNames, flags));
        }

        public long LatencyReset(string[] eventNames = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.LatencyReset(eventNames, flags));
        }

        public Task<LatencyHistoryEntry[]> LatencyHistoryAsync(string eventName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.LatencyHistoryAsync(eventName, flags));
        }

        public LatencyHistoryEntry[] LatencyHistory(string eventName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.LatencyHistory(eventName, flags));
        }

        public Task<LatencyLatestEntry[]> LatencyLatestAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.LatencyLatestAsync(flags));
        }

        public LatencyLatestEntry[] LatencyLatest(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.LatencyLatest(flags));
        }

        public Task<string> MemoryDoctorAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.MemoryDoctorAsync(flags));
        }

        public string MemoryDoctor(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.MemoryDoctor(flags));
        }

        public Task MemoryPurgeAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.MemoryPurgeAsync(flags));
        }

        public void MemoryPurge(CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _server.Value.MemoryPurge(flags));
        }

        public Task<RedisResult> MemoryStatsAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.MemoryStatsAsync(flags));
        }

        public RedisResult MemoryStats(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _server.Value.MemoryStats(flags));
        }

        public Task<string> MemoryAllocatorStatsAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _server.Value.MemoryAllocatorStatsAsync(flags));
        }

        /// <inheritdoc />
        public bool TryWait(Task task)
        {
            return ExecuteAction(() => _server.Value.TryWait(task));
        }

        /// <inheritdoc />
        public void Wait(Task task)
        {
            ExecuteAction(() => _server.Value.Wait(task));
        }

        /// <inheritdoc />
        public T Wait<T>(Task<T> task)
        {
            return ExecuteAction(() => _server.Value.Wait(task));
        }

        /// <inheritdoc />
        public void WaitAll(params Task[] tasks)
        {
            ExecuteAction(() => _server.Value.WaitAll(tasks));
        }

        #endregion

        private void ResetServer()
        {
            _server = new AtomicLazy<IServer>(_serverProvider);
        }

        private void CheckAndReset()
        {
            ResilientConnectionMultiplexer.CheckAndReset(
                _resilientConnectionMultiplexer.LastReconnectTicks,
                ref _lastReconnectTicks,
                _resetLock,
                ResetServer);
        }

        private T ExecuteAction<T>(Func<T> action)
        {
            return ResilientConnectionMultiplexer.ExecuteAction(_resilientConnectionMultiplexer, () =>
            {
                CheckAndReset();
                return action();
            });
        }

        private Task<T> ExecuteActionAsync<T>(Func<Task<T>> action)
        {
            return ResilientConnectionMultiplexer.ExecuteActionAsync(_resilientConnectionMultiplexer, () =>
            {
                CheckAndReset();
                return action();
            });
        }

        private Task ExecuteActionAsync(Func<Task> action)
        {
            return ResilientConnectionMultiplexer.ExecuteActionAsync(_resilientConnectionMultiplexer, () =>
            {
                CheckAndReset();
                return action();
            });
        }

        private void ExecuteAction(Action action)
        {
            ResilientConnectionMultiplexer.ExecuteAction(_resilientConnectionMultiplexer, () =>
            {
                CheckAndReset();
                action();
            });
        }
    }
}
