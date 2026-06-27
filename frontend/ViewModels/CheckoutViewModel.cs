using Frontend.Models.Order;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class CheckoutViewModel
    {
        private readonly OrderService _orderService;
        private readonly CartViewModel _cartViewModel;
        private readonly ToastService _toastService;

        public bool IsProcessing { get; private set; }
        public string SelectedPaymentMethod { get; set; } = "VNPay";
        public string? ErrorMessage { get; private set; }
        
        public string CouponCodeInput { get; set; } = string.Empty;
        public string? AppliedCouponCode { get; private set; }
        public decimal DiscountPercent { get; private set; }
        public string? CouponMessage { get; private set; }
        public bool IsCouponSuccess { get; private set; }

        public event Action? StateChanged;

        public CheckoutViewModel(OrderService orderService, CartViewModel cartViewModel, ToastService toastService)
        {
            _orderService = orderService;
            _cartViewModel = cartViewModel;
            _toastService = toastService;
        }

        public async Task<CheckoutResponse?> ProcessCheckoutAsync()
        {
            var selectedItems = _cartViewModel.Items.Where(i => i.IsSelected).ToList();
            if (!selectedItems.Any())
            {
                ErrorMessage = "Vui lòng chọn ít nhất 1 sản phẩm để thanh toán.";
                return null;
            }

            IsProcessing = true;
            ErrorMessage = null;
            StateChanged?.Invoke();

            var request = new CheckoutRequest
            {
                ProductIds = selectedItems.Select(i => i.ProductId).ToList(),
                PaymentMethod = SelectedPaymentMethod,
                CouponCode = AppliedCouponCode
            };

            var response = await _orderService.CheckoutAsync(request);

            if (response != null)
            {
                // Xóa những sản phẩm đã chọn và thanh toán thành công
                await _cartViewModel.RemoveSelectedItemsAsync();
            }
            else
            {
                ErrorMessage = "Thanh toán thất bại. Vui lòng thử lại.";
                _toastService.ShowError(ErrorMessage);
            }

            IsProcessing = false;
            StateChanged?.Invoke();
            
            return response;
        }

        public async Task ApplyCouponAsync()
        {
            if (string.IsNullOrWhiteSpace(CouponCodeInput))
            {
                CouponMessage = "Vui lòng nhập mã giảm giá.";
                IsCouponSuccess = false;
                StateChanged?.Invoke();
                return;
            }

            try
            {
                IsProcessing = true;
                StateChanged?.Invoke();

                using var http = new HttpClient();
                var req = new { code = CouponCodeInput.Trim(), orderTotal = _cartViewModel.SelectedTotalAmount };
                var res = await http.PostAsJsonAsync("http://localhost:5021/api/coupons/apply", req);
                
                if (res.IsSuccessStatusCode)
                {
                    var data = await res.Content.ReadFromJsonAsync<CouponApplyResponse>();
                    if (data != null)
                    {
                        AppliedCouponCode = CouponCodeInput.Trim();
                        DiscountPercent = data.DiscountPercent;
                        CouponMessage = $"Áp dụng mã thành công! Bạn được giảm {DiscountPercent}%.";
                        IsCouponSuccess = true;
                        _toastService.ShowSuccess("Áp dụng mã giảm giá thành công!");
                    }
                }
                else
                {
                    var error = await res.Content.ReadAsStringAsync();
                    CouponMessage = error.Trim('"');
                    IsCouponSuccess = false;
                    AppliedCouponCode = null;
                    DiscountPercent = 0;
                }
            }
            catch (Exception ex)
            {
                CouponMessage = "Lỗi khi kiểm tra mã giảm giá.";
                IsCouponSuccess = false;
                AppliedCouponCode = null;
                DiscountPercent = 0;
            }
            finally
            {
                IsProcessing = false;
                StateChanged?.Invoke();
            }
        }
    }

    public class CouponApplyResponse
    {
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NewTotal { get; set; }
    }
}
