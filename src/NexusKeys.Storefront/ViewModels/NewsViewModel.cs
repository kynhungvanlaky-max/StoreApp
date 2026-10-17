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
        public bool IsLoading { get; private set; } = true;
        public bool HasLoadedInitialData { get; private set; } = false;
        
        public int CurrentPage { get; private set; } = 1;
        public int PageSize { get; private set; } = 10;
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

            try
            {
                var result = await _newsService.GetNewsAsync(page, PageSize);
                CurrentPage = page;
                TotalPages = result.TotalPages;
                PagedArticles = result.Items;
            }
            finally
            {
                IsLoading = false;
                HasLoadedInitialData = true;
                StateChanged?.Invoke();
            }
        }

        public async Task LoadArticleDetailsAsync(int id)
        {
            IsLoading = true;
            CurrentArticle = null;
            StateChanged?.Invoke();

            try
            {
                CurrentArticle = await _newsService.GetNewsByIdAsync(id);
            }
            finally
            {
                IsLoading = false;
                HasLoadedInitialData = true;
                StateChanged?.Invoke();
            }
        }
    }
}
