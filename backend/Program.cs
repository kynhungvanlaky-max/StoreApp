var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<Backend.Filters.AdminAuthorizeFilter>();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<Backend.HttpHandlers.TokenHandler>();
builder.Services.AddScoped<Backend.Services.ApiService>();

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
