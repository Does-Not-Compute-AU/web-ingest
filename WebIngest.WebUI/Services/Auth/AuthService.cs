using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using WebIngest.Common.Models.AuthModels;

namespace WebIngest.WebUI.Services.Auth
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiAuthenticationStateProvider _authenticationStateProvider;

        public AuthService(HttpClient httpClient,
            AuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClient;
            _authenticationStateProvider = (ApiAuthenticationStateProvider)authenticationStateProvider;
        }

        public async Task<AuthenticationState> GetCurrentAuthSate()
        {
            return await  _authenticationStateProvider.GetAuthenticationStateAsync();
        }

        public async Task<bool> IsLoggedIn()
        {
            var authState = await  _authenticationStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity is {IsAuthenticated: true};
        }

        public async Task<RegisterResult> Register(RegisterModel registerModel)
        {
            var result = await _httpClient.PostAsJsonAsync("api/accounts", registerModel);
            return await result.Content.ReadFromJsonAsync<RegisterResult>();
        }

        public async Task<LoginResult> Login(LoginModel loginModel)
        {
            var response = await _httpClient.PostAsJsonAsync("api/login", loginModel);
            var loginResult = await response.Content.ReadFromJsonAsync<LoginResult>();
            
            if (!response.IsSuccessStatusCode || loginResult?.Token == null)
            {
                return loginResult;
            }
            
            await _authenticationStateProvider.SetTokenAsync(loginResult.Token, loginResult.Expiry);
            return loginResult;
        }

        public async Task Logout()
        {
            await _authenticationStateProvider.SetTokenAsync(null);
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}