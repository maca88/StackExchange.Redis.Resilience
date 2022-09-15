using System;
using System.Threading.Tasks;

#nullable enable

namespace StackExchange.Redis.Resilience
{
    internal partial class ResilientDatabase
    {
        private readonly ResilientConnectionMultiplexer _resilientConnectionMultiplexer;
        private readonly Func<StackExchange.Redis.IDatabase> _instanceProvider;
        private readonly object _resetLock = new object();
        private AtomicLazy<StackExchange.Redis.IDatabase>? _instance;
        private long _lastReconnectTicks;

        public ResilientDatabase(ResilientConnectionMultiplexer resilientConnectionMultiplexer, Func<StackExchange.Redis.IDatabase> instanceProvider)
        {
            _resilientConnectionMultiplexer = resilientConnectionMultiplexer;
            _instanceProvider = instanceProvider;
            _lastReconnectTicks = resilientConnectionMultiplexer.LastReconnectTicks;
            ResetInstance();
        }

        /// <inheritdoc />
        public IConnectionMultiplexer Multiplexer => _resilientConnectionMultiplexer;


        /// <inheritdoc />
        public StackExchange.Redis.IBatch CreateBatch(object? asyncState = default)
        {
            return ExecuteAction(() => _instance!.Value.CreateBatch(asyncState));
        }

        /// <inheritdoc />
        public StackExchange.Redis.ITransaction CreateTransaction(object? asyncState = default)
        {
            return ExecuteAction(() => _instance!.Value.CreateTransaction(asyncState));
        }

        /// <inheritdoc />
        public void KeyMigrate(StackExchange.Redis.RedisKey key, System.Net.EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0, StackExchange.Redis.MigrateOptions migrateOptions = StackExchange.Redis.MigrateOptions.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.KeyMigrate(key, toServer, toDatabase, timeoutMilliseconds, migrateOptions, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue DebugObject(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.DebugObject(key, flags));
        }

        /// <inheritdoc />
        public bool GeoAdd(StackExchange.Redis.RedisKey key, double longitude, double latitude, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoAdd(key, longitude, latitude, member, flags));
        }

