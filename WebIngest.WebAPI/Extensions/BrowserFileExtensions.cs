using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebIngest.WebAPI.Extensions
{
    public static class BrowserFileExtensions
    {
        public static async Task<Stream> OpenCompressedFileStream(this IFormFile file)
        {
            var inputFileStream = file.OpenReadStream();
            var transportFileStream = new MemoryStream();

            if (file.Headers["Content-Encoding"] == "gzip")
                await new GZipStream(inputFileStream, CompressionMode.Decompress).CopyToAsync(
                    transportFileStream);
            else if (file.Headers["Content-Encoding"] == "deflate")
                await new DeflateStream(inputFileStream, CompressionMode.Decompress).CopyToAsync(
                    transportFileStream);
            else
                await inputFileStream.CopyToAsync(transportFileStream);

            transportFileStream.Position = 0;

            return await Common.FileExtensions.BrowserFileExtensions.OpenCompressedFileStream(file.ContentType, transportFileStream);
        }
    }
}