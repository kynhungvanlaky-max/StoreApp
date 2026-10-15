using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using GameStore.API.Data;

namespace GameStore.Tests.IntegrationTests
{
    public class GameStoreApiFactory : WebApplicationFactory<GameStore.API.ApiMarker>
    {
        private static readonly Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot InMemoryRoot = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<GameStoreDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(GameStoreDbContext)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                // Add InMemory database for testing with isolated provider
                services.AddDbContext<GameStoreDbContext>(options =>
                {
                    options.UseInMemoryDatabase("GameStoreTestDb", InMemoryRoot);
                    options.UseInternalServiceProvider(inMemoryProvider);
                });
            });
        }
    }
}