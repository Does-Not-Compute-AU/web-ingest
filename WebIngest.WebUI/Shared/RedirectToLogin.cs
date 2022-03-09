using Microsoft.AspNetCore.Components;

namespace WebIngest.WebUI.Shared
{
    public class RedirectToLogin : ComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        protected override void OnInitialized()
        {
            NavigationManager.NavigateTo("/auth/login");
        }
    }
}