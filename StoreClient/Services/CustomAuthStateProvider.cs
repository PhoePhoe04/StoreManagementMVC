using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Text.Json;

namespace StoreClient.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _js;
        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

        public CustomAuthStateProvider(IJSRuntime js)
        {
            _js = js;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _js.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (string.IsNullOrEmpty(token))
            {
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }
            else
            {
                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                _currentUser = new ClaimsPrincipal(identity);
            }

            return new AuthenticationState(_currentUser);
        }

        public void NotifyUserChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var jsonBytes = Convert.FromBase64String(padded);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            return keyValuePairs!.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString() ?? ""));
        }
    }
}