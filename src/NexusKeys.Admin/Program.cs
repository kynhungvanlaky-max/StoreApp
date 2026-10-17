var builder = WebApplication.CreateBuilder(args);

// Keep local/server logging portable and avoid requiring Windows Event Log privileges.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<Backend.HttpHandlers.TokenHandler>();

builder.Services.AddHttpClient("API", client =>
{
    var baseUrl = builder.Configuration.GetValue<string>("ApiSettings:BaseUrl") ?? "http://localhost:5021/";
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<Backend.HttpHandlers.TokenHandler>();

var app = builder.Build();

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
