using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using StoreClient;
using StoreClient.Services;
using System.Net.Http.Headers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ToastService>();

// AuthHandler tự động thêm Bearer token
builder.Services.AddScoped<AuthHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthHandler>();
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri("https://localhost:7177/")
    };
    return client;
});

await builder.Build().RunAsync();

// AuthHandler class
public class AuthHandler : DelegatingHandler
{
    private readonly IJSRuntime _js;

    public AuthHandler(IJSRuntime js) => _js = js;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}