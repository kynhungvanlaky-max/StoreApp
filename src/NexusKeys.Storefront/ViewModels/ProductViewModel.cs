using Frontend.Models.Product;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class ProductViewModel
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;

        public List<ProductDto> Products { get; private set; } = new();
        public List<CategoryItemDto> AvailableCategories { get; private set; } = new();
        public bool IsLoading { get; private set; } = true;
        public bool HasLoadedInitialData { get; private set; }
        public int CurrentPage { get; private set; } = 1;
        public int TotalPages { get; private set; } = 1;
        public string SearchQuery { get; set; } = "";
        public string? SelectedType { get; set; }
        public List<string> SelectedCategories { get; set; } = new();
        public List<string> SelectedPriceRanges { get; set; } = new();
        public string SortBy { get; set; } = "newest";
        public string? ErrorMessage { get; private set; }

        public event Action? StateChanged;

        public ProductViewModel(ProductService productService, CategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;
        }

        public async Task LoadCategoriesAsync()
        {
            if (!AvailableCategories.Any())
            {
                AvailableCategories = await _categoryService.GetCategoriesAsync();
                StateChanged?.Invoke();
            }
        }

        public async Task LoadAsync(int page = 1)
        {
            IsLoading = true;
            StateChanged?.Invoke();

            try
            {
                if (!AvailableCategories.Any())
                {
                    var catTask = _categoryService.GetCategoriesAsync();
                    var prodTask = _productService.GetProductsAsync(page, 20, SearchQuery, SelectedCategories, SelectedPriceRanges, SortBy, SelectedType);
                    await Task.WhenAll(catTask, prodTask);
                    AvailableCategories = await catTask;
                    var result = await prodTask;
                    ErrorMessage = _productService.LastError;
                    if (result != null)
                    {
                        Products = result.Items;
                        TotalPages = result.TotalPages;
                        CurrentPage = result.PageNumber;
                    }
                }
                else
                {
                    var result = await _productService.GetProductsAsync(page, 20, SearchQuery, SelectedCategories, SelectedPriceRanges, SortBy, SelectedType);
                    ErrorMessage = _productService.LastError;
                    if (result != null)
                    {
                        Products = result.Items;
                        TotalPages = result.TotalPages;
                        CurrentPage = result.PageNumber;
                    }
                }
            }
            finally
            {
                IsLoading = false;
                HasLoadedInitialData = true;
                StateChanged?.Invoke();
            }
        }

        public async Task SearchAsync(string query)
        {
            SearchQuery = query;
            await LoadAsync(1);
        }

        public async Task SetTypeAsync(string? type)
        {
            SelectedType = type;
            SelectedCategories.Clear();
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
