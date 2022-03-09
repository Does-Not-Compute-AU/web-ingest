using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace WebIngest.WebUI.Services
{
    public class ScriptingService
    {
        private readonly HttpClient _http;

        public ScriptingService(HttpClient http)
        {
            _http = http;
        }

        public async Task<HttpResponseMessage> TestScript(string script)
        {
            return await _http.PostAsJsonAsync("api/scripting", script);
        }
    }
}