using System.Threading;

namespace GameStore.API.Data
{
    public static class CacheKeys
    {
        public static bool IsEnabled { get; set; } = true;

        private static long _productVersion = 1;
        private static long _newsVersion = 1;
        private static long _promotionVersion = 1;
        private static long _dashboardVersion = 1;

        public static long ProductVersion => Interlocked.Read(ref _productVersion);
        public static long NewsVersion => Interlocked.Read(ref _newsVersion);
        public static long PromotionVersion => Interlocked.Read(ref _promotionVersion);
        public static long DashboardVersion => Interlocked.Read(ref _dashboardVersion);

        public static void InvalidateProducts()
        {
            Interlocked.Increment(ref _productVersion);
            Interlocked.Increment(ref _dashboardVersion);
        }

        public static void InvalidateNews()
        {
            Interlocked.Increment(ref _newsVersion);
        }

        public static void InvalidatePromotions()
        {
            Interlocked.Increment(ref _promotionVersion);
            Interlocked.Increment(ref _productVersion);
        }

        public static void InvalidateDashboard()
        {
            Interlocked.Increment(ref _dashboardVersion);
        }
    }
}
