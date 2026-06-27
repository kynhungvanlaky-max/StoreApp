namespace Backend.Models
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int NewUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int AvailableKeys { get; set; }
        public int PendingSupportTickets { get; set; }
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
        public List<LowStockAlertViewModel> LowStockAlerts { get; set; } = new();
    }

    public class RecentOrderViewModel
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public string CustomerEmail { get; set; } = null!;
    }

    public class LowStockAlertViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int KeyCount { get; set; }
    }
}
