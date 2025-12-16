using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using StoreClient;
using StoreClient.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<StoreClient.Services.ToastService>();

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress/**/) });
// Đặt BaseAddress là URL của Admin (Backend)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7177/") });

await builder.Build().RunAsync();
