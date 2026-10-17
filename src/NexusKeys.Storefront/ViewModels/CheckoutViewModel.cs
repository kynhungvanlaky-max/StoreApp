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

            try
            {
                var response = await _orderService.CheckoutAsync(request);

                if (response != null)
                {
                    await _cartViewModel.RemoveSelectedItemsAsync();
                    return response;
                }

                ErrorMessage = "Thanh toán thất bại. Vui lòng kiểm tra phiên đăng nhập và thử lại.";
                _toastService.ShowError(ErrorMessage);
            }
            catch
            {
                ErrorMessage = "Không thể kết nối đến hệ thống thanh toán. Vui lòng thử lại sau.";
                _toastService.ShowError(ErrorMessage);
            }
            finally
            {
                IsProcessing = false;
                StateChanged?.Invoke();
            }

            return null;
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

                var (data, error) = await _orderService.ApplyCouponAsync(
                    CouponCodeInput.Trim(),
                    _cartViewModel.SelectedTotalAmount);

                if (data != null)
                {
                    AppliedCouponCode = CouponCodeInput.Trim();
                    DiscountPercent = data.DiscountPercent;
                    CouponMessage = $"Áp dụng mã thành công! Bạn được giảm {DiscountPercent}%.";
                    IsCouponSuccess = true;
                    _toastService.ShowSuccess("Áp dụng mã giảm giá thành công!");
                }
                else
                {
                    CouponMessage = error ?? "Mã giảm giá không hợp lệ.";
                    IsCouponSuccess = false;
                    AppliedCouponCode = null;
                    DiscountPercent = 0;
                }
            }
            catch
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

}
