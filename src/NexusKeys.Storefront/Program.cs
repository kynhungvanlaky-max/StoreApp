using Frontend.Components;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Configure Vietnamese culture for currency and numbers (e.g. 245.000 đ)
var viCulture = new System.Globalization.CultureInfo("vi-VN");
viCulture.NumberFormat.CurrencySymbol = "đ";
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = viCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = viCulture;

// Configure DataProtection to use local folder with resilient fallback for container non-root users
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
try
{
    Directory.CreateDirectory(dataProtectionKeysPath);
}
catch
{
    dataProtectionKeysPath = Path.Combine(Path.GetTempPath(), "NexusKeys-DataProtection-Keys");
    Directory.CreateDirectory(dataProtectionKeysPath);
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("GameStore.Frontend");

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});


// Keep local/server logging portable and avoid requiring Windows Event Log privileges.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure port dynamically for environments like Render, Railway, Cloud Run
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

// HttpClient -> API
builder.Services.AddHttpClient("API", client => {
    var rawUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5021/";
    if (!rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
        rawUrl = "https://" + rawUrl;
    }
    if (!rawUrl.EndsWith("/")) rawUrl += "/";
    client.BaseAddress = new Uri(rawUrl);
});

// Services & ViewModels will be registered here as they are created
builder.Services.AddScoped<Frontend.Services.ToastService>();
builder.Services.AddScoped<Frontend.Services.AuthService>();
builder.Services.AddScoped<Frontend.ViewModels.AuthViewModel>();
builder.Services.AddScoped<Frontend.Services.AuthorizedApiClient>();

builder.Services.AddScoped<Frontend.Services.ProductService>();
builder.Services.AddScoped<Frontend.Services.CategoryService>();
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

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found");

app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/health-storefront", (IConfiguration cfg) => { var apiBase = cfg["ApiSettings:BaseUrl"]; return Results.Ok(new { status = "healthy", apiBase }); });
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
