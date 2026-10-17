using Frontend.Models.Product;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class ProductDetailViewModel
    {
        private readonly ProductService _productService;
        private readonly ReviewService _reviewService;

        public ProductDto? Product { get; private set; }
        public bool IsLoading { get; private set; } = true;
        
        public event Action? StateChanged;

        public List<Frontend.Models.Review.ReviewDto> Reviews { get; private set; } = new();
        public List<ProductDto> RelatedProducts { get; private set; } = new();

        public ProductDetailViewModel(ProductService productService, ReviewService reviewService)
        {
            _productService = productService;
            _reviewService = reviewService;
        }

        public async Task LoadProductAsync(int id)
        {
            IsLoading = true;
            Product = null;
            StateChanged?.Invoke();

            try
            {
                var productTask = _productService.GetProductByIdAsync(id);
                var reviewsTask = _reviewService.GetProductReviewsAsync(id);

                await Task.WhenAll(productTask, reviewsTask);

                Product = await productTask;
                Reviews = await reviewsTask ?? new();
                
                if (Product != null && Product.Categories != null && Product.Categories.Any())
                {
                    var related = await _productService.GetProductsAsync(page: 1, pageSize: 5, categories: Product.Categories);
                    if (related?.Items != null)
                    {
                        RelatedProducts = related.Items.Where(p => p.Id != id).Take(4).ToList();
                    }
                }
                else
                {
                    RelatedProducts = new();
                }
            }
            finally
            {
                IsLoading = false;
                StateChanged?.Invoke();
            }
        }

        public async Task<bool> SubmitReviewAsync(Frontend.Models.Review.CreateReviewDto request)
        {
            var success = await _reviewService.CreateReviewAsync(request);
            if (success && Product != null)
            {
                // Reload reviews
                Reviews = await _reviewService.GetProductReviewsAsync(Product.Id);
                StateChanged?.Invoke();
            }
            return success;
        }

        public async Task<bool> SubmitReplyAsync(int reviewId, Frontend.Models.Review.ReplyReviewDto request)
        {
            var success = await _reviewService.ReplyToReviewAsync(reviewId, request);
            if (success && Product != null)
            {
                Reviews = await _reviewService.GetProductReviewsAsync(Product.Id);
                StateChanged?.Invoke();
            }
            return success;
        }
    }
}
