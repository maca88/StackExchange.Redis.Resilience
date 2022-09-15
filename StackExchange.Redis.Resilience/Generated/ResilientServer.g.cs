using System;
using System.Threading.Tasks;

#nullable enable

namespace StackExchange.Redis.Resilience
{
    internal partial class ResilientServer
    {
        private readonly ResilientConnectionMultiplexer _resilientConnectionMultiplexer;
        private readonly Func<StackExchange.Redis.IServer> _instanceProvider;
        private readonly object _resetLock = new object();
        private AtomicLazy<StackExchange.Redis.IServer>? _instance;
        private long _lastReconnectTicks;

        public ResilientServer(ResilientConnectionMultiplexer resilientConnectionMultiplexer, Func<StackExchange.Redis.IServer> instanceProvider)
        {
            _resilientConnectionMultiplexer = resilientConnectionMultiplexer;
            _instanceProvider = instanceProvider;
            _lastReconnectTicks = resilientConnectionMultiplexer.LastReconnectTicks;
            ResetInstance();
        }

        /// <inheritdoc />
        public IConnectionMultiplexer Multiplexer => _resilientConnectionMultiplexer;


        /// <inheritdoc />
        public void ClientKill(System.Net.EndPoint endpoint, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.ClientKill(endpoint, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ClientKillAsync(System.Net.EndPoint endpoint, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ClientKillAsync(endpoint, flags));
        }

        /// <inheritdoc />
        public long ClientKill(long? id = default, StackExchange.Redis.ClientType? clientType = default, System.Net.EndPoint? endpoint = default, bool skipMe = true, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ClientKill(id, clientType, endpoint, skipMe, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ClientKillAsync(long? id = default, StackExchange.Redis.ClientType? clientType = default, System.Net.EndPoint? endpoint = default, bool skipMe = true, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ClientKillAsync(id, clientType, endpoint, skipMe, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.ClientInfo[] ClientList(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ClientList(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.ClientInfo[]> ClientListAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ClientListAsync(flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.ClusterConfiguration? ClusterNodes(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ClusterNodes(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.ClusterConfiguration?> ClusterNodesAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ClusterNodesAsync(flags));
        }

        /// <inheritdoc />
        public string? ClusterNodesRaw(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ClusterNodesRaw(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string?> ClusterNodesRawAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ClusterNodesRawAsync(flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.KeyValuePair<string, string>[] ConfigGet(StackExchange.Redis.RedisValue pattern = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ConfigGet(pattern, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Collections.Generic.KeyValuePair<string, string>[]> ConfigGetAsync(StackExchange.Redis.RedisValue pattern = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ConfigGetAsync(pattern, flags));
        }

        /// <inheritdoc />
        public void ConfigResetStatistics(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.ConfigResetStatistics(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ConfigResetStatisticsAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ConfigResetStatisticsAsync(flags));
        }

        /// <inheritdoc />
        public void ConfigRewrite(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.ConfigRewrite(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ConfigRewriteAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ConfigRewriteAsync(flags));
        }

        /// <inheritdoc />
        public void ConfigSet(StackExchange.Redis.RedisValue setting, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.ConfigSet(setting, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ConfigSetAsync(StackExchange.Redis.RedisValue setting, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ConfigSetAsync(setting, value, flags));
        }

        /// <inheritdoc />
        public long CommandCount(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.CommandCount(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> CommandCountAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.CommandCountAsync(flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisKey[] CommandGetKeys(StackExchange.Redis.RedisValue[] command, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.CommandGetKeys(command, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisKey[]> CommandGetKeysAsync(StackExchange.Redis.RedisValue[] command, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.CommandGetKeysAsync(command, flags));
        }

        /// <inheritdoc />
        public string[] CommandList(StackExchange.Redis.RedisValue? moduleName = default, StackExchange.Redis.RedisValue? category = default, StackExchange.Redis.RedisValue? pattern = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.CommandList(moduleName, category, pattern, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string[]> CommandListAsync(StackExchange.Redis.RedisValue? moduleName = default, StackExchange.Redis.RedisValue? category = default, StackExchange.Redis.RedisValue? pattern = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.CommandListAsync(moduleName, category, pattern, flags));
        }

        /// <inheritdoc />
        public long DatabaseSize(int database = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.DatabaseSize(database, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> DatabaseSizeAsync(int database = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.DatabaseSizeAsync(database, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue Echo(StackExchange.Redis.RedisValue message, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Echo(message, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> EchoAsync(StackExchange.Redis.RedisValue message, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.EchoAsync(message, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult Execute(string command, object[] args)
        {
            return ExecuteAction(() => _instance!.Value.Execute(command, args));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> ExecuteAsync(string command, object[] args)
        {
            return ExecuteActionAsync(() => _instance!.Value.ExecuteAsync(command, args));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult Execute(string command, System.Collections.Generic.ICollection<object> args, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Execute(command, args, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> ExecuteAsync(string command, System.Collections.Generic.ICollection<object> args, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ExecuteAsync(command, args, flags));
        }

        /// <inheritdoc />
        public void FlushAllDatabases(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.FlushAllDatabases(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task FlushAllDatabasesAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.FlushAllDatabasesAsync(flags));
        }

        /// <inheritdoc />
        public void FlushDatabase(int database = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.FlushDatabase(database, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task FlushDatabaseAsync(int database = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.FlushDatabaseAsync(database, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.ServerCounters GetCounters()
        {
            return ExecuteAction(() => _instance!.Value.GetCounters());
        }

        /// <inheritdoc />
        public System.Linq.IGrouping<string, System.Collections.Generic.KeyValuePair<string, string>>[] Info(StackExchange.Redis.RedisValue section = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Info(section, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Linq.IGrouping<string, System.Collections.Generic.KeyValuePair<string, string>>[]> InfoAsync(StackExchange.Redis.RedisValue section = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.InfoAsync(section, flags));
        }

        /// <inheritdoc />
        public string? InfoRaw(StackExchange.Redis.RedisValue section = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.InfoRaw(section, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string?> InfoRawAsync(StackExchange.Redis.RedisValue section = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.InfoRawAsync(section, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IEnumerable<StackExchange.Redis.RedisKey> Keys(int database, StackExchange.Redis.RedisValue pattern, int pageSize, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.Keys(database, pattern, pageSize, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IEnumerable<StackExchange.Redis.RedisKey> Keys(int database = -1, StackExchange.Redis.RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Keys(database, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IAsyncEnumerable<StackExchange.Redis.RedisKey> KeysAsync(int database = -1, StackExchange.Redis.RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeysAsync(database, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public System.DateTime LastSave(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LastSave(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.DateTime> LastSaveAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LastSaveAsync(flags));
        }

        /// <inheritdoc />
        [System.ObsoleteAttribute("Please use MakePrimaryAsync, this will be removed in 3.0.")]
        public void MakeMaster(StackExchange.Redis.ReplicationChangeOptions options, System.IO.TextWriter? log = default)
        {
            ExecuteAction(() => _instance!.Value.MakeMaster(options, log));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task MakePrimaryAsync(StackExchange.Redis.ReplicationChangeOptions options, System.IO.TextWriter? log = default)
        {
            return ExecuteActionAsync(() => _instance!.Value.MakePrimaryAsync(options, log));
        }

        /// <inheritdoc />
        public StackExchange.Redis.Role Role(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Role(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.Role> RoleAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.RoleAsync(flags));
        }

        /// <inheritdoc />
        public void Save(StackExchange.Redis.SaveType type, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.Save(type, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task SaveAsync(StackExchange.Redis.SaveType type, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SaveAsync(type, flags));
        }

        /// <inheritdoc />
        public bool ScriptExists(string script, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ScriptExists(script, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> ScriptExistsAsync(string script, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptExistsAsync(script, flags));
        }

        /// <inheritdoc />
        public bool ScriptExists(byte[] sha1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ScriptExists(sha1, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> ScriptExistsAsync(byte[] sha1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptExistsAsync(sha1, flags));
        }

        /// <inheritdoc />
        public void ScriptFlush(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.ScriptFlush(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ScriptFlushAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptFlushAsync(flags));
        }

        /// <inheritdoc />
        public byte[] ScriptLoad(string script, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ScriptLoad(script, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<byte[]> ScriptLoadAsync(string script, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptLoadAsync(script, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.LoadedLuaScript ScriptLoad(StackExchange.Redis.LuaScript script, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ScriptLoad(script, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.LoadedLuaScript> ScriptLoadAsync(StackExchange.Redis.LuaScript script, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptLoadAsync(script, flags));
        }

        /// <inheritdoc />
        public void Shutdown(StackExchange.Redis.ShutdownMode shutdownMode = StackExchange.Redis.ShutdownMode.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.Shutdown(shutdownMode, flags));
        }

        /// <inheritdoc />
        [System.ObsoleteAttribute("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use ReplicaOfAsync instead, this will be removed in 3.0.")]
        public void SlaveOf(System.Net.EndPoint master, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.SlaveOf(master, flags));
        }

        /// <inheritdoc />
        [System.ObsoleteAttribute("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use ReplicaOfAsync instead, this will be removed in 3.0.")]
        public System.Threading.Tasks.Task SlaveOfAsync(System.Net.EndPoint master, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SlaveOfAsync(master, flags));
        }

        /// <inheritdoc />
        [System.ObsoleteAttribute("Please use ReplicaOfAsync, this will be removed in 3.0.")]
        public void ReplicaOf(System.Net.EndPoint master, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.ReplicaOf(master, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ReplicaOfAsync(System.Net.EndPoint master, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ReplicaOfAsync(master, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.CommandTrace[] SlowlogGet(int count = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SlowlogGet(count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.CommandTrace[]> SlowlogGetAsync(int count = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SlowlogGetAsync(count, flags));
        }

        /// <inheritdoc />
        public void SlowlogReset(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.SlowlogReset(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task SlowlogResetAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SlowlogResetAsync(flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisChannel[] SubscriptionChannels(StackExchange.Redis.RedisChannel pattern = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SubscriptionChannels(pattern, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisChannel[]> SubscriptionChannelsAsync(StackExchange.Redis.RedisChannel pattern = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SubscriptionChannelsAsync(pattern, flags));
        }

        /// <inheritdoc />
        public long SubscriptionPatternCount(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SubscriptionPatternCount(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SubscriptionPatternCountAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SubscriptionPatternCountAsync(flags));
        }

        /// <inheritdoc />
        public long SubscriptionSubscriberCount(StackExchange.Redis.RedisChannel channel, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SubscriptionSubscriberCount(channel, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SubscriptionSubscriberCountAsync(StackExchange.Redis.RedisChannel channel, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SubscriptionSubscriberCountAsync(channel, flags));
        }

        /// <inheritdoc />
        public void SwapDatabases(int first, int second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.SwapDatabases(first, second, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task SwapDatabasesAsync(int first, int second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SwapDatabasesAsync(first, second, flags));
        }

        /// <inheritdoc />
        public System.DateTime Time(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Time(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.DateTime> TimeAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.TimeAsync(flags));
        }

        /// <inheritdoc />
        public string LatencyDoctor(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LatencyDoctor(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string> LatencyDoctorAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LatencyDoctorAsync(flags));
        }

        /// <inheritdoc />
        public long LatencyReset(string[]? eventNames = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LatencyReset(eventNames, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> LatencyResetAsync(string[]? eventNames = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LatencyResetAsync(eventNames, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.LatencyHistoryEntry[] LatencyHistory(string eventName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LatencyHistory(eventName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.LatencyHistoryEntry[]> LatencyHistoryAsync(string eventName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LatencyHistoryAsync(eventName, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.LatencyLatestEntry[] LatencyLatest(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LatencyLatest(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.LatencyLatestEntry[]> LatencyLatestAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LatencyLatestAsync(flags));
        }

        /// <inheritdoc />
        public string MemoryDoctor(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.MemoryDoctor(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string> MemoryDoctorAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.MemoryDoctorAsync(flags));
        }

        /// <inheritdoc />
        public void MemoryPurge(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.MemoryPurge(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task MemoryPurgeAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.MemoryPurgeAsync(flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult MemoryStats(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.MemoryStats(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> MemoryStatsAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.MemoryStatsAsync(flags));
        }

        /// <inheritdoc />
        public string? MemoryAllocatorStats(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.MemoryAllocatorStats(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string?> MemoryAllocatorStatsAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.MemoryAllocatorStatsAsync(flags));
        }

        /// <inheritdoc />
        public System.Net.EndPoint? SentinelGetMasterAddressByName(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SentinelGetMasterAddressByName(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Net.EndPoint?> SentinelGetMasterAddressByNameAsync(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelGetMasterAddressByNameAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Net.EndPoint[] SentinelGetSentinelAddresses(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SentinelGetSentinelAddresses(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Net.EndPoint[]> SentinelGetSentinelAddressesAsync(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelGetSentinelAddressesAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Net.EndPoint[] SentinelGetReplicaAddresses(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SentinelGetReplicaAddresses(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Net.EndPoint[]> SentinelGetReplicaAddressesAsync(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelGetReplicaAddressesAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.KeyValuePair<string, string>[] SentinelMaster(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SentinelMaster(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Collections.Generic.KeyValuePair<string, string>[]> SentinelMasterAsync(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelMasterAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.KeyValuePair<string, string>[][] SentinelMasters(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SentinelMasters(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Collections.Generic.KeyValuePair<string, string>[][]> SentinelMastersAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelMastersAsync(flags));
        }

        /// <inheritdoc />
        [System.ObsoleteAttribute("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use SentinelReplicas instead, this will be removed in 3.0.")]
        public System.Collections.Generic.KeyValuePair<string, string>[][] SentinelSlaves(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SentinelSlaves(serviceName, flags));
        }

        /// <inheritdoc />
        [System.ObsoleteAttribute("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use SentinelReplicasAsync instead, this will be removed in 3.0.")]
        public System.Threading.Tasks.Task<System.Collections.Generic.KeyValuePair<string, string>[][]> SentinelSlavesAsync(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelSlavesAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.KeyValuePair<string, string>[][] SentinelReplicas(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SentinelReplicas(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Collections.Generic.KeyValuePair<string, string>[][]> SentinelReplicasAsync(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelReplicasAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public void SentinelFailover(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.SentinelFailover(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task SentinelFailoverAsync(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelFailoverAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.KeyValuePair<string, string>[][] SentinelSentinels(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SentinelSentinels(serviceName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Collections.Generic.KeyValuePair<string, string>[][]> SentinelSentinelsAsync(string serviceName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SentinelSentinelsAsync(serviceName, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.ClusterConfiguration? ClusterConfiguration => _instance!.Value.ClusterConfiguration;

        /// <inheritdoc />
        public System.Net.EndPoint EndPoint => _instance!.Value.EndPoint;

        /// <inheritdoc />
        public StackExchange.Redis.RedisFeatures Features => _instance!.Value.Features;

        /// <inheritdoc />
        public bool IsConnected => _instance!.Value.IsConnected;

        /// <inheritdoc />
        [System.ObsoleteAttribute("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use IsReplica instead, this will be removed in 3.0.")]
        public bool IsSlave => _instance!.Value.IsSlave;

        /// <inheritdoc />
        public bool IsReplica => _instance!.Value.IsReplica;

        /// <inheritdoc />
        [System.ObsoleteAttribute("Starting with Redis version 5, Redis has moved to 'replica' terminology. Please use AllowReplicaWrites instead, this will be removed in 3.0.")]
        public bool AllowSlaveWrites
        {
            get => _instance!.Value.AllowSlaveWrites;
            set => _instance!.Value.AllowSlaveWrites = value;
        }

        /// <inheritdoc />
        public bool AllowReplicaWrites
        {
            get => _instance!.Value.AllowReplicaWrites;
            set => _instance!.Value.AllowReplicaWrites = value;
        }

        /// <inheritdoc />
        public StackExchange.Redis.ServerType ServerType => _instance!.Value.ServerType;

        /// <inheritdoc />
        public System.Version Version => _instance!.Value.Version;

        /// <inheritdoc />
        public int DatabaseCount => _instance!.Value.DatabaseCount;

        /// <inheritdoc />
        public System.TimeSpan Ping(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Ping(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.TimeSpan> PingAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.PingAsync(flags));
        }

        /// <inheritdoc />
        public bool TryWait(System.Threading.Tasks.Task task)
        {
            return ExecuteAction(() => _instance!.Value.TryWait(task));
        }

        /// <inheritdoc />
        public void Wait(System.Threading.Tasks.Task task)
        {
            ExecuteAction(() => _instance!.Value.Wait(task));
        }

        /// <inheritdoc />
        public T Wait<T>(System.Threading.Tasks.Task<T> task)
        {
            return ExecuteAction(() => _instance!.Value.Wait<T>(task));
        }

        /// <inheritdoc />
        public void WaitAll(System.Threading.Tasks.Task[] tasks)
        {
            ExecuteAction(() => _instance!.Value.WaitAll(tasks));
        }

        private void ResetInstance()
        {
            _instance = new AtomicLazy<StackExchange.Redis.IServer>(_instanceProvider);
        }

        private void CheckAndReset()
        {
            ResilientConnectionMultiplexer.CheckAndReset(
                _resilientConnectionMultiplexer.LastReconnectTicks,
                ref _lastReconnectTicks,
                _resetLock,
                ResetInstance);
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
