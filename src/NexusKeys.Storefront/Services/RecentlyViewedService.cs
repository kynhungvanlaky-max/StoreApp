using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Frontend.Services
{
    public class RecentlyViewedService
    {
        private readonly ProtectedLocalStorage _storage;
        private const string StorageKey = "recently_viewed";
        private const int MaxItems = 10;

        public RecentlyViewedService(ProtectedLocalStorage storage)
        {
            _storage = storage;
        }

        public async Task<List<RecentlyViewedItem>> GetRecentlyViewedAsync()
        {
            try
            {
                var result = await _storage.GetAsync<List<RecentlyViewedItem>>(StorageKey);
                return result.Success ? result.Value ?? new List<RecentlyViewedItem>() : new List<RecentlyViewedItem>();
            }
            catch
            {
                return new List<RecentlyViewedItem>();
            }
        }

        public async Task AddRecentlyViewedAsync(int productId, string name, string imageUrl, decimal basePrice)
        {
            var items = await GetRecentlyViewedAsync();
            
            // Remove if already exists (to move to top)
            items.RemoveAll(i => i.ProductId == productId);
            
            // Add to front
            items.Insert(0, new RecentlyViewedItem
            {
                ProductId = productId,
                Name = name,
                ImageUrl = imageUrl,
                BasePrice = basePrice,
                ViewedAt = DateTime.UtcNow
            });
            
            // Keep only max items
            if (items.Count > MaxItems)
                items = items.Take(MaxItems).ToList();
            
            try
            {
                await _storage.SetAsync(StorageKey, items);
            }
            catch
            {
                // Ignore storage errors
            }
        }

        public async Task ClearRecentlyViewedAsync()
        {
            try
            {
                await _storage.DeleteAsync(StorageKey);
            }
            catch
            {
                // Ignore storage errors
            }
        }
    }

    public class RecentlyViewedItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public DateTime ViewedAt { get; set; }
    }
}