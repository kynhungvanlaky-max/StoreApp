using Microsoft.EntityFrameworkCore;
using GameStore.API.Data;
using GameStore.API.Endpoints;
using GameStore.API.Hubs;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<GameStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextjs", builder =>
    {
        builder.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:5000")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

// Configure Payment Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<GameStore.API.Services.Payment.VnPayService>();
builder.Services.AddScoped<GameStore.API.Services.Payment.MoMoService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowNextjs");
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
if (System.IO.Directory.Exists(@"D:\DA\MediaStorage"))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(@"D:\DA\MediaStorage"),
        RequestPath = "/media",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        }
    });
}

// Map Endpoints
app.MapGet("/", () => "Nexus Store API is running successfully!");
app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapUserEndpoints();
app.MapOrderEndpoints();
app.MapProductKeyEndpoints();
app.MapUploadEndpoints();
app.MapAssistantEndpoints();
app.MapPaymentGatewayEndpoints();
app.MapNewsEndpoints();
app.MapReviewEndpoints();
app.MapHub<NexusHub>("/hubs/nexus");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
    try {
        db.Database.ExecuteSqlRaw("ALTER TABLE Reviews ADD CreatedAt datetime2 NOT NULL DEFAULT GETDATE();");
        db.Database.ExecuteSqlRaw("ALTER TABLE Reviews ADD UpdatedAt datetime2 NULL;");
    } catch {}
}

// Seed Database
DbSeeder.Seed(app.Services);

app.Run();
