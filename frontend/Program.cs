using Frontend.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

// HttpClient -> API
builder.Services.AddHttpClient("API", client => {
    client.BaseAddress = new Uri(
        builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5021/");
});

// Services & ViewModels will be registered here as they are created
builder.Services.AddScoped<Frontend.Services.ToastService>();
builder.Services.AddScoped<Frontend.Services.AuthService>();
builder.Services.AddScoped<Frontend.ViewModels.AuthViewModel>();

builder.Services.AddScoped<Frontend.Services.ProductService>();
builder.Services.AddScoped<Frontend.ViewModels.ProductViewModel>();
builder.Services.AddScoped<Frontend.ViewModels.ProductDetailViewModel>();

builder.Services.AddScoped<Frontend.Services.AssistantService>();
builder.Services.AddScoped<Frontend.ViewModels.AssistantViewModel>();

builder.Services.AddScoped<Frontend.Services.OrderService>();
builder.Services.AddScoped<Frontend.ViewModels.CartViewModel>();
builder.Services.AddScoped<Frontend.ViewModels.CheckoutViewModel>();

builder.Services.AddScoped<Frontend.Services.UserService>();
builder.Services.AddScoped<Frontend.ViewModels.ProfileViewModel>();
builder.Services.AddScoped<Frontend.ViewModels.OrderViewModel>();

builder.Services.AddScoped<Frontend.Services.WishlistService>();
builder.Services.AddScoped<Frontend.ViewModels.WishlistViewModel>();

builder.Services.AddScoped<Frontend.Services.NewsService>();
builder.Services.AddScoped<Frontend.ViewModels.NewsViewModel>();

builder.Services.AddScoped<Frontend.Services.ReviewService>();
builder.Services.AddScoped<Frontend.Services.WalletService>();
builder.Services.AddScoped<Frontend.Services.SupportService>();
builder.Services.AddScoped<Frontend.Services.RecentlyViewedService>();
builder.Services.AddScoped<Frontend.Services.RecommendationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
