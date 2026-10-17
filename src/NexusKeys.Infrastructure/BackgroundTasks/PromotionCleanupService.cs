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
    public class PromotionCleanupService : BackgroundService
    {
        private readonly ILogger<PromotionCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PromotionCleanupService(ILogger<PromotionCleanupService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Promotion Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredPromotionsAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing promotion cleanup.");
                }
            }

            _logger.LogInformation("Promotion Cleanup Service is stopping.");
        }

        private async Task CleanupExpiredPromotionsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();

            var now = DateTime.UtcNow;

            // Xử lý Coupons đã hết hạn nhưng vẫn còn IsActive = true
            var expiredCoupons = await dbContext.Coupons
                .Where(c => c.IsActive && c.ExpiryDate < now)
                .ToListAsync(cancellationToken);

            if (expiredCoupons.Any())
            {
                foreach (var coupon in expiredCoupons)
                {
                    coupon.IsActive = false;
                    _logger.LogInformation("Deactivated expired coupon {Code}", coupon.Code);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            
            // Xử lý Coupons hết lượt dùng (nếu có logic MaxUsage > 0)
            var exhaustedCoupons = await dbContext.Coupons
                .Where(c => c.IsActive && c.MaxUsage > 0 && c.CurrentUsage >= c.MaxUsage)
                .ToListAsync(cancellationToken);

            if (exhaustedCoupons.Any())
            {
                foreach (var coupon in exhaustedCoupons)
                {
                    coupon.IsActive = false;
                    _logger.LogInformation("Deactivated exhausted coupon {Code} (Usage: {CurrentUsage}/{MaxUsage})", coupon.Code, coupon.CurrentUsage, coupon.MaxUsage);
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
