namespace Backend.Models
{
    public class PagedResponse<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }

    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int CustomerCount { get; set; }
        public int TotalProducts { get; set; }
        public int AvailableKeys { get; set; }
        public int PendingSupportTickets { get; set; }
        public bool ApiAvailable { get; set; }
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
        public List<LowStockAlertViewModel> LowStockAlerts { get; set; } = new();
        public List<MonthlyRevenueViewModel> MonthlyRevenue { get; set; } = new();
        public List<OrderStatusViewModel> OrdersByStatus { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
    }

    public class RecentOrderViewModel
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
    }

    public class LowStockAlertViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int KeyCount { get; set; }
    }

    public class MonthlyRevenueViewModel
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
    }

    public class OrderStatusViewModel
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = "Game";
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = "Active";
        public string DeveloperName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Completed";
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public List<string> Items { get; set; } = new();
        public List<OrderKeyDto> Keys { get; set; } = new();
    }

    public class OrderKeyDto
    {
        public int Id { get; set; }
        public string KeyCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }

    public class ProductKeyViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string KeyCode { get; set; } = string.Empty;
        public bool IsSold { get; set; }
        public int? OrderId { get; set; }
        public string Status { get; set; } = "Chưa bán";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = "Customer";
        public bool IsLocked { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class CouponViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public int MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(30);
        public bool IsActive { get; set; } = true;
    }

    public class PromotionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(14);
        public bool IsActive { get; set; } = true;
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // --- NEW MODELS FOR 6 REMAINING PAGES ---

    public class ReviewViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPositive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<ReviewCommentDto> Replies { get; set; } = new();
    }

    public class ReviewCommentDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class NewsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; } = DateTime.Now;
    }

    public class SupportTicketViewModel
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? AdminReply { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class PaymentConfigViewModel
    {
        public int Id { get; set; }
        public string GatewayName { get; set; } = string.Empty;
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = "Deposit";
        public string PaymentGateway { get; set; } = "VnPay";
        public string? GatewayTransactionId { get; set; }
        public string Status { get; set; } = "Success";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class AuditLogViewModel
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string AdminUsername { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
