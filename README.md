# StackExchange.Redis.Resilience
Provides a wrapper for `IConnectionMultiplexer` which is able to do a force reconnect when `StackExchange.Redis` fails to reconnect by using the logic from [LazyReconnect](https://gist.github.com/JonCole/925630df72be1351b21440625ff2671f#reconnecting-with-lazyt-pattern).

Nuget package:

- [StackExchange.Redis.Resilience](https://www.nuget.org/packages/StackExchange.Redis.Resilience/)

## Usage
```csharp
var configuration = "127.0.0.1";
var multiplexer = new ResilientConnectionMultiplexer(() => ConnectionMultiplexer.Connect(configuration));
var multiplexer2 = new ResilientConnectionMultiplexer(
    () => ConnectionMultiplexer.Connect(configuration),
    new ResilientConnectionConfiguration
    {
        ReconnectMinFrequency = TimeSpan.FromSeconds(60),
        ReconnectErrorThreshold = TimeSpan.FromSeconds(30)
    });
var multiplexer3 = new ResilientConnectionMultiplexer(
    () => ConnectionMultiplexer.Connect(configuration),
    () => ConnectionMultiplexer.ConnectAsync(configuration));
```

## Known limitations
- `IBatch` methods may throw an `ObjectDisposedException` when a reconnect was done after the batch was created.
- `ITransaction` methods may throw an `ObjectDisposedException` when a reconnect was done after the transaction was created.
- Enumerating a collection from `IDatabase.SetScan`, `IDatabase.HashScan` and `IDatabase.SortedSetScan` methods may throw an `ObjectDisposedException` when a reconnect was done while iterating the collection.