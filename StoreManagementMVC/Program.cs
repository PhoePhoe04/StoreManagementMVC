
﻿using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using StoreManagementMVC.Data;
using StoreManagementMVC.Services;
using System.Text;

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
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PromotionService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<UserService>();

//// 👉 JWT Authentication 
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
//        };

//        // ⭐ QUAN TRỌNG: chặn redirect HTML
//        options.Events = new JwtBearerEvents
//        {
//            OnChallenge = context =>
//            {
//                context.HandleResponse();
//                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
//                return Task.CompletedTask;
//            }
//        };
//    });



// 👉 CẤU HÌNH AUTHENTICATION (GỘP CHUNG JWT VÀ COOKIE)
builder.Services.AddAuthentication(options =>
{
    // Đặt mặc định là Cookie (Để trang Admin tự Redirect khi chưa đăng nhập)
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    // Cấu hình cho Admin MVC
    options.LoginPath = "/Admin/Account/Login";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    // Cấu hình cho API (Blazor gọi vào)
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

var app = builder.Build();
#endregion


#region Pipeline

app.UseHttpsRedirection();

// ⚠️ KHÔNG dùng UseStaticFiles nếu đây là API thuần
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowBlazorClient");

app.UseAuthentication(); // Xác thực danh tính (Bạn là ai?)
app.UseAuthorization(); // Phân quyền (Bạn được làm gì?)

// 👉 API endpoints
app.MapControllers();


// Route cho Admin Area
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// 👉 MVC routes (Admin, Dashboard…)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

#endregion

app.Run();
