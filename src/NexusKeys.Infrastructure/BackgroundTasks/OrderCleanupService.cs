using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GameStore.API.Data;

namespace GameStore.API.BackgroundTasks
{
    public class OrderCleanupService : BackgroundService
    {
        private readonly ILogger<OrderCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public OrderCleanupService(ILogger<OrderCleanupService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupPendingOrdersAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing order cleanup.");
                }
            }

            _logger.LogInformation("Order Cleanup Service is stopping.");
        }

        private async Task CleanupPendingOrdersAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();

            // Lấy các đơn hàng Pending quá 24h
            var expirationTime = DateTime.UtcNow.AddDays(-1);
            var expiredOrders = await dbContext.Orders
                .Include(o => o.ProductKeys)
                .Where(o => o.Status == "Pending" && o.OrderDate < expirationTime)
                .ToListAsync(cancellationToken);

            if (expiredOrders.Any())
            {
                foreach (var order in expiredOrders)
                {
                    order.Status = "Cancelled";
                    
                    // Giải phóng Key đã bị giữ
                    foreach (var key in order.ProductKeys)
                    {
                        key.IsSold = false;
                        key.OrderId = null;
                    }

                    _logger.LogInformation("Cancelled expired pending order {OrderId}", order.Id);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully cleaned up {Count} expired orders.", expiredOrders.Count);
            }
        }
    }
}
