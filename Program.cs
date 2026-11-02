using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=rentapp.db"));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var jwtKey = "SUPER_SECRET_KEY_FOR_RENTAPP_PRACTICE_1234567890";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// миграции + сиды при старте
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
    if (!db.Users.Any()) {
        db.Users.AddRange(
            new User { Email = "admin@rent.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "admin", FullName = "Админ" },
            new User { Email = "manager@rent.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"), Role = "manager", FullName = "Менеджер" },
            new User { Email = "client@rent.ru", PasswordHash = BCrypt.Net.BCrypt.HashPassword("client123"), Role = "client", FullName = "Клиент" }
        );
        db.Categories.AddRange(new Category { Name = "Спортинвентарь" }, new Category { Name = "Строительное" });
        db.SaveChanges();
        var cat = db.Categories.First();
        db.Equipment.AddRange(
            new Equipment { CategoryId = cat.Id, Name = "Сапборд (SUP-доска)", PricePerDay = 1200, TotalQty = 4, Description = "Надувной, 320 см, с веслом и насосом" },
            new Equipment { CategoryId = cat.Id, Name = "Квадрокоптер DJI Mini", PricePerDay = 2500, TotalQty = 2, Description = "4K видео, 3 аккумулятора, чехол" },
            new Equipment { CategoryId = cat.Id, Name = "Радиостанции (пара)", PricePerDay = 300, TotalQty = 8, Description = "Дальность до 5 км, 16 каналов" },
            new Equipment { CategoryId = cat.Id, Name = "Мангал складной", PricePerDay = 400, TotalQty = 10, Description = "Чугунный, с шампурами и решеткой" }
        );
        db.SaveChanges();
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();