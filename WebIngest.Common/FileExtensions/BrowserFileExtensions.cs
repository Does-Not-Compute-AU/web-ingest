using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace WebIngest.Common.FileExtensions
{
    public static class BrowserFileExtensions
    {

        public static async Task<Stream> OpenCompressedFileStream(string contentType, Stream transportFileStream)

        {
            if (IsZipFile(contentType))
            {
                var ms = new MemoryStream();
                await transportFileStream.CopyToAsync(ms);
                var archive = new ZipArchive(ms);

                var firstEntry = archive.Entries?.FirstOrDefault();
                if (firstEntry == null)
                    throw new Exception("No entries found in zip archive");

                return firstEntry.Open();
            }

            return transportFileStream;
        }

        public static bool IsZipFile(string contentType)
        {
            return new[] {"zip", "application/x-zip-compressed"}.Contains(contentType);
        }
    }
}

