using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VelvetCakes.Api.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Явно указываем WebRootPath и ContentRoot
builder.WebHost.UseWebRoot("wwwroot");
builder.WebHost.UseContentRoot(Directory.GetCurrentDirectory());

// Настройка подключения к PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Поддержка переменной окружения DATABASE_URL (для Render)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Парсим DATABASE_URL: postgresql://user:pass@host:port/db
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};Port={uri.Port};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(connectionString));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5500";
        policy.WithOrigins(frontendUrl, "http://localhost:5500", "http://localhost:3000", "https://*.onrender.com", "https://*.github.io")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY") ?? "ThisIsMyVerySecureSecretKey123!";

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    o.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка размера файлов
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

var app = builder.Build();

// Создаём папку wwwroot/uploads
var wwwrootPath = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPath = Path.Combine(wwwrootPath, "uploads");

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    Console.WriteLine($"✅ Создана папка: {uploadsPath}");
}

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Инициализация базы данных
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Применяем миграции или создаём базу
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✅ База данных готова");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка базы данных: {ex.Message}");
    }

    // Добавляем роли, если их нет
    if (!context.Roles.Any())
    {
        context.Roles.AddRange(
            new Role { Name = "user", Description = "Обычный пользователь" },
            new Role { Name = "manager", Description = "Менеджер" },
            new Role { Name = "pastry_chef", Description = "Кондитер" }
        );
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Роли созданы");
    }

    // Добавляем компоненты, если их нет
    if (!context.Components.Any())
    {
        context.Components.AddRange(
            new Component { Type = "filling", Name = "Клубника", BasePricePerUnit = 300, CreatedAt = DateTime.UtcNow },
            new Component { Type = "filling", Name = "Черника", BasePricePerUnit = 350, CreatedAt = DateTime.UtcNow },
            new Component { Type = "filling", Name = "Шоколад", BasePricePerUnit = 400, CreatedAt = DateTime.UtcNow },
            new Component { Type = "filling", Name = "Карамель", BasePricePerUnit = 380, CreatedAt = DateTime.UtcNow },
            new Component { Type = "filling", Name = "Малина", BasePricePerUnit = 320, CreatedAt = DateTime.UtcNow },
            new Component { Type = "cake_base", Name = "Ванильный бисквит", BasePricePerUnit = 200, CreatedAt = DateTime.UtcNow },
            new Component { Type = "cake_base", Name = "Шоколадный бисквит", BasePricePerUnit = 220, CreatedAt = DateTime.UtcNow },
            new Component { Type = "cake_base", Name = "Медовый бисквит", BasePricePerUnit = 250, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();
        Console.WriteLine("✅ Компоненты созданы");
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");