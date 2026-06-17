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
}
