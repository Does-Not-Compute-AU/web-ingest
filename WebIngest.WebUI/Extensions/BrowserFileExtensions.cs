using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace WebIngest.WebUI.Extensions
{
    public static class BrowserFileExtensions
    {
        public static async Task<Stream> OpenCompressedFileStream(this IBrowserFile file)
        {
            return await Common.FileExtensions.BrowserFileExtensions.OpenCompressedFileStream(file.ContentType, file.OpenReadStream(int.MaxValue));
        }
    }
}