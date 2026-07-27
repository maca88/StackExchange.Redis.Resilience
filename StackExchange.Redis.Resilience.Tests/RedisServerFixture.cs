using System.Threading.Tasks;
using NUnit.Framework;
using Testcontainers.Redis;

namespace StackExchange.Redis.Resilience.Tests
{
    /// <summary>
    /// Starts a disposable Redis container for the duration of the test run so that tests no longer
    /// require a Redis instance to be started manually beforehand. The container is started once
    /// before any test runs and is disposed after all tests in the assembly have completed.
    /// </summary>
    [SetUpFixture]
    public class RedisServerFixture
    {
        private static RedisContainer _container;

        public static string ConnectionString => _container.GetConnectionString();

        [OneTimeSetUp]
        public async Task StartRedisAsync()
        {
            _container = new RedisBuilder("redis:7-alpine")
                .Build();

            await _container.StartAsync();
        }

        [OneTimeTearDown]
        public async Task StopRedisAsync()
        {
            if (_container != null)
            {
                await _container.DisposeAsync();
            }
        }
    }
}
