using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace StackExchange.Redis.Resilience
{
    internal class ResilientDatabase : IDatabase
    {
        private readonly IResilientConnectionMultiplexer _resilientConnectionMultiplexer;
        private readonly Func<IDatabase> _databaseProvider;
        private readonly object _resetLock = new object();
        private AtomicLazy<IDatabase> _database;
        private long _lastReconnectTicks;

        public ResilientDatabase(IResilientConnectionMultiplexer resilientConnectionMultiplexer, Func<IDatabase> databaseProvider)
        {
            _resilientConnectionMultiplexer = resilientConnectionMultiplexer;
            _databaseProvider = databaseProvider;
            _lastReconnectTicks = resilientConnectionMultiplexer.LastReconnectTicks;
            ResetDatabase();
        }

        #region IDatabase implementation

        public long KeyTouch(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyTouch(keys, flags));
        }

        /// <inheritdoc />
        public int Database => _database.Value.Database;

        /// <inheritdoc />
        public IConnectionMultiplexer Multiplexer => _resilientConnectionMultiplexer;

        /// <inheritdoc />
        public IBatch CreateBatch(object asyncState = null)
        {
            return ExecuteAction(() => _database.Value.CreateBatch(asyncState));
        }

        /// <inheritdoc />
        public ITransaction CreateTransaction(object asyncState = null)
        {
            return ExecuteAction(() => _database.Value.CreateTransaction(asyncState));
        }

        /// <inheritdoc />
        /// <inheritdoc />
        public RedisValue DebugObject(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.DebugObject(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> DebugObjectAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.DebugObjectAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisResult Execute(string command, params object[] args)
        {
            return ExecuteAction(() => _database.Value.Execute(command, args));
        }

        /// <inheritdoc />
        public RedisResult Execute(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.Execute(command, args, flags));
        }

        /// <inheritdoc />
        public Task<RedisResult> ExecuteAsync(string command, params object[] args)
        {
            return ExecuteActionAsync(() => _database.Value.ExecuteAsync(command, args));
        }

        /// <inheritdoc />
        public Task<RedisResult> ExecuteAsync(string command, ICollection<object> args, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ExecuteAsync(command, args, flags));
        }

        /// <inheritdoc />
        public bool GeoAdd(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoAdd(key, longitude, latitude, member, flags));
        }

        /// <inheritdoc />
        public bool GeoAdd(RedisKey key, GeoEntry value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoAdd(key, value, flags));
        }

        /// <inheritdoc />
        public long GeoAdd(RedisKey key, GeoEntry[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoAdd(key, values, flags));
        }

        /// <inheritdoc />
        public Task<bool> GeoAddAsync(RedisKey key, double longitude, double latitude, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoAddAsync(key, longitude, latitude, member, flags));
        }

        /// <inheritdoc />
        public Task<bool> GeoAddAsync(RedisKey key, GeoEntry value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoAddAsync(key, value, flags));
        }

        /// <inheritdoc />
        public Task<long> GeoAddAsync(RedisKey key, GeoEntry[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoAddAsync(key, values, flags));
        }

        /// <inheritdoc />
        public double? GeoDistance(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = GeoUnit.Meters, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoDistance(key, member1, member2, unit, flags));
        }

        /// <inheritdoc />
        public Task<double?> GeoDistanceAsync(RedisKey key, RedisValue member1, RedisValue member2, GeoUnit unit = GeoUnit.Meters, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoDistanceAsync(key, member1, member2, unit, flags));
        }

        /// <inheritdoc />
        public string[] GeoHash(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoHash(key, members, flags));
        }

        /// <inheritdoc />
        public string GeoHash(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoHash(key, member, flags));
        }

        /// <inheritdoc />
        public Task<string[]> GeoHashAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoHashAsync(key, members, flags));
        }

        /// <inheritdoc />
        public Task<string> GeoHashAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoHashAsync(key, member, flags));
        }

        /// <inheritdoc />
        public GeoPosition?[] GeoPosition(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoPosition(key, members, flags));
        }

        /// <inheritdoc />
        public GeoPosition? GeoPosition(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoPosition(key, member, flags));
        }

        /// <inheritdoc />
        public Task<GeoPosition?[]> GeoPositionAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoPositionAsync(key, members, flags));
        }

        /// <inheritdoc />
        public Task<GeoPosition?> GeoPositionAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoPositionAsync(key, member, flags));
        }

        /// <inheritdoc />
        public GeoRadiusResult[] GeoRadius(RedisKey key, RedisValue member, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoRadius(key, member, radius, unit, count, order, options, flags));
        }

        /// <inheritdoc />
        public GeoRadiusResult[] GeoRadius(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoRadius(key, longitude, latitude, radius, unit, count, order, options, flags));
        }

        /// <inheritdoc />
        public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, RedisValue member, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoRadiusAsync(key, member, radius, unit, count, order, options, flags));
        }

        /// <inheritdoc />
        public Task<GeoRadiusResult[]> GeoRadiusAsync(RedisKey key, double longitude, double latitude, double radius, GeoUnit unit = GeoUnit.Meters, int count = -1, Order? order = null, GeoRadiusOptions options = GeoRadiusOptions.Default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoRadiusAsync(key, longitude, latitude, radius, unit, count, order, options, flags));
        }

        /// <inheritdoc />
        public bool GeoRemove(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.GeoRemove(key, member, flags));
        }

        /// <inheritdoc />
        public Task<bool> GeoRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.GeoRemoveAsync(key, member, flags));
        }

        /// <inheritdoc />
        public long HashDecrement(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashDecrement(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public double HashDecrement(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashDecrement(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public Task<long> HashDecrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashDecrementAsync(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public Task<double> HashDecrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashDecrementAsync(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public bool HashDelete(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashDelete(key, hashField, flags));
        }

        /// <inheritdoc />
        public long HashDelete(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashDelete(key, hashFields, flags));
        }

        /// <inheritdoc />
        public Task<bool> HashDeleteAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashDeleteAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public Task<long> HashDeleteAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashDeleteAsync(key, hashFields, flags));
        }

        /// <inheritdoc />
        public bool HashExists(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashExists(key, hashField, flags));
        }

        /// <inheritdoc />
        public Task<bool> HashExistsAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashExistsAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public RedisValue HashGet(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashGet(key, hashField, flags));
        }

        /// <inheritdoc />
        public RedisValue[] HashGet(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashGet(key, hashFields, flags));
        }

        /// <inheritdoc />
        public HashEntry[] HashGetAll(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashGetAll(key, flags));
        }

        /// <inheritdoc />
        public Task<HashEntry[]> HashGetAllAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashGetAllAsync(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> HashGetAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashGetAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> HashGetAsync(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashGetAsync(key, hashFields, flags));
        }

        /// <inheritdoc />
        public Lease<byte> HashGetLease(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashGetLease(key, hashField, flags));
        }

        /// <inheritdoc />
        public Task<Lease<byte>> HashGetLeaseAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashGetLeaseAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public long HashIncrement(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashIncrement(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public double HashIncrement(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashIncrement(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public Task<long> HashIncrementAsync(RedisKey key, RedisValue hashField, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashIncrementAsync(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public Task<double> HashIncrementAsync(RedisKey key, RedisValue hashField, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashIncrementAsync(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public RedisValue[] HashKeys(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashKeys(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> HashKeysAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashKeysAsync(key, flags));
        }

        /// <inheritdoc />
        public long HashLength(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashLength(key, flags));
        }

        /// <inheritdoc />
        public Task<long> HashLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashLengthAsync(key, flags));
        }

        public IAsyncEnumerable<HashEntry> HashScanAsync(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250, long cursor = 0,
            int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashScanAsync(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
        {
            return ExecuteAction(() => _database.Value.HashScan(key, pattern, pageSize, flags));
        }

        /// <inheritdoc />
        public IEnumerable<HashEntry> HashScan(RedisKey key, RedisValue pattern = default, int pageSize = 10, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashScan(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _database.Value.HashSet(key, hashFields, flags));
        }

        /// <inheritdoc />
        public bool HashSet(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashSet(key, hashField, value, when, flags));
        }

        public long HashStringLength(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashStringLength(key, hashField, flags));
        }

        /// <inheritdoc />
        public Task HashSetAsync(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashSetAsync(key, hashFields, flags));
        }

        /// <inheritdoc />
        public Task<bool> HashSetAsync(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashSetAsync(key, hashField, value, when, flags));
        }

        public Task<long> HashStringLengthAsync(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashStringLengthAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public RedisValue[] HashValues(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HashValues(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> HashValuesAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HashValuesAsync(key, flags));
        }

        /// <inheritdoc />
        public bool HyperLogLogAdd(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HyperLogLogAdd(key, value, flags));
        }

        /// <inheritdoc />
        public bool HyperLogLogAdd(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HyperLogLogAdd(key, values, flags));
        }

        /// <inheritdoc />
        public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HyperLogLogAddAsync(key, value, flags));
        }

        /// <inheritdoc />
        public Task<bool> HyperLogLogAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HyperLogLogAddAsync(key, values, flags));
        }

        /// <inheritdoc />
        public long HyperLogLogLength(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HyperLogLogLength(key, flags));
        }

        /// <inheritdoc />
        public long HyperLogLogLength(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.HyperLogLogLength(keys, flags));
        }

        /// <inheritdoc />
        public Task<long> HyperLogLogLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HyperLogLogLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public Task<long> HyperLogLogLengthAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HyperLogLogLengthAsync(keys, flags));
        }

        /// <inheritdoc />
        public void HyperLogLogMerge(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _database.Value.HyperLogLogMerge(destination, first, second, flags));
        }

        /// <inheritdoc />
        public void HyperLogLogMerge(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _database.Value.HyperLogLogMerge(destination, sourceKeys, flags));
        }

        /// <inheritdoc />
        public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HyperLogLogMergeAsync(destination, first, second, flags));
        }

        /// <inheritdoc />
        public Task HyperLogLogMergeAsync(RedisKey destination, RedisKey[] sourceKeys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.HyperLogLogMergeAsync(destination, sourceKeys, flags));
        }

        /// <inheritdoc />
        public EndPoint IdentifyEndpoint(RedisKey key = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.IdentifyEndpoint(key, flags));
        }

        /// <inheritdoc />
        public Task<EndPoint> IdentifyEndpointAsync(RedisKey key = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.IdentifyEndpointAsync(key, flags));
        }

        /// <inheritdoc />
        public bool IsConnected(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.IsConnected(key, flags));
        }

        /// <inheritdoc />
        public bool KeyDelete(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyDelete(key, flags));
        }

        /// <inheritdoc />
        public long KeyDelete(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyDelete(keys, flags));
        }

        /// <inheritdoc />
        public Task<bool> KeyDeleteAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyDeleteAsync(key, flags));
        }

        /// <inheritdoc />
        public Task<long> KeyDeleteAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyDeleteAsync(keys, flags));
        }

        /// <inheritdoc />
        public byte[] KeyDump(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyDump(key, flags));
        }

        /// <inheritdoc />
        public Task<byte[]> KeyDumpAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyDumpAsync(key, flags));
        }

        /// <inheritdoc />
        public bool KeyExists(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyExists(key, flags));
        }

        /// <inheritdoc />
        public long KeyExists(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyExists(keys, flags));
        }

        /// <inheritdoc />
        public Task<bool> KeyExistsAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyExistsAsync(key, flags));
        }

        /// <inheritdoc />
        public Task<long> KeyExistsAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyExistsAsync(keys, flags));
        }

        /// <inheritdoc />
        public bool KeyExpire(RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyExpire(key, expiry, flags));
        }

        /// <inheritdoc />
        public bool KeyExpire(RedisKey key, DateTime? expiry, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyExpire(key, expiry, flags));
        }

        /// <inheritdoc />
        public Task<bool> KeyExpireAsync(RedisKey key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyExpireAsync(key, expiry, flags));
        }

        /// <inheritdoc />
        public Task<bool> KeyExpireAsync(RedisKey key, DateTime? expiry, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyExpireAsync(key, expiry, flags));
        }

        /// <inheritdoc />
        public TimeSpan? KeyIdleTime(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyIdleTime(key, flags));
        }

        /// <inheritdoc />
        public Task<TimeSpan?> KeyIdleTimeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyIdleTimeAsync(key, flags));
        }

        /// <inheritdoc />
        public void KeyMigrate(RedisKey key, EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0, MigrateOptions migrateOptions = MigrateOptions.None, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _database.Value.KeyMigrate(key, toServer, toDatabase, timeoutMilliseconds, migrateOptions, flags));
        }

        /// <inheritdoc />
        public Task KeyMigrateAsync(RedisKey key, EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0, MigrateOptions migrateOptions = MigrateOptions.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyMigrateAsync(key, toServer, toDatabase, timeoutMilliseconds, migrateOptions, flags));
        }

        /// <inheritdoc />
        public bool KeyMove(RedisKey key, int database, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyMove(key, database, flags));
        }

        /// <inheritdoc />
        public Task<bool> KeyMoveAsync(RedisKey key, int database, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyMoveAsync(key, database, flags));
        }

        /// <inheritdoc />
        public bool KeyPersist(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyPersist(key, flags));
        }

        /// <inheritdoc />
        public Task<bool> KeyPersistAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyPersistAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisKey KeyRandom(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyRandom(flags));
        }

        /// <inheritdoc />
        public Task<RedisKey> KeyRandomAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyRandomAsync(flags));
        }

        /// <inheritdoc />
        public bool KeyRename(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyRename(key, newKey, when, flags));
        }

        /// <inheritdoc />
        public Task<bool> KeyRenameAsync(RedisKey key, RedisKey newKey, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyRenameAsync(key, newKey, when, flags));
        }

        /// <inheritdoc />
        public void KeyRestore(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _database.Value.KeyRestore(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public Task KeyRestoreAsync(RedisKey key, byte[] value, TimeSpan? expiry = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyRestoreAsync(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public TimeSpan? KeyTimeToLive(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyTimeToLive(key, flags));
        }

        /// <inheritdoc />
        public Task<TimeSpan?> KeyTimeToLiveAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyTimeToLiveAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisType KeyType(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyType(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisType> KeyTypeAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyTypeAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisValue ListGetByIndex(RedisKey key, long index, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListGetByIndex(key, index, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> ListGetByIndexAsync(RedisKey key, long index, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListGetByIndexAsync(key, index, flags));
        }

        /// <inheritdoc />
        public long ListInsertAfter(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListInsertAfter(key, pivot, value, flags));
        }

        /// <inheritdoc />
        public Task<long> ListInsertAfterAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListInsertAfterAsync(key, pivot, value, flags));
        }

        /// <inheritdoc />
        public long ListInsertBefore(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListInsertBefore(key, pivot, value, flags));
        }

        /// <inheritdoc />
        public Task<long> ListInsertBeforeAsync(RedisKey key, RedisValue pivot, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListInsertBeforeAsync(key, pivot, value, flags));
        }

        /// <inheritdoc />
        public RedisValue ListLeftPop(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListLeftPop(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> ListLeftPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListLeftPopAsync(key, flags));
        }

        /// <inheritdoc />
        public long ListLeftPush(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListLeftPush(key, value, when, flags));
        }

        public long ListLeftPush(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListLeftPush(key, values, when, flags));
        }

        /// <inheritdoc />
        public long ListLeftPush(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListLeftPush(key, values, flags));
        }

        /// <inheritdoc />
        public Task<long> ListLeftPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListLeftPushAsync(key, value, when, flags));
        }

        public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListLeftPushAsync(key, values, when, flags));
        }

        /// <inheritdoc />
        public Task<long> ListLeftPushAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListLeftPushAsync(key, values, flags));
        }

        /// <inheritdoc />
        public long ListLength(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListLength(key, flags));
        }

        /// <inheritdoc />
        public Task<long> ListLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisValue[] ListRange(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListRange(key, start, stop, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> ListRangeAsync(RedisKey key, long start = 0, long stop = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListRangeAsync(key, start, stop, flags));
        }

        /// <inheritdoc />
        public long ListRemove(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListRemove(key, value, count, flags));
        }

        /// <inheritdoc />
        public Task<long> ListRemoveAsync(RedisKey key, RedisValue value, long count = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListRemoveAsync(key, value, count, flags));
        }

        /// <inheritdoc />
        public RedisValue ListRightPop(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListRightPop(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> ListRightPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListRightPopAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisValue ListRightPopLeftPush(RedisKey source, RedisKey destination, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListRightPopLeftPush(source, destination, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> ListRightPopLeftPushAsync(RedisKey source, RedisKey destination, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListRightPopLeftPushAsync(source, destination, flags));
        }

        /// <inheritdoc />
        public long ListRightPush(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListRightPush(key, value, when, flags));
        }

        public long ListRightPush(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListRightPush(key, values, when, flags));
        }

        /// <inheritdoc />
        public long ListRightPush(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListRightPush(key, values, flags));
        }

        /// <inheritdoc />
        public Task<long> ListRightPushAsync(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListRightPushAsync(key, value, when, flags));
        }

        public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListRightPushAsync(key, values, when, flags));
        }

        /// <inheritdoc />
        public Task<long> ListRightPushAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ListRightPushAsync(key, values, flags));
        }

        /// <inheritdoc />
        public void ListSetByIndex(RedisKey key, long index, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _database.Value.ListSetByIndex(key, index, value, flags));
        }

        /// <inheritdoc />
        public Task ListSetByIndexAsync(RedisKey key, long index, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListSetByIndexAsync(key, index, value, flags));
        }

        /// <inheritdoc />
        public void ListTrim(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
        {
            ExecuteAction(() => _database.Value.ListTrim(key, start, stop, flags));
        }

        /// <inheritdoc />
        public Task ListTrimAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ListTrimAsync(key, start, stop, flags));
        }

        /// <inheritdoc />
        public bool LockExtend(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.LockExtend(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public Task<bool> LockExtendAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.LockExtendAsync(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public RedisValue LockQuery(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.LockQuery(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> LockQueryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.LockQueryAsync(key, flags));
        }

        /// <inheritdoc />
        public bool LockRelease(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.LockRelease(key, value, flags));
        }

        /// <inheritdoc />
        public Task<bool> LockReleaseAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.LockReleaseAsync(key, value, flags));
        }

        /// <inheritdoc />
        public bool LockTake(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.LockTake(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public Task<bool> LockTakeAsync(RedisKey key, RedisValue value, TimeSpan expiry, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.LockTakeAsync(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public TimeSpan Ping(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.Ping(flags));
        }

        /// <inheritdoc />
        public Task<TimeSpan> PingAsync(CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.PingAsync(flags));
        }

        /// <inheritdoc />
        public long Publish(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.Publish(channel, message, flags));
        }

        /// <inheritdoc />
        public Task<long> PublishAsync(RedisChannel channel, RedisValue message, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.PublishAsync(channel, message, flags));
        }

        /// <inheritdoc />
        public RedisResult ScriptEvaluate(string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ScriptEvaluate(script, keys, values, flags));
        }

        /// <inheritdoc />
        public RedisResult ScriptEvaluate(byte[] hash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ScriptEvaluate(hash, keys, values, flags));
        }

        /// <inheritdoc />
        public RedisResult ScriptEvaluate(LuaScript script, object parameters = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ScriptEvaluate(script, parameters, flags));
        }

        /// <inheritdoc />
        public RedisResult ScriptEvaluate(LoadedLuaScript script, object parameters = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.ScriptEvaluate(script, parameters, flags));
        }

        /// <inheritdoc />
        public Task<RedisResult> ScriptEvaluateAsync(string script, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ScriptEvaluateAsync(script, keys, values, flags));
        }

        /// <inheritdoc />
        public Task<RedisResult> ScriptEvaluateAsync(byte[] hash, RedisKey[] keys = null, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ScriptEvaluateAsync(hash, keys, values, flags));
        }

        /// <inheritdoc />
        public Task<RedisResult> ScriptEvaluateAsync(LuaScript script, object parameters = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ScriptEvaluateAsync(script, parameters, flags));
        }

        /// <inheritdoc />
        public Task<RedisResult> ScriptEvaluateAsync(LoadedLuaScript script, object parameters = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.ScriptEvaluateAsync(script, parameters, flags));
        }

        /// <inheritdoc />
        public bool SetAdd(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetAdd(key, value, flags));
        }

        /// <inheritdoc />
        public long SetAdd(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetAdd(key, values, flags));
        }

        /// <inheritdoc />
        public Task<bool> SetAddAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetAddAsync(key, value, flags));
        }

        /// <inheritdoc />
        public Task<long> SetAddAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetAddAsync(key, values, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SetCombine(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetCombine(operation, first, second, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SetCombine(SetOperation operation, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetCombine(operation, keys, flags));
        }

        /// <inheritdoc />
        public long SetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetCombineAndStore(operation, destination, first, second, flags));
        }

        /// <inheritdoc />
        public long SetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetCombineAndStore(operation, destination, keys, flags));
        }

        /// <inheritdoc />
        public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetCombineAndStoreAsync(operation, destination, first, second, flags));
        }

        /// <inheritdoc />
        public Task<long> SetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetCombineAndStoreAsync(operation, destination, keys, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey first, RedisKey second, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetCombineAsync(operation, first, second, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SetCombineAsync(SetOperation operation, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetCombineAsync(operation, keys, flags));
        }

        /// <inheritdoc />
        public bool SetContains(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetContains(key, value, flags));
        }

        /// <inheritdoc />
        public Task<bool> SetContainsAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetContainsAsync(key, value, flags));
        }

        /// <inheritdoc />
        public long SetLength(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetLength(key, flags));
        }

        /// <inheritdoc />
        public Task<long> SetLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SetMembers(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetMembers(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SetMembersAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetMembersAsync(key, flags));
        }

        /// <inheritdoc />
        public bool SetMove(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetMove(source, destination, value, flags));
        }

        /// <inheritdoc />
        public Task<bool> SetMoveAsync(RedisKey source, RedisKey destination, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetMoveAsync(source, destination, value, flags));
        }

        /// <inheritdoc />
        public RedisValue SetPop(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetPop(key, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SetPop(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetPop(key, count, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> SetPopAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetPopAsync(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SetPopAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetPopAsync(key, count, flags));
        }

        /// <inheritdoc />
        public RedisValue SetRandomMember(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetRandomMember(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> SetRandomMemberAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetRandomMemberAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SetRandomMembers(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetRandomMembers(key, count, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SetRandomMembersAsync(RedisKey key, long count, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetRandomMembersAsync(key, count, flags));
        }

        /// <inheritdoc />
        public bool SetRemove(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetRemove(key, value, flags));
        }

        /// <inheritdoc />
        public long SetRemove(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetRemove(key, values, flags));
        }

        /// <inheritdoc />
        public Task<bool> SetRemoveAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetRemoveAsync(key, value, flags));
        }

        /// <inheritdoc />
        public Task<long> SetRemoveAsync(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SetRemoveAsync(key, values, flags));
        }

        /// <inheritdoc />
        public IEnumerable<RedisValue> SetScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
        {
            return ExecuteAction(() => _database.Value.SetScan(key, pattern, pageSize, flags));
        }

        /// <inheritdoc />
        public IEnumerable<RedisValue> SetScan(RedisKey key, RedisValue pattern = default, int pageSize = 10, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetScan(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public RedisValue[] Sort(RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric, RedisValue by = default, RedisValue[] get = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.Sort(key, skip, take, order, sortType, by, get, flags));
        }

        /// <inheritdoc />
        public long SortAndStore(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric, RedisValue by = default, RedisValue[] get = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortAndStore(destination, key, skip, take, order, sortType, by, get, flags));
        }

        /// <inheritdoc />
        public Task<long> SortAndStoreAsync(RedisKey destination, RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric, RedisValue by = default, RedisValue[] get = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortAndStoreAsync(destination, key, skip, take, order, sortType, by, get, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SortAsync(RedisKey key, long skip = 0, long take = -1, Order order = Order.Ascending, SortType sortType = SortType.Numeric, RedisValue by = default, RedisValue[] get = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortAsync(key, skip, take, order, sortType, by, get, flags));
        }

        /// <inheritdoc />
        public bool SortedSetAdd(RedisKey key, RedisValue member, double score, CommandFlags flags)
        {
            return ExecuteAction(() => _database.Value.SortedSetAdd(key, member, score, flags));
        }

        /// <inheritdoc />
        public bool SortedSetAdd(RedisKey key, RedisValue member, double score, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetAdd(key, member, score, when, flags));
        }

        /// <inheritdoc />
        public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, CommandFlags flags)
        {
            return ExecuteAction(() => _database.Value.SortedSetAdd(key, values, flags));
        }

        /// <inheritdoc />
        public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetAdd(key, values, when, flags));
        }

        /// <inheritdoc />
        public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, CommandFlags flags)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetAddAsync(key, member, score, flags));
        }

        /// <inheritdoc />
        public Task<bool> SortedSetAddAsync(RedisKey key, RedisValue member, double score, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetAddAsync(key, member, score, when, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, CommandFlags flags)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetAddAsync(key, values, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetAddAsync(RedisKey key, SortedSetEntry[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetAddAsync(key, values, when, flags));
        }

        /// <inheritdoc />
        public long SortedSetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetCombineAndStore(operation, destination, first, second, aggregate, flags));
        }

        /// <inheritdoc />
        public long SortedSetCombineAndStore(SetOperation operation, RedisKey destination, RedisKey[] keys, double[] weights = null, Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetCombineAndStore(operation, destination, keys, weights, aggregate, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey first, RedisKey second, Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetCombineAndStoreAsync(operation, destination, first, second, aggregate, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetCombineAndStoreAsync(SetOperation operation, RedisKey destination, RedisKey[] keys, double[] weights = null, Aggregate aggregate = Aggregate.Sum, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetCombineAndStoreAsync(operation, destination, keys, weights, aggregate, flags));
        }

        /// <inheritdoc />
        public double SortedSetDecrement(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetDecrement(key, member, value, flags));
        }

        /// <inheritdoc />
        public Task<double> SortedSetDecrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetDecrementAsync(key, member, value, flags));
        }

        /// <inheritdoc />
        public double SortedSetIncrement(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetIncrement(key, member, value, flags));
        }

        /// <inheritdoc />
        public Task<double> SortedSetIncrementAsync(RedisKey key, RedisValue member, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetIncrementAsync(key, member, value, flags));
        }

        /// <inheritdoc />
        public long SortedSetLength(RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetLength(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetLengthAsync(RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetLengthAsync(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public long SortedSetLengthByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetLengthByValue(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetLengthByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetLengthByValueAsync(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SortedSetRangeByRank(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRangeByRank(key, start, stop, order, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SortedSetRangeByRankAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRangeByRankAsync(key, start, stop, order, flags));
        }

        /// <inheritdoc />
        public SortedSetEntry[] SortedSetRangeByRankWithScores(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRangeByRankWithScores(key, start, stop, order, flags));
        }

        /// <inheritdoc />
        public Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(RedisKey key, long start = 0, long stop = -1, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SortedSetRangeByScore(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRangeByScore(key, start, stop, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SortedSetRangeByScoreAsync(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRangeByScoreAsync(key, start, stop, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public SortedSetEntry[] SortedSetRangeByScoreWithScores(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRangeByScoreWithScores(key, start, stop, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public Task<SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude, long skip, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRangeByValue(key, min, max, exclude, skip, take, flags));
        }

        /// <inheritdoc />
        public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min = default, RedisValue max = default, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRangeByValue(key, min, max, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude, long skip, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRangeByValueAsync(key, min, max, exclude, skip, take, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> SortedSetRangeByValueAsync(RedisKey key, RedisValue min = default, RedisValue max = default, Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRangeByValueAsync(key, min, max, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public long? SortedSetRank(RedisKey key, RedisValue member, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRank(key, member, order, flags));
        }

        /// <inheritdoc />
        public Task<long?> SortedSetRankAsync(RedisKey key, RedisValue member, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRankAsync(key, member, order, flags));
        }

        /// <inheritdoc />
        public bool SortedSetRemove(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRemove(key, member, flags));
        }

        /// <inheritdoc />
        public long SortedSetRemove(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRemove(key, members, flags));
        }

        /// <inheritdoc />
        public Task<bool> SortedSetRemoveAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRemoveAsync(key, member, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetRemoveAsync(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRemoveAsync(key, members, flags));
        }

        /// <inheritdoc />
        public long SortedSetRemoveRangeByRank(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRemoveRangeByRank(key, start, stop, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetRemoveRangeByRankAsync(RedisKey key, long start, long stop, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRemoveRangeByRankAsync(key, start, stop, flags));
        }

        /// <inheritdoc />
        public long SortedSetRemoveRangeByScore(RedisKey key, double start, double stop, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRemoveRangeByScore(key, start, stop, exclude, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetRemoveRangeByScoreAsync(RedisKey key, double start, double stop, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude, flags));
        }

        /// <inheritdoc />
        public long SortedSetRemoveRangeByValue(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetRemoveRangeByValue(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public Task<long> SortedSetRemoveRangeByValueAsync(RedisKey key, RedisValue min, RedisValue max, Exclude exclude = Exclude.None, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetRemoveRangeByValueAsync(key, min, max, exclude, flags));
        }

        public IAsyncEnumerable<RedisValue> SetScanAsync(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250, long cursor = 0,
            int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SetScanAsync(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        public IAsyncEnumerable<SortedSetEntry> SortedSetScanAsync(RedisKey key, RedisValue pattern = new RedisValue(), int pageSize = 250,
            long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetScanAsync(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public IEnumerable<SortedSetEntry> SortedSetScan(RedisKey key, RedisValue pattern, int pageSize, CommandFlags flags)
        {
            return ExecuteAction(() => _database.Value.SortedSetScan(key, pattern, pageSize, flags));
        }

        /// <inheritdoc />
        public IEnumerable<SortedSetEntry> SortedSetScan(RedisKey key, RedisValue pattern = default, int pageSize = 10, long cursor = 0, int pageOffset = 0, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetScan(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public double? SortedSetScore(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetScore(key, member, flags));
        }

        public SortedSetEntry? SortedSetPop(RedisKey key, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetPop(key, order, flags));
        }

        public SortedSetEntry[] SortedSetPop(RedisKey key, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.SortedSetPop(key, count, order, flags));
        }

        /// <inheritdoc />
        public Task<double?> SortedSetScoreAsync(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetScoreAsync(key, member, flags));
        }

        public Task<SortedSetEntry?> SortedSetPopAsync(RedisKey key, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetPopAsync(key, order, flags));
        }

        public Task<SortedSetEntry[]> SortedSetPopAsync(RedisKey key, long count, Order order = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.SortedSetPopAsync(key, count, order, flags));
        }

        /// <inheritdoc />
        public long StreamAcknowledge(RedisKey key, RedisValue groupName, RedisValue messageId, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamAcknowledge(key, groupName, messageId, flags));
        }

        /// <inheritdoc />
        public long StreamAcknowledge(RedisKey key, RedisValue groupName, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamAcknowledge(key, groupName, messageIds, flags));
        }

        /// <inheritdoc />
        public Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue groupName, RedisValue messageId, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamAcknowledgeAsync(key, groupName, messageId, flags));
        }

        /// <inheritdoc />
        public Task<long> StreamAcknowledgeAsync(RedisKey key, RedisValue groupName, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamAcknowledgeAsync(key, groupName, messageIds, flags));
        }

        /// <inheritdoc />
        public RedisValue StreamAdd(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamAdd(key, streamField, streamValue, messageId, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public RedisValue StreamAdd(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamAdd(key, streamPairs, messageId, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> StreamAddAsync(RedisKey key, RedisValue streamField, RedisValue streamValue, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamAddAsync(key, streamField, streamValue, messageId, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> StreamAddAsync(RedisKey key, NameValueEntry[] streamPairs, RedisValue? messageId = null, int? maxLength = null, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamAddAsync(key, streamPairs, messageId, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public StreamEntry[] StreamClaim(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamClaim(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags));
        }

        /// <inheritdoc />
        public Task<StreamEntry[]> StreamClaimAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamClaimAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags));
        }

        /// <inheritdoc />
        public RedisValue[] StreamClaimIdsOnly(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamClaimIdsOnly(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> StreamClaimIdsOnlyAsync(RedisKey key, RedisValue consumerGroup, RedisValue claimingConsumer, long minIdleTimeInMs, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamClaimIdsOnlyAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags));
        }

        /// <inheritdoc />
        public bool StreamConsumerGroupSetPosition(RedisKey key, RedisValue groupName, RedisValue position, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamConsumerGroupSetPosition(key, groupName, position, flags));
        }

        /// <inheritdoc />
        public Task<bool> StreamConsumerGroupSetPositionAsync(RedisKey key, RedisValue groupName, RedisValue position, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamConsumerGroupSetPositionAsync(key, groupName, position, flags));
        }

        /// <inheritdoc />
        public StreamConsumerInfo[] StreamConsumerInfo(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamConsumerInfo(key, groupName, flags));
        }

        /// <inheritdoc />
        public Task<StreamConsumerInfo[]> StreamConsumerInfoAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamConsumerInfoAsync(key, groupName, flags));
        }

        /// <inheritdoc />
        public bool StreamCreateConsumerGroup(RedisKey key, RedisValue groupName, RedisValue? position = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamCreateConsumerGroup(key, groupName, position, flags));
        }

        public bool StreamCreateConsumerGroup(RedisKey key, RedisValue groupName, RedisValue? position = null,
            bool createStream = true, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamCreateConsumerGroup(key, groupName, position, createStream, flags));
        }

        /// <inheritdoc />
        public Task<bool> StreamCreateConsumerGroupAsync(RedisKey key, RedisValue groupName, RedisValue? position = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamCreateConsumerGroupAsync(key, groupName, position, flags));
        }

        public Task<bool> StreamCreateConsumerGroupAsync(RedisKey key, RedisValue groupName, RedisValue? position = null,
            bool createStream = true, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamCreateConsumerGroupAsync(key, groupName, position, createStream, flags));
        }

        /// <inheritdoc />
        public long StreamDelete(RedisKey key, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamDelete(key, messageIds, flags));
        }

        /// <inheritdoc />
        public Task<long> StreamDeleteAsync(RedisKey key, RedisValue[] messageIds, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamDeleteAsync(key, messageIds, flags));
        }

        /// <inheritdoc />
        public long StreamDeleteConsumer(RedisKey key, RedisValue groupName, RedisValue consumerName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamDeleteConsumer(key, groupName, consumerName, flags));
        }

        /// <inheritdoc />
        public Task<long> StreamDeleteConsumerAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamDeleteConsumerAsync(key, groupName, consumerName, flags));
        }

        /// <inheritdoc />
        public bool StreamDeleteConsumerGroup(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamDeleteConsumerGroup(key, groupName, flags));
        }

        /// <inheritdoc />
        public Task<bool> StreamDeleteConsumerGroupAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamDeleteConsumerGroupAsync(key, groupName, flags));
        }

        /// <inheritdoc />
        public StreamGroupInfo[] StreamGroupInfo(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamGroupInfo(key, flags));
        }

        /// <inheritdoc />
        public Task<StreamGroupInfo[]> StreamGroupInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamGroupInfoAsync(key, flags));
        }

        /// <inheritdoc />
        public StreamInfo StreamInfo(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamInfo(key, flags));
        }

        /// <inheritdoc />
        public Task<StreamInfo> StreamInfoAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamInfoAsync(key, flags));
        }

        /// <inheritdoc />
        public long StreamLength(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamLength(key, flags));
        }

        /// <inheritdoc />
        public Task<long> StreamLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public StreamPendingInfo StreamPending(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamPending(key, groupName, flags));
        }

        /// <inheritdoc />
        public Task<StreamPendingInfo> StreamPendingAsync(RedisKey key, RedisValue groupName, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamPendingAsync(key, groupName, flags));
        }

        /// <inheritdoc />
        public StreamPendingMessageInfo[] StreamPendingMessages(RedisKey key, RedisValue groupName, int count, RedisValue consumerName, RedisValue? minId = null, RedisValue? maxId = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamPendingMessages(key, groupName, count, consumerName, minId, maxId, flags));
        }

        /// <inheritdoc />
        public Task<StreamPendingMessageInfo[]> StreamPendingMessagesAsync(RedisKey key, RedisValue groupName, int count, RedisValue consumerName, RedisValue? minId = null, RedisValue? maxId = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamPendingMessagesAsync(key, groupName, count, consumerName, minId, maxId, flags));
        }

        /// <inheritdoc />
        public StreamEntry[] StreamRange(RedisKey key, RedisValue? minId = null, RedisValue? maxId = null, int? count = null, Order messageOrder = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamRange(key, minId, maxId, count, messageOrder, flags));
        }

        /// <inheritdoc />
        public Task<StreamEntry[]> StreamRangeAsync(RedisKey key, RedisValue? minId = null, RedisValue? maxId = null, int? count = null, Order messageOrder = Order.Ascending, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamRangeAsync(key, minId, maxId, count, messageOrder, flags));
        }

        /// <inheritdoc />
        public StreamEntry[] StreamRead(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamRead(key, position, count, flags));
        }

        /// <inheritdoc />
        public RedisStream[] StreamRead(StreamPosition[] streamPositions, int? countPerStream = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamRead(streamPositions, countPerStream, flags));
        }

        /// <inheritdoc />
        public Task<StreamEntry[]> StreamReadAsync(RedisKey key, RedisValue position, int? count = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamReadAsync(key, position, count, flags));
        }

        /// <inheritdoc />
        public Task<RedisStream[]> StreamReadAsync(StreamPosition[] streamPositions, int? countPerStream = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamReadAsync(streamPositions, countPerStream, flags));
        }

        /// <inheritdoc />
        public StreamEntry[] StreamReadGroup(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null, int? count = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamReadGroup(key, groupName, consumerName, position, count, flags));
        }

        public StreamEntry[] StreamReadGroup(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null,
            int? count = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamReadGroup(key, groupName, consumerName, position, count, noAck, flags));
        }

        /// <inheritdoc />
        public RedisStream[] StreamReadGroup(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName, int? countPerStream = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamReadGroup(streamPositions, groupName, consumerName, countPerStream, flags));
        }

        public RedisStream[] StreamReadGroup(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName,
            int? countPerStream = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamReadGroup(streamPositions, groupName, consumerName, countPerStream, noAck, flags));
        }

        /// <inheritdoc />
        public Task<StreamEntry[]> StreamReadGroupAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null, int? count = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamReadGroupAsync(key, groupName, consumerName, position, count, flags));
        }

        public Task<StreamEntry[]> StreamReadGroupAsync(RedisKey key, RedisValue groupName, RedisValue consumerName, RedisValue? position = null,
            int? count = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamReadGroupAsync(key, groupName, consumerName, position, count, noAck, flags));
        }

        /// <inheritdoc />
        public Task<RedisStream[]> StreamReadGroupAsync(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName, int? countPerStream = null, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamReadGroupAsync(streamPositions, groupName, consumerName, countPerStream, flags));
        }

        public Task<RedisStream[]> StreamReadGroupAsync(StreamPosition[] streamPositions, RedisValue groupName, RedisValue consumerName,
            int? countPerStream = null, bool noAck = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamReadGroupAsync(streamPositions, groupName, consumerName, countPerStream, noAck, flags));
        }

        /// <inheritdoc />
        public long StreamTrim(RedisKey key, int maxLength, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StreamTrim(key, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public Task<long> StreamTrimAsync(RedisKey key, int maxLength, bool useApproximateMaxLength = false, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StreamTrimAsync(key, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public long StringAppend(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringAppend(key, value, flags));
        }

        /// <inheritdoc />
        public Task<long> StringAppendAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringAppendAsync(key, value, flags));
        }

        /// <inheritdoc />
        public long StringBitCount(RedisKey key, long start = 0, long end = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringBitCount(key, start, end, flags));
        }

        /// <inheritdoc />
        public Task<long> StringBitCountAsync(RedisKey key, long start = 0, long end = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringBitCountAsync(key, start, end, flags));
        }

        /// <inheritdoc />
        public long StringBitOperation(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringBitOperation(operation, destination, first, second, flags));
        }

        /// <inheritdoc />
        public long StringBitOperation(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringBitOperation(operation, destination, keys, flags));
        }

        /// <inheritdoc />
        public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey first, RedisKey second = default, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringBitOperationAsync(operation, destination, first, second, flags));
        }

        /// <inheritdoc />
        public Task<long> StringBitOperationAsync(Bitwise operation, RedisKey destination, RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringBitOperationAsync(operation, destination, keys, flags));
        }

        /// <inheritdoc />
        public long StringBitPosition(RedisKey key, bool bit, long start = 0, long end = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringBitPosition(key, bit, start, end, flags));
        }

        /// <inheritdoc />
        public Task<long> StringBitPositionAsync(RedisKey key, bool bit, long start = 0, long end = -1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringBitPositionAsync(key, bit, start, end, flags));
        }

        /// <inheritdoc />
        public long StringDecrement(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringDecrement(key, value, flags));
        }

        /// <inheritdoc />
        public double StringDecrement(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringDecrement(key, value, flags));
        }

        /// <inheritdoc />
        public Task<long> StringDecrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringDecrementAsync(key, value, flags));
        }

        /// <inheritdoc />
        public Task<double> StringDecrementAsync(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringDecrementAsync(key, value, flags));
        }

        /// <inheritdoc />
        public RedisValue StringGet(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringGet(key, flags));
        }

        /// <inheritdoc />
        public RedisValue[] StringGet(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringGet(keys, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> StringGetAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringGetAsync(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue[]> StringGetAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringGetAsync(keys, flags));
        }

        /// <inheritdoc />
        public bool StringGetBit(RedisKey key, long offset, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringGetBit(key, offset, flags));
        }

        /// <inheritdoc />
        public Task<bool> StringGetBitAsync(RedisKey key, long offset, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringGetBitAsync(key, offset, flags));
        }

        /// <inheritdoc />
        public Lease<byte> StringGetLease(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringGetLease(key, flags));
        }

        /// <inheritdoc />
        public Task<Lease<byte>> StringGetLeaseAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringGetLeaseAsync(key, flags));
        }

        /// <inheritdoc />
        public RedisValue StringGetRange(RedisKey key, long start, long end, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringGetRange(key, start, end, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> StringGetRangeAsync(RedisKey key, long start, long end, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringGetRangeAsync(key, start, end, flags));
        }

        /// <inheritdoc />
        public RedisValue StringGetSet(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringGetSet(key, value, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> StringGetSetAsync(RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringGetSetAsync(key, value, flags));
        }

        /// <inheritdoc />
        public RedisValueWithExpiry StringGetWithExpiry(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringGetWithExpiry(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValueWithExpiry> StringGetWithExpiryAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringGetWithExpiryAsync(key, flags));
        }

        /// <inheritdoc />
        public long StringIncrement(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringIncrement(key, value, flags));
        }

        /// <inheritdoc />
        public double StringIncrement(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringIncrement(key, value, flags));
        }

        /// <inheritdoc />
        public Task<long> StringIncrementAsync(RedisKey key, long value = 1, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringIncrementAsync(key, value, flags));
        }

        /// <inheritdoc />
        public Task<double> StringIncrementAsync(RedisKey key, double value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringIncrementAsync(key, value, flags));
        }

        /// <inheritdoc />
        public long StringLength(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringLength(key, flags));
        }

        /// <inheritdoc />
        public Task<long> StringLengthAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringSet(key, value, expiry, when, flags));
        }

        /// <inheritdoc />
        public bool StringSet(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringSet(values, when, flags));
        }

        /// <inheritdoc />
        public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringSetAsync(key, value, expiry, when, flags));
        }

        /// <inheritdoc />
        public Task<bool> StringSetAsync(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringSetAsync(values, when, flags));
        }

        /// <inheritdoc />
        public bool StringSetBit(RedisKey key, long offset, bool bit, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringSetBit(key, offset, bit, flags));
        }

        /// <inheritdoc />
        public Task<bool> StringSetBitAsync(RedisKey key, long offset, bool bit, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringSetBitAsync(key, offset, bit, flags));
        }

        /// <inheritdoc />
        public RedisValue StringSetRange(RedisKey key, long offset, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.StringSetRange(key, offset, value, flags));
        }

        public bool KeyTouch(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteAction(() => _database.Value.KeyTouch(key, flags));
        }

        /// <inheritdoc />
        public Task<RedisValue> StringSetRangeAsync(RedisKey key, long offset, RedisValue value, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.StringSetRangeAsync(key, offset, value, flags));
        }

        public Task<bool> KeyTouchAsync(RedisKey key, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyTouchAsync(key, flags));
        }

        public Task<long> KeyTouchAsync(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
        {
            return ExecuteActionAsync(() => _database.Value.KeyTouchAsync(keys, flags));
        }

        /// <inheritdoc />
        public bool TryWait(Task task)
        {
            return ExecuteAction(() => _database.Value.TryWait(task));
        }

        /// <inheritdoc />
        public void Wait(Task task)
        {
            ExecuteAction(() => _database.Value.Wait(task));
        }

        /// <inheritdoc />
        public T Wait<T>(Task<T> task)
        {
            return ExecuteAction(() => _database.Value.Wait(task));
        }

        /// <inheritdoc />
        public void WaitAll(params Task[] tasks)
        {
            ExecuteAction(() => _database.Value.WaitAll(tasks));
        }

        #endregion

        private void ResetDatabase()
        {
            _database = new AtomicLazy<IDatabase>(_databaseProvider);
        }

        private void CheckAndReset()
        {
            ResilientConnectionMultiplexer.CheckAndReset(
                _resilientConnectionMultiplexer.LastReconnectTicks,
                ref _lastReconnectTicks,
                _resetLock,
                ResetDatabase);
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
