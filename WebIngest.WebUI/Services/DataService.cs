using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json.Linq;
using WebIngest.Common.Extensions;

namespace WebIngest.WebUI.Services
{
    public class DataService
    {
        private readonly HttpClient _http;

        public DataService(HttpClient http)
        {
            _http = http;
        }

        public async Task<IEnumerable<JObject>> GetData(int dataTypeId)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<JObject>>(
                await _http.GetStringAsync($"api/data/{dataTypeId}"));
        }

        public async Task<HttpResponseMessage> PostDataFiles(int dataTypeId, params IBrowserFile[] files)
        {
            var form = new MultipartFormDataContent();
            foreach (var file in files)
            {
                await using var stream = file.OpenReadStream(int.MaxValue);
                var fileBytes = await stream.ReadFully();
                var fileContent = new ByteArrayContent(fileBytes);
                var compressed = new CompressedContent(fileContent, CompressionMethod.GZip, file.ContentType);
                
                form.Add(compressed, $"gzip-{file.ContentType}-file", file.Name);
            }
            return await _http.PostAsync($"api/data/{dataTypeId}", form);
        }
    }
}