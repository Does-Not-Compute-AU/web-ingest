using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using WebIngest.WebUI.Services;
using WebIngest.WebUI.Services.Auth;
using WebIngest.WebUI.Services.Hubs;

namespace WebIngest.WebUI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            
            builder.RootComponents.Add<App>("#app");
            var apiAddress = builder.Configuration["WebApi:BaseUri"] ?? builder.HostEnvironment.BaseAddress;
            var apiBaseUri = new Uri(apiAddress);
            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = apiBaseUri,
                Timeout = TimeSpan.FromMinutes(10)
            });
            
            builder.Services.AddScoped<EntityService>();
            builder.Services.AddScoped<DataService>();
            builder.Services.AddScoped<ScriptingService>();
            builder.Services.AddSingleton<SignalRService>();
            
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthService>();
                
            builder.Services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 5000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            });
            await builder.Build().RunAsync();
        }
    }
}
