var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<Backend.HttpHandlers.TokenHandler>();

builder.Services.AddHttpClient("API", client =>
{
    var baseUrl = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl") ?? builder.Configuration.GetValue<string>("ApiSettings__BaseUrl") ?? "https://nexuskeys-api.onrender.com/";
    if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) baseUrl = "https://" + baseUrl;
    if (!baseUrl.EndsWith("/")) baseUrl += "/";
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<Backend.HttpHandlers.TokenHandler>();

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Health check endpoints for Render, Railway, Docker probes
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "nexuskeys-admin" }));
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", service = "nexuskeys-admin" }));

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

// Tự động cấp Cookie Admin Token cho Browser của FE người dùng
app.UseMiddleware<Backend.Middleware.AdminAuthMiddleware>();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
