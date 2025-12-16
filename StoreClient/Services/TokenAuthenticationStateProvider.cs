using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace StoreClient.Services
{
    public class TokenAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _http;

        public TokenAuthenticationStateProvider(IJSRuntime jsRuntime, HttpClient http)
        {
            _jsRuntime = jsRuntime;
            _http = http;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Set Authorization header for HttpClient
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);
                return new AuthenticationState(user);
            }
            catch
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void MarkUserAsAuthenticated(string token)
        {
            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            // Set header
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            _http.DefaultRequestHeaders.Authorization = null;
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return claims;
                var payload = parts[1];
                var jsonBytes = ParseBase64WithoutPadding(payload);
                var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
                if (keyValuePairs == null) return claims;

                if (keyValuePairs.TryGetValue("unique_name", out var uniqueName))
                    claims.Add(new Claim(ClaimTypes.Name, uniqueName.ToString()));
                else if (keyValuePairs.TryGetValue("name", out var name))
                    claims.Add(new Claim(ClaimTypes.Name, name.ToString()));
                else if (keyValuePairs.TryGetValue("sub", out var sub))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, sub.ToString()));

                if (keyValuePairs.TryGetValue("role", out var role))
                {
                    // role may be single or array
                    if (role is JsonElement je && je.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in je.EnumerateArray())
                        {
                            claims.Add(new Claim(ClaimTypes.Role, r.GetString()));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                    }
                }

                // Also include any other claim strings
                foreach (var kv in keyValuePairs)
                {
                    if (kv.Key == "unique_name" || kv.Key == "name" || kv.Key == "sub" || kv.Key == "role") continue;
                    if (kv.Value == null) continue;
                    claims.Add(new Claim(kv.Key, kv.Value.ToString()));
                }
            }
            catch
            {
                // ignore
            }

            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            string s = base64.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
