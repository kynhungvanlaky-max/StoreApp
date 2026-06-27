using Frontend.Models.Order;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class OrderViewModel
    {
        private readonly OrderService _orderService;

        public List<OrderDto> Orders { get; private set; } = new();
        public OrderDto? CurrentOrder { get; private set; }
        public bool IsLoading { get; private set; }

        public event Action? StateChanged;

        public OrderViewModel(OrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task LoadOrdersAsync()
        {
            IsLoading = true;
            StateChanged?.Invoke();

            var orders = await _orderService.GetMyOrdersAsync();
            if (orders != null)
            {
                Orders = orders;
            }

            IsLoading = false;
            StateChanged?.Invoke();
        }

        public async Task LoadOrderDetailsAsync(int orderId)
        {
            IsLoading = true;
            CurrentOrder = null;
            StateChanged?.Invoke();

            CurrentOrder = await _orderService.GetOrderByIdAsync(orderId);

            IsLoading = false;
            StateChanged?.Invoke();
        }
    }
}
