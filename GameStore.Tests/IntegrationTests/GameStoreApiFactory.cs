using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using GameStore.API.Data;

namespace GameStore.Tests.IntegrationTests
{
    public class GameStoreApiFactory : WebApplicationFactory<object>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GameStoreDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add InMemory database for testing
                services.AddDbContext<GameStoreDbContext>(options =>
                {
                    options.UseInMemoryDatabase("GameStoreTestDb");
                });

                // Ensure database is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }
}