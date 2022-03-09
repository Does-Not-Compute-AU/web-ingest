using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;

namespace WebIngest.WebUI.Services.Auth
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public ApiAuthenticationStateProvider(
            HttpClient httpClient,
            ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await GetTokenAsync();

            var identity = string.IsNullOrEmpty(token)
                ? new ClaimsIdentity()
                : new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public async Task<string> GetTokenAsync()
        {
            string token = null;
            var expiry = await _localStorage.GetItemAsync<DateTime>("authTokenExpiry");
            if (expiry != default)
            {
                if (expiry > DateTime.Now)
                    token = await _localStorage.GetItemAsync<string>("authToken");
                else
                    await SetTokenAsync(null);
            }

            SetHttpClientToken(token);
            return token;
        }

        public async Task SetTokenAsync(string token, DateTime expiry = default)
        {
            if (token == null)
            {
                await _localStorage.RemoveItemAsync("authToken");
                await _localStorage.RemoveItemAsync("authTokenExpiry");
            }
            else
            {
                await _localStorage.SetItemAsync("authToken", token);
                await _localStorage.SetItemAsync("authTokenExpiry", expiry);
            }

            SetHttpClientToken(token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private void SetHttpClientToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = token == null
                ? null
                : new AuthenticationHeaderValue("bearer", token);
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            return keyValuePairs
                .Select(kvp =>
                    new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty)
                );
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}