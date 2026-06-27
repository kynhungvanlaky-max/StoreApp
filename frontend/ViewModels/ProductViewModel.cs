using Frontend.Models.Product;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class ProductViewModel
    {
        private readonly ProductService _productService;

        public List<ProductDto> Products { get; private set; } = new();
        public bool IsLoading { get; private set; }
        public int CurrentPage { get; private set; } = 1;
        public int TotalPages { get; private set; } = 1;
        public string SearchQuery { get; set; } = "";
        public List<string> SelectedCategories { get; set; } = new();
        public List<string> SelectedPriceRanges { get; set; } = new();
        public string SortBy { get; set; } = "newest";

        public event Action? StateChanged;

        public ProductViewModel(ProductService productService)
        {
            _productService = productService;
        }

        public async Task LoadAsync(int page = 1)
        {
            IsLoading = true;
            StateChanged?.Invoke();

            var result = await _productService.GetProductsAsync(page, 20, SearchQuery, SelectedCategories, SelectedPriceRanges, SortBy);
            if (result != null)
            {
                Products = result.Items;
                TotalPages = result.TotalPages;
                CurrentPage = result.PageNumber;
            }

            IsLoading = false;
            StateChanged?.Invoke();
        }

        public async Task SearchAsync(string query)
        {
            SearchQuery = query;
            await LoadAsync(1);
        }

        public async Task ToggleCategoryAsync(string category)
        {
            if (SelectedCategories.Contains(category))
                SelectedCategories.Remove(category);
            else
                SelectedCategories.Add(category);
            
            await LoadAsync(1);
        }

        public async Task TogglePriceRangeAsync(string range)
        {
            if (SelectedPriceRanges.Contains(range))
                SelectedPriceRanges.Remove(range);
            else
                SelectedPriceRanges.Add(range);

            await LoadAsync(1);
        }

        public async Task SetSortByAsync(string sortBy)
        {
            SortBy = sortBy;
            await LoadAsync(1);
        }
    }
}
