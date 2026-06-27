using Frontend.Models.News;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class NewsViewModel
    {
        private readonly NewsService _newsService;

        public List<NewsArticleDto> Articles { get; private set; } = new();
        public List<NewsArticleDto> PagedArticles { get; private set; } = new();
        public NewsArticleDto? CurrentArticle { get; private set; }
        public bool IsLoading { get; private set; }
        
        public int CurrentPage { get; private set; } = 1;
        public int PageSize { get; private set; } = 6;
        public int TotalPages { get; private set; }

        public event Action? StateChanged;

        public NewsViewModel(NewsService newsService)
        {
            _newsService = newsService;
        }

        public async Task LoadArticlesAsync()
        {
            await LoadPageAsync(1);
        }
        
        public async Task LoadPageAsync(int page)
        {
            if (page < 1) page = 1;
            
            IsLoading = true;
            StateChanged?.Invoke();

            var result = await _newsService.GetNewsAsync(page, PageSize);
            
            CurrentPage = page;
            TotalPages = result.TotalPages;
            PagedArticles = result.Items;
            
            IsLoading = false;
            StateChanged?.Invoke();
        }

        public async Task LoadArticleDetailsAsync(int id)
        {
            IsLoading = true;
            CurrentArticle = null;
            StateChanged?.Invoke();

            CurrentArticle = await _newsService.GetNewsByIdAsync(id);

            IsLoading = false;
            StateChanged?.Invoke();
        }
    }
}
