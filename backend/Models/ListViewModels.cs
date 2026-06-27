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

    public class ProductListViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
    }

    public class OrderListViewModel
    {
        public List<OrderViewModel> Orders { get; set; } = new List<OrderViewModel>();
    }

    public class UserListViewModel
    {
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
    }

    public class KeyListViewModel
    {
        public List<ProductKeyViewModel> Keys { get; set; } = new List<ProductKeyViewModel>();
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CommentListViewModel
    {
        public List<CommentViewModel> Comments { get; set; } = new List<CommentViewModel>();
    }

    public class NewsListViewModel
    {
        public List<NewsViewModel> NewsArticles { get; set; } = new List<NewsViewModel>();
    }

    public class SupportTicketViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AdminReply { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? RepliedAt { get; set; }
    }

    public class SupportListViewModel
    {
        public List<SupportTicketViewModel> Tickets { get; set; } = new List<SupportTicketViewModel>();
    }

    public class TransactionViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentGateway { get; set; } = string.Empty;
        public string? GatewayTransactionId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TransactionListViewModel
    {
        public List<TransactionViewModel> Transactions { get; set; } = new List<TransactionViewModel>();
    }

    public class CouponViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public int MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PromotionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public List<int> ProductIds { get; set; } = new List<int>();
    }

    public class AuditLogViewModel
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string AdminUsername { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogListViewModel
    {
        public List<AuditLogViewModel> Logs { get; set; } = new List<AuditLogViewModel>();
    }
}