        /// <inheritdoc />
        public bool GeoAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.GeoEntry value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoAdd(key, value, flags));
        }

        /// <inheritdoc />
        public long GeoAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.GeoEntry[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoAdd(key, values, flags));
        }

        /// <inheritdoc />
        public bool GeoRemove(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoRemove(key, member, flags));
        }

        /// <inheritdoc />
        public double? GeoDistance(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member1, StackExchange.Redis.RedisValue member2, StackExchange.Redis.GeoUnit unit = StackExchange.Redis.GeoUnit.Meters, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoDistance(key, member1, member2, unit, flags));
        }

        /// <inheritdoc />
        public string?[] GeoHash(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] members, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoHash(key, members, flags));
        }

        /// <inheritdoc />
        public string? GeoHash(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoHash(key, member, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.GeoPosition?[] GeoPosition(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] members, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoPosition(key, members, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.GeoPosition? GeoPosition(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoPosition(key, member, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.GeoRadiusResult[] GeoRadius(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double radius, StackExchange.Redis.GeoUnit unit = StackExchange.Redis.GeoUnit.Meters, int count = -1, StackExchange.Redis.Order? order = default, StackExchange.Redis.GeoRadiusOptions options = StackExchange.Redis.GeoRadiusOptions.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoRadius(key, member, radius, unit, count, order, options, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.GeoRadiusResult[] GeoRadius(StackExchange.Redis.RedisKey key, double longitude, double latitude, double radius, StackExchange.Redis.GeoUnit unit = StackExchange.Redis.GeoUnit.Meters, int count = -1, StackExchange.Redis.Order? order = default, StackExchange.Redis.GeoRadiusOptions options = StackExchange.Redis.GeoRadiusOptions.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoRadius(key, longitude, latitude, radius, unit, count, order, options, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.GeoRadiusResult[] GeoSearch(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.GeoSearchShape shape, int count = -1, bool demandClosest = true, StackExchange.Redis.Order? order = default, StackExchange.Redis.GeoRadiusOptions options = StackExchange.Redis.GeoRadiusOptions.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoSearch(key, member, shape, count, demandClosest, order, options, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.GeoRadiusResult[] GeoSearch(StackExchange.Redis.RedisKey key, double longitude, double latitude, StackExchange.Redis.GeoSearchShape shape, int count = -1, bool demandClosest = true, StackExchange.Redis.Order? order = default, StackExchange.Redis.GeoRadiusOptions options = StackExchange.Redis.GeoRadiusOptions.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoSearch(key, longitude, latitude, shape, count, demandClosest, order, options, flags));
        }

        /// <inheritdoc />
        public long GeoSearchAndStore(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, StackExchange.Redis.RedisValue member, StackExchange.Redis.GeoSearchShape shape, int count = -1, bool demandClosest = true, StackExchange.Redis.Order? order = default, bool storeDistances = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoSearchAndStore(sourceKey, destinationKey, member, shape, count, demandClosest, order, storeDistances, flags));
        }

        /// <inheritdoc />
        public long GeoSearchAndStore(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, double longitude, double latitude, StackExchange.Redis.GeoSearchShape shape, int count = -1, bool demandClosest = true, StackExchange.Redis.Order? order = default, bool storeDistances = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.GeoSearchAndStore(sourceKey, destinationKey, longitude, latitude, shape, count, demandClosest, order, storeDistances, flags));
        }

        /// <inheritdoc />
        public long HashDecrement(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, long value = 1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashDecrement(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public double HashDecrement(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashDecrement(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public bool HashDelete(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashDelete(key, hashField, flags));
        }

        /// <inheritdoc />
        public long HashDelete(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] hashFields, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashDelete(key, hashFields, flags));
        }

        /// <inheritdoc />
        public bool HashExists(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashExists(key, hashField, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue HashGet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashGet(key, hashField, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.Lease<byte>? HashGetLease(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashGetLease(key, hashField, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] HashGet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] hashFields, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashGet(key, hashFields, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.HashEntry[] HashGetAll(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashGetAll(key, flags));
        }

        /// <inheritdoc />
        public long HashIncrement(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, long value = 1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashIncrement(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public double HashIncrement(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashIncrement(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] HashKeys(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashKeys(key, flags));
        }

        /// <inheritdoc />
        public long HashLength(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashLength(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue HashRandomField(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashRandomField(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] HashRandomFields(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashRandomFields(key, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.HashEntry[] HashRandomFieldsWithValues(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashRandomFieldsWithValues(key, count, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IEnumerable<StackExchange.Redis.HashEntry> HashScan(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern, int pageSize, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.HashScan(key, pattern, pageSize, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IEnumerable<StackExchange.Redis.HashEntry> HashScan(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashScan(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public void HashSet(StackExchange.Redis.RedisKey key, StackExchange.Redis.HashEntry[] hashFields, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.HashSet(key, hashFields, flags));
        }

        /// <inheritdoc />
        public bool HashSet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.RedisValue value, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashSet(key, hashField, value, when, flags));
        }

        /// <inheritdoc />
        public long HashStringLength(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashStringLength(key, hashField, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] HashValues(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashValues(key, flags));
        }

        /// <inheritdoc />
        public bool HyperLogLogAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HyperLogLogAdd(key, value, flags));
        }

        /// <inheritdoc />
        public bool HyperLogLogAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HyperLogLogAdd(key, values, flags));
        }

        /// <inheritdoc />
        public long HyperLogLogLength(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HyperLogLogLength(key, flags));
        }

        /// <inheritdoc />
        public long HyperLogLogLength(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HyperLogLogLength(keys, flags));
        }

        /// <inheritdoc />
        public void HyperLogLogMerge(StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.HyperLogLogMerge(destination, first, second, flags));
        }

        /// <inheritdoc />
        public void HyperLogLogMerge(StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey[] sourceKeys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.HyperLogLogMerge(destination, sourceKeys, flags));
        }

        /// <inheritdoc />
        public System.Net.EndPoint? IdentifyEndpoint(StackExchange.Redis.RedisKey key = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.IdentifyEndpoint(key, flags));
        }

        /// <inheritdoc />
        public bool KeyCopy(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, int destinationDatabase = -1, bool replace = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyCopy(sourceKey, destinationKey, destinationDatabase, replace, flags));
        }

        /// <inheritdoc />
        public bool KeyDelete(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyDelete(key, flags));
        }

        /// <inheritdoc />
        public long KeyDelete(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyDelete(keys, flags));
        }

        /// <inheritdoc />
        public byte[]? KeyDump(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyDump(key, flags));
        }

        /// <inheritdoc />
        public string? KeyEncoding(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyEncoding(key, flags));
        }

        /// <inheritdoc />
        public bool KeyExists(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyExists(key, flags));
        }

        /// <inheritdoc />
        public long KeyExists(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyExists(keys, flags));
        }

        /// <inheritdoc />
        public bool KeyExpire(StackExchange.Redis.RedisKey key, System.TimeSpan? expiry, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.KeyExpire(key, expiry, flags));
        }

        /// <inheritdoc />
        public bool KeyExpire(StackExchange.Redis.RedisKey key, System.TimeSpan? expiry, StackExchange.Redis.ExpireWhen when = StackExchange.Redis.ExpireWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyExpire(key, expiry, when, flags));
        }

        /// <inheritdoc />
        public bool KeyExpire(StackExchange.Redis.RedisKey key, System.DateTime? expiry, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.KeyExpire(key, expiry, flags));
        }

        /// <inheritdoc />
        public bool KeyExpire(StackExchange.Redis.RedisKey key, System.DateTime? expiry, StackExchange.Redis.ExpireWhen when = StackExchange.Redis.ExpireWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyExpire(key, expiry, when, flags));
        }

        /// <inheritdoc />
        public System.DateTime? KeyExpireTime(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyExpireTime(key, flags));
        }

        /// <inheritdoc />
        public long? KeyFrequency(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyFrequency(key, flags));
        }

        /// <inheritdoc />
        public System.TimeSpan? KeyIdleTime(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyIdleTime(key, flags));
        }

        /// <inheritdoc />
        public bool KeyMove(StackExchange.Redis.RedisKey key, int database, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyMove(key, database, flags));
        }

        /// <inheritdoc />
        public bool KeyPersist(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyPersist(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisKey KeyRandom(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyRandom(flags));
        }

        /// <inheritdoc />
        public long? KeyRefCount(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyRefCount(key, flags));
        }

        /// <inheritdoc />
        public bool KeyRename(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisKey newKey, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyRename(key, newKey, when, flags));
        }

        /// <inheritdoc />
        public void KeyRestore(StackExchange.Redis.RedisKey key, byte[] value, System.TimeSpan? expiry = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.KeyRestore(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public System.TimeSpan? KeyTimeToLive(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyTimeToLive(key, flags));
        }

        /// <inheritdoc />
        public bool KeyTouch(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyTouch(key, flags));
        }

        /// <inheritdoc />
        public long KeyTouch(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyTouch(keys, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisType KeyType(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.KeyType(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue ListGetByIndex(StackExchange.Redis.RedisKey key, long index, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListGetByIndex(key, index, flags));
        }

        /// <inheritdoc />
        public long ListInsertAfter(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pivot, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListInsertAfter(key, pivot, value, flags));
        }

        /// <inheritdoc />
        public long ListInsertBefore(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pivot, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListInsertBefore(key, pivot, value, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue ListLeftPop(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListLeftPop(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] ListLeftPop(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListLeftPop(key, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.ListPopResult ListLeftPop(StackExchange.Redis.RedisKey[] keys, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListLeftPop(keys, count, flags));
        }

        /// <inheritdoc />
        public long ListPosition(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue element, long rank = 1, long maxLength = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListPosition(key, element, rank, maxLength, flags));
        }

        /// <inheritdoc />
        public long[] ListPositions(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue element, long count, long rank = 1, long maxLength = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListPositions(key, element, count, rank, maxLength, flags));
        }

        /// <inheritdoc />
        public long ListLeftPush(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListLeftPush(key, value, when, flags));
        }

        /// <inheritdoc />
        public long ListLeftPush(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListLeftPush(key, values, when, flags));
        }

        /// <inheritdoc />
        public long ListLeftPush(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.ListLeftPush(key, values, flags));
        }

        /// <inheritdoc />
        public long ListLength(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListLength(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue ListMove(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, StackExchange.Redis.ListSide sourceSide, StackExchange.Redis.ListSide destinationSide, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListMove(sourceKey, destinationKey, sourceSide, destinationSide, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] ListRange(StackExchange.Redis.RedisKey key, long start = 0, long stop = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListRange(key, start, stop, flags));
        }

        /// <inheritdoc />
        public long ListRemove(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, long count = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListRemove(key, value, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue ListRightPop(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListRightPop(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] ListRightPop(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListRightPop(key, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.ListPopResult ListRightPop(StackExchange.Redis.RedisKey[] keys, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListRightPop(keys, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue ListRightPopLeftPush(StackExchange.Redis.RedisKey source, StackExchange.Redis.RedisKey destination, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListRightPopLeftPush(source, destination, flags));
        }

        /// <inheritdoc />
        public long ListRightPush(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListRightPush(key, value, when, flags));
        }

        /// <inheritdoc />
        public long ListRightPush(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ListRightPush(key, values, when, flags));
        }

        /// <inheritdoc />
        public long ListRightPush(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.ListRightPush(key, values, flags));
        }

        /// <inheritdoc />
        public void ListSetByIndex(StackExchange.Redis.RedisKey key, long index, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.ListSetByIndex(key, index, value, flags));
        }

        /// <inheritdoc />
        public void ListTrim(StackExchange.Redis.RedisKey key, long start, long stop, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            ExecuteAction(() => _instance!.Value.ListTrim(key, start, stop, flags));
        }

        /// <inheritdoc />
        public bool LockExtend(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan expiry, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LockExtend(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue LockQuery(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LockQuery(key, flags));
        }

        /// <inheritdoc />
        public bool LockRelease(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LockRelease(key, value, flags));
        }

        /// <inheritdoc />
        public bool LockTake(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan expiry, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.LockTake(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public long Publish(StackExchange.Redis.RedisChannel channel, StackExchange.Redis.RedisValue message, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Publish(channel, message, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult Execute(string command, object[] args)
        {
            return ExecuteAction(() => _instance!.Value.Execute(command, args));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult Execute(string command, System.Collections.Generic.ICollection<object> args, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Execute(command, args, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult ScriptEvaluate(string script, StackExchange.Redis.RedisKey[]? keys = default, StackExchange.Redis.RedisValue[]? values = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ScriptEvaluate(script, keys, values, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult ScriptEvaluate(byte[] hash, StackExchange.Redis.RedisKey[]? keys = default, StackExchange.Redis.RedisValue[]? values = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ScriptEvaluate(hash, keys, values, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult ScriptEvaluate(StackExchange.Redis.LuaScript script, object? parameters = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ScriptEvaluate(script, parameters, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisResult ScriptEvaluate(StackExchange.Redis.LoadedLuaScript script, object? parameters = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.ScriptEvaluate(script, parameters, flags));
        }

        /// <inheritdoc />
        public bool SetAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetAdd(key, value, flags));
        }

        /// <inheritdoc />
        public long SetAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetAdd(key, values, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SetCombine(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetCombine(operation, first, second, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SetCombine(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetCombine(operation, keys, flags));
        }

        /// <inheritdoc />
        public long SetCombineAndStore(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetCombineAndStore(operation, destination, first, second, flags));
        }

        /// <inheritdoc />
        public long SetCombineAndStore(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetCombineAndStore(operation, destination, keys, flags));
        }

        /// <inheritdoc />
        public bool SetContains(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetContains(key, value, flags));
        }

        /// <inheritdoc />
        public bool[] SetContains(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetContains(key, values, flags));
        }

        /// <inheritdoc />
        public long SetIntersectionLength(StackExchange.Redis.RedisKey[] keys, long limit = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetIntersectionLength(keys, limit, flags));
        }

        /// <inheritdoc />
        public long SetLength(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetLength(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SetMembers(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetMembers(key, flags));
        }

        /// <inheritdoc />
        public bool SetMove(StackExchange.Redis.RedisKey source, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetMove(source, destination, value, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue SetPop(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetPop(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SetPop(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetPop(key, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue SetRandomMember(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetRandomMember(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SetRandomMembers(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetRandomMembers(key, count, flags));
        }

        /// <inheritdoc />
        public bool SetRemove(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetRemove(key, value, flags));
        }

        /// <inheritdoc />
        public long SetRemove(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetRemove(key, values, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IEnumerable<StackExchange.Redis.RedisValue> SetScan(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern, int pageSize, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.SetScan(key, pattern, pageSize, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IEnumerable<StackExchange.Redis.RedisValue> SetScan(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetScan(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] Sort(StackExchange.Redis.RedisKey key, long skip = 0, long take = -1, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.SortType sortType = StackExchange.Redis.SortType.Numeric, StackExchange.Redis.RedisValue by = default, StackExchange.Redis.RedisValue[]? get = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Sort(key, skip, take, order, sortType, by, get, flags));
        }

        /// <inheritdoc />
        public long SortAndStore(StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey key, long skip = 0, long take = -1, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.SortType sortType = StackExchange.Redis.SortType.Numeric, StackExchange.Redis.RedisValue by = default, StackExchange.Redis.RedisValue[]? get = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortAndStore(destination, key, skip, take, order, sortType, by, get, flags));
        }

        /// <inheritdoc />
        public bool SortedSetAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double score, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetAdd(key, member, score, flags));
        }

        /// <inheritdoc />
        public bool SortedSetAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double score, StackExchange.Redis.When when, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetAdd(key, member, score, when, flags));
        }

        /// <inheritdoc />
        public bool SortedSetAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double score, StackExchange.Redis.SortedSetWhen when = StackExchange.Redis.SortedSetWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetAdd(key, member, score, when, flags));
        }

        /// <inheritdoc />
        public long SortedSetAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.SortedSetEntry[] values, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetAdd(key, values, flags));
        }

        /// <inheritdoc />
        public long SortedSetAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.SortedSetEntry[] values, StackExchange.Redis.When when, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetAdd(key, values, when, flags));
        }

        /// <inheritdoc />
        public long SortedSetAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.SortedSetEntry[] values, StackExchange.Redis.SortedSetWhen when = StackExchange.Redis.SortedSetWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetAdd(key, values, when, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SortedSetCombine(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey[] keys, double[]? weights = default, StackExchange.Redis.Aggregate aggregate = StackExchange.Redis.Aggregate.Sum, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetCombine(operation, keys, weights, aggregate, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.SortedSetEntry[] SortedSetCombineWithScores(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey[] keys, double[]? weights = default, StackExchange.Redis.Aggregate aggregate = StackExchange.Redis.Aggregate.Sum, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetCombineWithScores(operation, keys, weights, aggregate, flags));
        }

        /// <inheritdoc />
        public long SortedSetCombineAndStore(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.Aggregate aggregate = StackExchange.Redis.Aggregate.Sum, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetCombineAndStore(operation, destination, first, second, aggregate, flags));
        }

        /// <inheritdoc />
        public long SortedSetCombineAndStore(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey[] keys, double[]? weights = default, StackExchange.Redis.Aggregate aggregate = StackExchange.Redis.Aggregate.Sum, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetCombineAndStore(operation, destination, keys, weights, aggregate, flags));
        }

        /// <inheritdoc />
        public double SortedSetDecrement(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetDecrement(key, member, value, flags));
        }

        /// <inheritdoc />
        public double SortedSetIncrement(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetIncrement(key, member, value, flags));
        }

        /// <inheritdoc />
        public long SortedSetIntersectionLength(StackExchange.Redis.RedisKey[] keys, long limit = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetIntersectionLength(keys, limit, flags));
        }

        /// <inheritdoc />
        public long SortedSetLength(StackExchange.Redis.RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetLength(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public long SortedSetLengthByValue(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue min, StackExchange.Redis.RedisValue max, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetLengthByValue(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue SortedSetRandomMember(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRandomMember(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SortedSetRandomMembers(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRandomMembers(key, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.SortedSetEntry[] SortedSetRandomMembersWithScores(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRandomMembersWithScores(key, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SortedSetRangeByRank(StackExchange.Redis.RedisKey key, long start = 0, long stop = -1, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRangeByRank(key, start, stop, order, flags));
        }

        /// <inheritdoc />
        public long SortedSetRangeAndStore(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, StackExchange.Redis.RedisValue start, StackExchange.Redis.RedisValue stop, StackExchange.Redis.SortedSetOrder sortedSetOrder = StackExchange.Redis.SortedSetOrder.ByRank, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, long skip = 0, long? take = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRangeAndStore(sourceKey, destinationKey, start, stop, sortedSetOrder, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.SortedSetEntry[] SortedSetRangeByRankWithScores(StackExchange.Redis.RedisKey key, long start = 0, long stop = -1, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRangeByRankWithScores(key, start, stop, order, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SortedSetRangeByScore(StackExchange.Redis.RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, long skip = 0, long take = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRangeByScore(key, start, stop, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.SortedSetEntry[] SortedSetRangeByScoreWithScores(StackExchange.Redis.RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, long skip = 0, long take = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRangeByScoreWithScores(key, start, stop, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SortedSetRangeByValue(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue min, StackExchange.Redis.RedisValue max, StackExchange.Redis.Exclude exclude, long skip, long take = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRangeByValue(key, min, max, exclude, skip, take, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] SortedSetRangeByValue(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue min = default, StackExchange.Redis.RedisValue max = default, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, long skip = 0, long take = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRangeByValue(key, min, max, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public long? SortedSetRank(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRank(key, member, order, flags));
        }

        /// <inheritdoc />
        public bool SortedSetRemove(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRemove(key, member, flags));
        }

        /// <inheritdoc />
        public long SortedSetRemove(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] members, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRemove(key, members, flags));
        }

        /// <inheritdoc />
        public long SortedSetRemoveRangeByRank(StackExchange.Redis.RedisKey key, long start, long stop, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRemoveRangeByRank(key, start, stop, flags));
        }

        /// <inheritdoc />
        public long SortedSetRemoveRangeByScore(StackExchange.Redis.RedisKey key, double start, double stop, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRemoveRangeByScore(key, start, stop, exclude, flags));
        }

        /// <inheritdoc />
        public long SortedSetRemoveRangeByValue(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue min, StackExchange.Redis.RedisValue max, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetRemoveRangeByValue(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IEnumerable<StackExchange.Redis.SortedSetEntry> SortedSetScan(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern, int pageSize, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetScan(key, pattern, pageSize, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IEnumerable<StackExchange.Redis.SortedSetEntry> SortedSetScan(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetScan(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public double? SortedSetScore(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetScore(key, member, flags));
        }

        /// <inheritdoc />
        public double?[] SortedSetScores(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] members, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetScores(key, members, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.SortedSetEntry? SortedSetPop(StackExchange.Redis.RedisKey key, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetPop(key, order, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.SortedSetEntry[] SortedSetPop(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetPop(key, count, order, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.SortedSetPopResult SortedSetPop(StackExchange.Redis.RedisKey[] keys, long count, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetPop(keys, count, order, flags));
        }

        /// <inheritdoc />
        public bool SortedSetUpdate(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double score, StackExchange.Redis.SortedSetWhen when = StackExchange.Redis.SortedSetWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetUpdate(key, member, score, when, flags));
        }

        /// <inheritdoc />
        public long SortedSetUpdate(StackExchange.Redis.RedisKey key, StackExchange.Redis.SortedSetEntry[] values, StackExchange.Redis.SortedSetWhen when = StackExchange.Redis.SortedSetWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetUpdate(key, values, when, flags));
        }

        /// <inheritdoc />
        public long StreamAcknowledge(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue messageId, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamAcknowledge(key, groupName, messageId, flags));
        }

        /// <inheritdoc />
        public long StreamAcknowledge(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue[] messageIds, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamAcknowledge(key, groupName, messageIds, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StreamAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue streamField, StackExchange.Redis.RedisValue streamValue, StackExchange.Redis.RedisValue? messageId = default, int? maxLength = default, bool useApproximateMaxLength = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamAdd(key, streamField, streamValue, messageId, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StreamAdd(StackExchange.Redis.RedisKey key, StackExchange.Redis.NameValueEntry[] streamPairs, StackExchange.Redis.RedisValue? messageId = default, int? maxLength = default, bool useApproximateMaxLength = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamAdd(key, streamPairs, messageId, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamAutoClaimResult StreamAutoClaim(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue consumerGroup, StackExchange.Redis.RedisValue claimingConsumer, long minIdleTimeInMs, StackExchange.Redis.RedisValue startAtId, int? count = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamAutoClaim(key, consumerGroup, claimingConsumer, minIdleTimeInMs, startAtId, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamAutoClaimIdsOnlyResult StreamAutoClaimIdsOnly(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue consumerGroup, StackExchange.Redis.RedisValue claimingConsumer, long minIdleTimeInMs, StackExchange.Redis.RedisValue startAtId, int? count = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamAutoClaimIdsOnly(key, consumerGroup, claimingConsumer, minIdleTimeInMs, startAtId, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamEntry[] StreamClaim(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue consumerGroup, StackExchange.Redis.RedisValue claimingConsumer, long minIdleTimeInMs, StackExchange.Redis.RedisValue[] messageIds, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamClaim(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] StreamClaimIdsOnly(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue consumerGroup, StackExchange.Redis.RedisValue claimingConsumer, long minIdleTimeInMs, StackExchange.Redis.RedisValue[] messageIds, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamClaimIdsOnly(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags));
        }

        /// <inheritdoc />
        public bool StreamConsumerGroupSetPosition(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue position, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamConsumerGroupSetPosition(key, groupName, position, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamConsumerInfo[] StreamConsumerInfo(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamConsumerInfo(key, groupName, flags));
        }

        /// <inheritdoc />
        public bool StreamCreateConsumerGroup(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue? position, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.StreamCreateConsumerGroup(key, groupName, position, flags));
        }

        /// <inheritdoc />
        public bool StreamCreateConsumerGroup(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue? position = default, bool createStream = true, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamCreateConsumerGroup(key, groupName, position, createStream, flags));
        }

        /// <inheritdoc />
        public long StreamDelete(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] messageIds, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamDelete(key, messageIds, flags));
        }

        /// <inheritdoc />
        public long StreamDeleteConsumer(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamDeleteConsumer(key, groupName, consumerName, flags));
        }

        /// <inheritdoc />
        public bool StreamDeleteConsumerGroup(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamDeleteConsumerGroup(key, groupName, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamGroupInfo[] StreamGroupInfo(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamGroupInfo(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamInfo StreamInfo(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamInfo(key, flags));
        }

        /// <inheritdoc />
        public long StreamLength(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamLength(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamPendingInfo StreamPending(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamPending(key, groupName, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamPendingMessageInfo[] StreamPendingMessages(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, int count, StackExchange.Redis.RedisValue consumerName, StackExchange.Redis.RedisValue? minId = default, StackExchange.Redis.RedisValue? maxId = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamPendingMessages(key, groupName, count, consumerName, minId, maxId, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamEntry[] StreamRange(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue? minId = default, StackExchange.Redis.RedisValue? maxId = default, int? count = default, StackExchange.Redis.Order messageOrder = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamRange(key, minId, maxId, count, messageOrder, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamEntry[] StreamRead(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue position, int? count = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamRead(key, position, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisStream[] StreamRead(StackExchange.Redis.StreamPosition[] streamPositions, int? countPerStream = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamRead(streamPositions, countPerStream, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamEntry[] StreamReadGroup(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, StackExchange.Redis.RedisValue? position, int? count, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.StreamReadGroup(key, groupName, consumerName, position, count, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.StreamEntry[] StreamReadGroup(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, StackExchange.Redis.RedisValue? position = default, int? count = default, bool noAck = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamReadGroup(key, groupName, consumerName, position, count, noAck, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisStream[] StreamReadGroup(StackExchange.Redis.StreamPosition[] streamPositions, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, int? countPerStream, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.StreamReadGroup(streamPositions, groupName, consumerName, countPerStream, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisStream[] StreamReadGroup(StackExchange.Redis.StreamPosition[] streamPositions, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, int? countPerStream = default, bool noAck = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamReadGroup(streamPositions, groupName, consumerName, countPerStream, noAck, flags));
        }

        /// <inheritdoc />
        public long StreamTrim(StackExchange.Redis.RedisKey key, int maxLength, bool useApproximateMaxLength = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StreamTrim(key, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public long StringAppend(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringAppend(key, value, flags));
        }

        /// <inheritdoc />
        public long StringBitCount(StackExchange.Redis.RedisKey key, long start, long end, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.StringBitCount(key, start, end, flags));
        }

        /// <inheritdoc />
        public long StringBitCount(StackExchange.Redis.RedisKey key, long start = 0, long end = -1, StackExchange.Redis.StringIndexType indexType = StackExchange.Redis.StringIndexType.Byte, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringBitCount(key, start, end, indexType, flags));
        }

        /// <inheritdoc />
        public long StringBitOperation(StackExchange.Redis.Bitwise operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringBitOperation(operation, destination, first, second, flags));
        }

        /// <inheritdoc />
        public long StringBitOperation(StackExchange.Redis.Bitwise operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringBitOperation(operation, destination, keys, flags));
        }

        /// <inheritdoc />
        public long StringBitPosition(StackExchange.Redis.RedisKey key, bool bit, long start, long end, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.StringBitPosition(key, bit, start, end, flags));
        }

        /// <inheritdoc />
        public long StringBitPosition(StackExchange.Redis.RedisKey key, bool bit, long start = 0, long end = -1, StackExchange.Redis.StringIndexType indexType = StackExchange.Redis.StringIndexType.Byte, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringBitPosition(key, bit, start, end, indexType, flags));
        }

        /// <inheritdoc />
        public long StringDecrement(StackExchange.Redis.RedisKey key, long value = 1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringDecrement(key, value, flags));
        }

        /// <inheritdoc />
        public double StringDecrement(StackExchange.Redis.RedisKey key, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringDecrement(key, value, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringGet(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGet(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue[] StringGet(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGet(keys, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.Lease<byte>? StringGetLease(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGetLease(key, flags));
        }

        /// <inheritdoc />
        public bool StringGetBit(StackExchange.Redis.RedisKey key, long offset, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGetBit(key, offset, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringGetRange(StackExchange.Redis.RedisKey key, long start, long end, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGetRange(key, start, end, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringGetSet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGetSet(key, value, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringGetSetExpiry(StackExchange.Redis.RedisKey key, System.TimeSpan? expiry, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGetSetExpiry(key, expiry, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringGetSetExpiry(StackExchange.Redis.RedisKey key, System.DateTime expiry, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGetSetExpiry(key, expiry, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringGetDelete(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGetDelete(key, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValueWithExpiry StringGetWithExpiry(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringGetWithExpiry(key, flags));
        }

        /// <inheritdoc />
        public long StringIncrement(StackExchange.Redis.RedisKey key, long value = 1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringIncrement(key, value, flags));
        }

        /// <inheritdoc />
        public double StringIncrement(StackExchange.Redis.RedisKey key, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringIncrement(key, value, flags));
        }

        /// <inheritdoc />
        public long StringLength(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringLength(key, flags));
        }

        /// <inheritdoc />
        public string? StringLongestCommonSubsequence(StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringLongestCommonSubsequence(first, second, flags));
        }

        /// <inheritdoc />
        public long StringLongestCommonSubsequenceLength(StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringLongestCommonSubsequenceLength(first, second, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.LCSMatchResult StringLongestCommonSubsequenceWithMatches(StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, long minLength = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringLongestCommonSubsequenceWithMatches(first, second, minLength, flags));
        }

        /// <inheritdoc />
        public bool StringSet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry, StackExchange.Redis.When when)
        {
            return ExecuteAction(() => _instance!.Value.StringSet(key, value, expiry, when));
        }

        /// <inheritdoc />
        public bool StringSet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry, StackExchange.Redis.When when, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.StringSet(key, value, expiry, when, flags));
        }

        /// <inheritdoc />
        public bool StringSet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry = default, bool keepTtl = false, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringSet(key, value, expiry, keepTtl, when, flags));
        }

        /// <inheritdoc />
        public bool StringSet(System.Collections.Generic.KeyValuePair<StackExchange.Redis.RedisKey, StackExchange.Redis.RedisValue>[] values, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringSet(values, when, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringSetAndGet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry, StackExchange.Redis.When when, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteAction(() => _instance!.Value.StringSetAndGet(key, value, expiry, when, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringSetAndGet(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry = default, bool keepTtl = false, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringSetAndGet(key, value, expiry, keepTtl, when, flags));
        }

        /// <inheritdoc />
        public bool StringSetBit(StackExchange.Redis.RedisKey key, long offset, bool bit, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringSetBit(key, offset, bit, flags));
        }

        /// <inheritdoc />
        public StackExchange.Redis.RedisValue StringSetRange(StackExchange.Redis.RedisKey key, long offset, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.StringSetRange(key, offset, value, flags));
        }

        /// <inheritdoc />
        public int Database => _instance!.Value.Database;

        /// <inheritdoc />
        public System.TimeSpan Ping(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.Ping(flags));
        }

        /// <inheritdoc />
        public bool IsConnected(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.IsConnected(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task KeyMigrateAsync(StackExchange.Redis.RedisKey key, System.Net.EndPoint toServer, int toDatabase = 0, int timeoutMilliseconds = 0, StackExchange.Redis.MigrateOptions migrateOptions = StackExchange.Redis.MigrateOptions.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyMigrateAsync(key, toServer, toDatabase, timeoutMilliseconds, migrateOptions, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> DebugObjectAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.DebugObjectAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> GeoAddAsync(StackExchange.Redis.RedisKey key, double longitude, double latitude, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoAddAsync(key, longitude, latitude, member, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> GeoAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.GeoEntry value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoAddAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> GeoAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.GeoEntry[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoAddAsync(key, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> GeoRemoveAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoRemoveAsync(key, member, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double?> GeoDistanceAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member1, StackExchange.Redis.RedisValue member2, StackExchange.Redis.GeoUnit unit = StackExchange.Redis.GeoUnit.Meters, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoDistanceAsync(key, member1, member2, unit, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string?[]> GeoHashAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] members, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoHashAsync(key, members, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string?> GeoHashAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoHashAsync(key, member, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.GeoPosition?[]> GeoPositionAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] members, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoPositionAsync(key, members, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.GeoPosition?> GeoPositionAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoPositionAsync(key, member, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.GeoRadiusResult[]> GeoRadiusAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double radius, StackExchange.Redis.GeoUnit unit = StackExchange.Redis.GeoUnit.Meters, int count = -1, StackExchange.Redis.Order? order = default, StackExchange.Redis.GeoRadiusOptions options = StackExchange.Redis.GeoRadiusOptions.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoRadiusAsync(key, member, radius, unit, count, order, options, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.GeoRadiusResult[]> GeoRadiusAsync(StackExchange.Redis.RedisKey key, double longitude, double latitude, double radius, StackExchange.Redis.GeoUnit unit = StackExchange.Redis.GeoUnit.Meters, int count = -1, StackExchange.Redis.Order? order = default, StackExchange.Redis.GeoRadiusOptions options = StackExchange.Redis.GeoRadiusOptions.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoRadiusAsync(key, longitude, latitude, radius, unit, count, order, options, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.GeoRadiusResult[]> GeoSearchAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.GeoSearchShape shape, int count = -1, bool demandClosest = true, StackExchange.Redis.Order? order = default, StackExchange.Redis.GeoRadiusOptions options = StackExchange.Redis.GeoRadiusOptions.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoSearchAsync(key, member, shape, count, demandClosest, order, options, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.GeoRadiusResult[]> GeoSearchAsync(StackExchange.Redis.RedisKey key, double longitude, double latitude, StackExchange.Redis.GeoSearchShape shape, int count = -1, bool demandClosest = true, StackExchange.Redis.Order? order = default, StackExchange.Redis.GeoRadiusOptions options = StackExchange.Redis.GeoRadiusOptions.Default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoSearchAsync(key, longitude, latitude, shape, count, demandClosest, order, options, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> GeoSearchAndStoreAsync(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, StackExchange.Redis.RedisValue member, StackExchange.Redis.GeoSearchShape shape, int count = -1, bool demandClosest = true, StackExchange.Redis.Order? order = default, bool storeDistances = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoSearchAndStoreAsync(sourceKey, destinationKey, member, shape, count, demandClosest, order, storeDistances, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> GeoSearchAndStoreAsync(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, double longitude, double latitude, StackExchange.Redis.GeoSearchShape shape, int count = -1, bool demandClosest = true, StackExchange.Redis.Order? order = default, bool storeDistances = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.GeoSearchAndStoreAsync(sourceKey, destinationKey, longitude, latitude, shape, count, demandClosest, order, storeDistances, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> HashDecrementAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, long value = 1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashDecrementAsync(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double> HashDecrementAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashDecrementAsync(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> HashDeleteAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashDeleteAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> HashDeleteAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] hashFields, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashDeleteAsync(key, hashFields, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> HashExistsAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashExistsAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> HashGetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashGetAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.Lease<byte>?> HashGetLeaseAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashGetLeaseAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> HashGetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] hashFields, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashGetAsync(key, hashFields, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.HashEntry[]> HashGetAllAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashGetAllAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> HashIncrementAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, long value = 1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashIncrementAsync(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double> HashIncrementAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashIncrementAsync(key, hashField, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> HashKeysAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashKeysAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> HashLengthAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> HashRandomFieldAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashRandomFieldAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> HashRandomFieldsAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashRandomFieldsAsync(key, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.HashEntry[]> HashRandomFieldsWithValuesAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashRandomFieldsWithValuesAsync(key, count, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IAsyncEnumerable<StackExchange.Redis.HashEntry> HashScanAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.HashScanAsync(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task HashSetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.HashEntry[] hashFields, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashSetAsync(key, hashFields, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> HashSetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.RedisValue value, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashSetAsync(key, hashField, value, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> HashStringLengthAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue hashField, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashStringLengthAsync(key, hashField, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> HashValuesAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HashValuesAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> HyperLogLogAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HyperLogLogAddAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> HyperLogLogAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HyperLogLogAddAsync(key, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> HyperLogLogLengthAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HyperLogLogLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> HyperLogLogLengthAsync(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HyperLogLogLengthAsync(keys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task HyperLogLogMergeAsync(StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HyperLogLogMergeAsync(destination, first, second, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task HyperLogLogMergeAsync(StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey[] sourceKeys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.HyperLogLogMergeAsync(destination, sourceKeys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.Net.EndPoint?> IdentifyEndpointAsync(StackExchange.Redis.RedisKey key = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.IdentifyEndpointAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyCopyAsync(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, int destinationDatabase = -1, bool replace = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyCopyAsync(sourceKey, destinationKey, destinationDatabase, replace, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyDeleteAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyDeleteAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> KeyDeleteAsync(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyDeleteAsync(keys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<byte[]?> KeyDumpAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyDumpAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string?> KeyEncodingAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyEncodingAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyExistsAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyExistsAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> KeyExistsAsync(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyExistsAsync(keys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyExpireAsync(StackExchange.Redis.RedisKey key, System.TimeSpan? expiry, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyExpireAsync(key, expiry, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyExpireAsync(StackExchange.Redis.RedisKey key, System.TimeSpan? expiry, StackExchange.Redis.ExpireWhen when = StackExchange.Redis.ExpireWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyExpireAsync(key, expiry, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyExpireAsync(StackExchange.Redis.RedisKey key, System.DateTime? expiry, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyExpireAsync(key, expiry, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyExpireAsync(StackExchange.Redis.RedisKey key, System.DateTime? expiry, StackExchange.Redis.ExpireWhen when = StackExchange.Redis.ExpireWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyExpireAsync(key, expiry, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.DateTime?> KeyExpireTimeAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyExpireTimeAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long?> KeyFrequencyAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyFrequencyAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.TimeSpan?> KeyIdleTimeAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyIdleTimeAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyMoveAsync(StackExchange.Redis.RedisKey key, int database, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyMoveAsync(key, database, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyPersistAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyPersistAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisKey> KeyRandomAsync(StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyRandomAsync(flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long?> KeyRefCountAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyRefCountAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyRenameAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisKey newKey, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyRenameAsync(key, newKey, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task KeyRestoreAsync(StackExchange.Redis.RedisKey key, byte[] value, System.TimeSpan? expiry = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyRestoreAsync(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<System.TimeSpan?> KeyTimeToLiveAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyTimeToLiveAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> KeyTouchAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyTouchAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> KeyTouchAsync(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyTouchAsync(keys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisType> KeyTypeAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.KeyTypeAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> ListGetByIndexAsync(StackExchange.Redis.RedisKey key, long index, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListGetByIndexAsync(key, index, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListInsertAfterAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pivot, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListInsertAfterAsync(key, pivot, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListInsertBeforeAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pivot, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListInsertBeforeAsync(key, pivot, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> ListLeftPopAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListLeftPopAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> ListLeftPopAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListLeftPopAsync(key, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.ListPopResult> ListLeftPopAsync(StackExchange.Redis.RedisKey[] keys, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListLeftPopAsync(keys, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListPositionAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue element, long rank = 1, long maxLength = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListPositionAsync(key, element, rank, maxLength, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long[]> ListPositionsAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue element, long count, long rank = 1, long maxLength = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListPositionsAsync(key, element, count, rank, maxLength, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListLeftPushAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListLeftPushAsync(key, value, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListLeftPushAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListLeftPushAsync(key, values, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListLeftPushAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListLeftPushAsync(key, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListLengthAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> ListMoveAsync(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, StackExchange.Redis.ListSide sourceSide, StackExchange.Redis.ListSide destinationSide, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListMoveAsync(sourceKey, destinationKey, sourceSide, destinationSide, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> ListRangeAsync(StackExchange.Redis.RedisKey key, long start = 0, long stop = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRangeAsync(key, start, stop, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListRemoveAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, long count = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRemoveAsync(key, value, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> ListRightPopAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRightPopAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> ListRightPopAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRightPopAsync(key, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.ListPopResult> ListRightPopAsync(StackExchange.Redis.RedisKey[] keys, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRightPopAsync(keys, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> ListRightPopLeftPushAsync(StackExchange.Redis.RedisKey source, StackExchange.Redis.RedisKey destination, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRightPopLeftPushAsync(source, destination, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListRightPushAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRightPushAsync(key, value, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListRightPushAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRightPushAsync(key, values, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> ListRightPushAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListRightPushAsync(key, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ListSetByIndexAsync(StackExchange.Redis.RedisKey key, long index, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListSetByIndexAsync(key, index, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task ListTrimAsync(StackExchange.Redis.RedisKey key, long start, long stop, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ListTrimAsync(key, start, stop, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> LockExtendAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan expiry, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LockExtendAsync(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> LockQueryAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LockQueryAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> LockReleaseAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LockReleaseAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> LockTakeAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan expiry, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.LockTakeAsync(key, value, expiry, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> PublishAsync(StackExchange.Redis.RedisChannel channel, StackExchange.Redis.RedisValue message, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.PublishAsync(channel, message, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> ExecuteAsync(string command, object[] args)
        {
            return ExecuteActionAsync(() => _instance!.Value.ExecuteAsync(command, args));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> ExecuteAsync(string command, System.Collections.Generic.ICollection<object>? args, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ExecuteAsync(command, args, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> ScriptEvaluateAsync(string script, StackExchange.Redis.RedisKey[]? keys = default, StackExchange.Redis.RedisValue[]? values = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptEvaluateAsync(script, keys, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> ScriptEvaluateAsync(byte[] hash, StackExchange.Redis.RedisKey[]? keys = default, StackExchange.Redis.RedisValue[]? values = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptEvaluateAsync(hash, keys, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> ScriptEvaluateAsync(StackExchange.Redis.LuaScript script, object? parameters = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptEvaluateAsync(script, parameters, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisResult> ScriptEvaluateAsync(StackExchange.Redis.LoadedLuaScript script, object? parameters = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.ScriptEvaluateAsync(script, parameters, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SetAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetAddAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SetAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetAddAsync(key, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SetCombineAsync(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetCombineAsync(operation, first, second, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SetCombineAsync(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetCombineAsync(operation, keys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SetCombineAndStoreAsync(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetCombineAndStoreAsync(operation, destination, first, second, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SetCombineAndStoreAsync(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetCombineAndStoreAsync(operation, destination, keys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SetContainsAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetContainsAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool[]> SetContainsAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetContainsAsync(key, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SetIntersectionLengthAsync(StackExchange.Redis.RedisKey[] keys, long limit = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetIntersectionLengthAsync(keys, limit, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SetLengthAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SetMembersAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetMembersAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SetMoveAsync(StackExchange.Redis.RedisKey source, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetMoveAsync(source, destination, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> SetPopAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetPopAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SetPopAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetPopAsync(key, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> SetRandomMemberAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetRandomMemberAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SetRandomMembersAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetRandomMembersAsync(key, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SetRemoveAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetRemoveAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SetRemoveAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] values, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SetRemoveAsync(key, values, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IAsyncEnumerable<StackExchange.Redis.RedisValue> SetScanAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SetScanAsync(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SortAsync(StackExchange.Redis.RedisKey key, long skip = 0, long take = -1, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.SortType sortType = StackExchange.Redis.SortType.Numeric, StackExchange.Redis.RedisValue by = default, StackExchange.Redis.RedisValue[]? get = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortAsync(key, skip, take, order, sortType, by, get, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortAndStoreAsync(StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey key, long skip = 0, long take = -1, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.SortType sortType = StackExchange.Redis.SortType.Numeric, StackExchange.Redis.RedisValue by = default, StackExchange.Redis.RedisValue[]? get = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortAndStoreAsync(destination, key, skip, take, order, sortType, by, get, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SortedSetAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double score, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetAddAsync(key, member, score, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SortedSetAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double score, StackExchange.Redis.When when, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetAddAsync(key, member, score, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SortedSetAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double score, StackExchange.Redis.SortedSetWhen when = StackExchange.Redis.SortedSetWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetAddAsync(key, member, score, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.SortedSetEntry[] values, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetAddAsync(key, values, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.SortedSetEntry[] values, StackExchange.Redis.When when, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetAddAsync(key, values, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.SortedSetEntry[] values, StackExchange.Redis.SortedSetWhen when = StackExchange.Redis.SortedSetWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetAddAsync(key, values, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SortedSetCombineAsync(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey[] keys, double[]? weights = default, StackExchange.Redis.Aggregate aggregate = StackExchange.Redis.Aggregate.Sum, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetCombineAsync(operation, keys, weights, aggregate, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.SortedSetEntry[]> SortedSetCombineWithScoresAsync(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey[] keys, double[]? weights = default, StackExchange.Redis.Aggregate aggregate = StackExchange.Redis.Aggregate.Sum, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetCombineWithScoresAsync(operation, keys, weights, aggregate, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetCombineAndStoreAsync(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.Aggregate aggregate = StackExchange.Redis.Aggregate.Sum, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetCombineAndStoreAsync(operation, destination, first, second, aggregate, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetCombineAndStoreAsync(StackExchange.Redis.SetOperation operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey[] keys, double[]? weights = default, StackExchange.Redis.Aggregate aggregate = StackExchange.Redis.Aggregate.Sum, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetCombineAndStoreAsync(operation, destination, keys, weights, aggregate, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double> SortedSetDecrementAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetDecrementAsync(key, member, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double> SortedSetIncrementAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetIncrementAsync(key, member, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetIntersectionLengthAsync(StackExchange.Redis.RedisKey[] keys, long limit = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetIntersectionLengthAsync(keys, limit, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetLengthAsync(StackExchange.Redis.RedisKey key, double min = double.NegativeInfinity, double max = double.PositiveInfinity, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetLengthAsync(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetLengthByValueAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue min, StackExchange.Redis.RedisValue max, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetLengthByValueAsync(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> SortedSetRandomMemberAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRandomMemberAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SortedSetRandomMembersAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRandomMembersAsync(key, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.SortedSetEntry[]> SortedSetRandomMembersWithScoresAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRandomMembersWithScoresAsync(key, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SortedSetRangeByRankAsync(StackExchange.Redis.RedisKey key, long start = 0, long stop = -1, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRangeByRankAsync(key, start, stop, order, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetRangeAndStoreAsync(StackExchange.Redis.RedisKey sourceKey, StackExchange.Redis.RedisKey destinationKey, StackExchange.Redis.RedisValue start, StackExchange.Redis.RedisValue stop, StackExchange.Redis.SortedSetOrder sortedSetOrder = StackExchange.Redis.SortedSetOrder.ByRank, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, long skip = 0, long? take = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRangeAndStoreAsync(sourceKey, destinationKey, start, stop, sortedSetOrder, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(StackExchange.Redis.RedisKey key, long start = 0, long stop = -1, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SortedSetRangeByScoreAsync(StackExchange.Redis.RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, long skip = 0, long take = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRangeByScoreAsync(key, start, stop, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.SortedSetEntry[]> SortedSetRangeByScoreWithScoresAsync(StackExchange.Redis.RedisKey key, double start = double.NegativeInfinity, double stop = double.PositiveInfinity, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, long skip = 0, long take = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SortedSetRangeByValueAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue min, StackExchange.Redis.RedisValue max, StackExchange.Redis.Exclude exclude, long skip, long take = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRangeByValueAsync(key, min, max, exclude, skip, take, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> SortedSetRangeByValueAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue min = default, StackExchange.Redis.RedisValue max = default, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, long skip = 0, long take = -1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRangeByValueAsync(key, min, max, exclude, order, skip, take, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long?> SortedSetRankAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRankAsync(key, member, order, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SortedSetRemoveAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRemoveAsync(key, member, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetRemoveAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] members, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRemoveAsync(key, members, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetRemoveRangeByRankAsync(StackExchange.Redis.RedisKey key, long start, long stop, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRemoveRangeByRankAsync(key, start, stop, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetRemoveRangeByScoreAsync(StackExchange.Redis.RedisKey key, double start, double stop, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetRemoveRangeByValueAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue min, StackExchange.Redis.RedisValue max, StackExchange.Redis.Exclude exclude = StackExchange.Redis.Exclude.None, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetRemoveRangeByValueAsync(key, min, max, exclude, flags));
        }

        /// <inheritdoc />
        public System.Collections.Generic.IAsyncEnumerable<StackExchange.Redis.SortedSetEntry> SortedSetScanAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue pattern = default, int pageSize = 250, long cursor = 0, int pageOffset = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteAction(() => _instance!.Value.SortedSetScanAsync(key, pattern, pageSize, cursor, pageOffset, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double?> SortedSetScoreAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetScoreAsync(key, member, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double?[]> SortedSetScoresAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] members, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetScoresAsync(key, members, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> SortedSetUpdateAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue member, double score, StackExchange.Redis.SortedSetWhen when = StackExchange.Redis.SortedSetWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetUpdateAsync(key, member, score, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> SortedSetUpdateAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.SortedSetEntry[] values, StackExchange.Redis.SortedSetWhen when = StackExchange.Redis.SortedSetWhen.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetUpdateAsync(key, values, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.SortedSetEntry?> SortedSetPopAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetPopAsync(key, order, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.SortedSetEntry[]> SortedSetPopAsync(StackExchange.Redis.RedisKey key, long count, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetPopAsync(key, count, order, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.SortedSetPopResult> SortedSetPopAsync(StackExchange.Redis.RedisKey[] keys, long count, StackExchange.Redis.Order order = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.SortedSetPopAsync(keys, count, order, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StreamAcknowledgeAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue messageId, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamAcknowledgeAsync(key, groupName, messageId, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StreamAcknowledgeAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue[] messageIds, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamAcknowledgeAsync(key, groupName, messageIds, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StreamAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue streamField, StackExchange.Redis.RedisValue streamValue, StackExchange.Redis.RedisValue? messageId = default, int? maxLength = default, bool useApproximateMaxLength = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamAddAsync(key, streamField, streamValue, messageId, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StreamAddAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.NameValueEntry[] streamPairs, StackExchange.Redis.RedisValue? messageId = default, int? maxLength = default, bool useApproximateMaxLength = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamAddAsync(key, streamPairs, messageId, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamAutoClaimResult> StreamAutoClaimAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue consumerGroup, StackExchange.Redis.RedisValue claimingConsumer, long minIdleTimeInMs, StackExchange.Redis.RedisValue startAtId, int? count = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamAutoClaimAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, startAtId, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamAutoClaimIdsOnlyResult> StreamAutoClaimIdsOnlyAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue consumerGroup, StackExchange.Redis.RedisValue claimingConsumer, long minIdleTimeInMs, StackExchange.Redis.RedisValue startAtId, int? count = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamAutoClaimIdsOnlyAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, startAtId, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamEntry[]> StreamClaimAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue consumerGroup, StackExchange.Redis.RedisValue claimingConsumer, long minIdleTimeInMs, StackExchange.Redis.RedisValue[] messageIds, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamClaimAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> StreamClaimIdsOnlyAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue consumerGroup, StackExchange.Redis.RedisValue claimingConsumer, long minIdleTimeInMs, StackExchange.Redis.RedisValue[] messageIds, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamClaimIdsOnlyAsync(key, consumerGroup, claimingConsumer, minIdleTimeInMs, messageIds, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StreamConsumerGroupSetPositionAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue position, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamConsumerGroupSetPositionAsync(key, groupName, position, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamConsumerInfo[]> StreamConsumerInfoAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamConsumerInfoAsync(key, groupName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StreamCreateConsumerGroupAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue? position, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamCreateConsumerGroupAsync(key, groupName, position, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StreamCreateConsumerGroupAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue? position = default, bool createStream = true, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamCreateConsumerGroupAsync(key, groupName, position, createStream, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StreamDeleteAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue[] messageIds, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamDeleteAsync(key, messageIds, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StreamDeleteConsumerAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamDeleteConsumerAsync(key, groupName, consumerName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StreamDeleteConsumerGroupAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamDeleteConsumerGroupAsync(key, groupName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamGroupInfo[]> StreamGroupInfoAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamGroupInfoAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamInfo> StreamInfoAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamInfoAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StreamLengthAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamPendingInfo> StreamPendingAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamPendingAsync(key, groupName, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamPendingMessageInfo[]> StreamPendingMessagesAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, int count, StackExchange.Redis.RedisValue consumerName, StackExchange.Redis.RedisValue? minId = default, StackExchange.Redis.RedisValue? maxId = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamPendingMessagesAsync(key, groupName, count, consumerName, minId, maxId, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamEntry[]> StreamRangeAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue? minId = default, StackExchange.Redis.RedisValue? maxId = default, int? count = default, StackExchange.Redis.Order messageOrder = StackExchange.Redis.Order.Ascending, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamRangeAsync(key, minId, maxId, count, messageOrder, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamEntry[]> StreamReadAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue position, int? count = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamReadAsync(key, position, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisStream[]> StreamReadAsync(StackExchange.Redis.StreamPosition[] streamPositions, int? countPerStream = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamReadAsync(streamPositions, countPerStream, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamEntry[]> StreamReadGroupAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, StackExchange.Redis.RedisValue? position, int? count, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamReadGroupAsync(key, groupName, consumerName, position, count, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.StreamEntry[]> StreamReadGroupAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, StackExchange.Redis.RedisValue? position = default, int? count = default, bool noAck = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamReadGroupAsync(key, groupName, consumerName, position, count, noAck, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisStream[]> StreamReadGroupAsync(StackExchange.Redis.StreamPosition[] streamPositions, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, int? countPerStream, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamReadGroupAsync(streamPositions, groupName, consumerName, countPerStream, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisStream[]> StreamReadGroupAsync(StackExchange.Redis.StreamPosition[] streamPositions, StackExchange.Redis.RedisValue groupName, StackExchange.Redis.RedisValue consumerName, int? countPerStream = default, bool noAck = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamReadGroupAsync(streamPositions, groupName, consumerName, countPerStream, noAck, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StreamTrimAsync(StackExchange.Redis.RedisKey key, int maxLength, bool useApproximateMaxLength = false, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StreamTrimAsync(key, maxLength, useApproximateMaxLength, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringAppendAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringAppendAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringBitCountAsync(StackExchange.Redis.RedisKey key, long start, long end, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringBitCountAsync(key, start, end, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringBitCountAsync(StackExchange.Redis.RedisKey key, long start = 0, long end = -1, StackExchange.Redis.StringIndexType indexType = StackExchange.Redis.StringIndexType.Byte, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringBitCountAsync(key, start, end, indexType, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringBitOperationAsync(StackExchange.Redis.Bitwise operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second = default, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringBitOperationAsync(operation, destination, first, second, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringBitOperationAsync(StackExchange.Redis.Bitwise operation, StackExchange.Redis.RedisKey destination, StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringBitOperationAsync(operation, destination, keys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringBitPositionAsync(StackExchange.Redis.RedisKey key, bool bit, long start, long end, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringBitPositionAsync(key, bit, start, end, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringBitPositionAsync(StackExchange.Redis.RedisKey key, bool bit, long start = 0, long end = -1, StackExchange.Redis.StringIndexType indexType = StackExchange.Redis.StringIndexType.Byte, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringBitPositionAsync(key, bit, start, end, indexType, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringDecrementAsync(StackExchange.Redis.RedisKey key, long value = 1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringDecrementAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double> StringDecrementAsync(StackExchange.Redis.RedisKey key, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringDecrementAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringGetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue[]> StringGetAsync(StackExchange.Redis.RedisKey[] keys, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetAsync(keys, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.Lease<byte>?> StringGetLeaseAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetLeaseAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StringGetBitAsync(StackExchange.Redis.RedisKey key, long offset, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetBitAsync(key, offset, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringGetRangeAsync(StackExchange.Redis.RedisKey key, long start, long end, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetRangeAsync(key, start, end, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringGetSetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetSetAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringGetSetExpiryAsync(StackExchange.Redis.RedisKey key, System.TimeSpan? expiry, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetSetExpiryAsync(key, expiry, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringGetSetExpiryAsync(StackExchange.Redis.RedisKey key, System.DateTime expiry, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetSetExpiryAsync(key, expiry, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringGetDeleteAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetDeleteAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValueWithExpiry> StringGetWithExpiryAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringGetWithExpiryAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringIncrementAsync(StackExchange.Redis.RedisKey key, long value = 1, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringIncrementAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<double> StringIncrementAsync(StackExchange.Redis.RedisKey key, double value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringIncrementAsync(key, value, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringLengthAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringLengthAsync(key, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<string?> StringLongestCommonSubsequenceAsync(StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringLongestCommonSubsequenceAsync(first, second, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<long> StringLongestCommonSubsequenceLengthAsync(StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringLongestCommonSubsequenceLengthAsync(first, second, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.LCSMatchResult> StringLongestCommonSubsequenceWithMatchesAsync(StackExchange.Redis.RedisKey first, StackExchange.Redis.RedisKey second, long minLength = 0, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringLongestCommonSubsequenceWithMatchesAsync(first, second, minLength, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StringSetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry, StackExchange.Redis.When when)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringSetAsync(key, value, expiry, when));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StringSetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry, StackExchange.Redis.When when, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringSetAsync(key, value, expiry, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StringSetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry = default, bool keepTtl = false, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringSetAsync(key, value, expiry, keepTtl, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StringSetAsync(System.Collections.Generic.KeyValuePair<StackExchange.Redis.RedisKey, StackExchange.Redis.RedisValue>[] values, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringSetAsync(values, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringSetAndGetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry, StackExchange.Redis.When when, StackExchange.Redis.CommandFlags flags)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringSetAndGetAsync(key, value, expiry, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringSetAndGetAsync(StackExchange.Redis.RedisKey key, StackExchange.Redis.RedisValue value, System.TimeSpan? expiry = default, bool keepTtl = false, StackExchange.Redis.When when = StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringSetAndGetAsync(key, value, expiry, keepTtl, when, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<bool> StringSetBitAsync(StackExchange.Redis.RedisKey key, long offset, bool bit, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringSetBitAsync(key, offset, bit, flags));
        }

        /// <inheritdoc />
        public System.Threading.Tasks.Task<StackExchange.Redis.RedisValue> StringSetRangeAsync(StackExchange.Redis.RedisKey key, long offset, StackExchange.Redis.RedisValue value, StackExchange.Redis.CommandFlags flags = StackExchange.Redis.CommandFlags.None)
        {
            return ExecuteActionAsync(() => _instance!.Value.StringSetRangeAsync(key, offset, value, flags));
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
            _instance = new AtomicLazy<StackExchange.Redis.IDatabase>(_instanceProvider);
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
