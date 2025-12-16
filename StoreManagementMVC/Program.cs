using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using StoreManagementMVC.Data;
using StoreManagementMVC.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

#region Services

// 👉 API Controllers (trả JSON)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 👉 MVC (Admin / Razor Views nếu có)
builder.Services.AddControllersWithViews();

// 👉 DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 👉 Application Services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<PaymentService>();

// 👉 JWT Authentication (FIX HTML REDIRECT)
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        // ⭐ QUAN TRỌNG: chặn redirect HTML
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
        };
    });

// 👉 CORS cho Blazor WASM
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7027") // port Blazor
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

#endregion

var app = builder.Build();

#region Pipeline

app.UseHttpsRedirection();

// ⚠️ KHÔNG dùng UseStaticFiles nếu đây là API thuần
// app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowBlazorClient");

app.UseAuthentication();
app.UseAuthorization();

// 👉 API endpoints
app.MapControllers();

// 👉 MVC routes (Admin, Dashboard…)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

#endregion

app.Run();
