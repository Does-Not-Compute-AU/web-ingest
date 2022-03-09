using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace WebIngest.Common.Extensions
{
    public static class HttpExtensions
    {
        public static string[] GetErrorMessages(this HttpResponseMessage res)
        {
            var content = res.Content.ReadAsStringAsync()
                .Result
                .FromJson<JObject>();

            return content["errors"]
                ?.ToObject<JObject>()
                ?.Properties()
                .SelectMany(x =>
                    x.Value.ToArray().Select(y => $"{x.Name}: {y}")
                )
                .ToArray();
        }
    }

    public enum CompressionMethod
    {
        GZip = 1,
        Deflate = 2
    }

    public class CompressedContent : HttpContent
    {
        private readonly HttpContent _originalContent;
        private readonly CompressionMethod _compressionMethod;

        public CompressedContent(HttpContent content, CompressionMethod compressionMethod, string contentType = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            _originalContent = content;
            _compressionMethod = compressionMethod;

            foreach (KeyValuePair<string, IEnumerable<string>> header in _originalContent.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (!String.IsNullOrEmpty(contentType))
                Headers.ContentType = new MediaTypeHeaderValue(contentType);

            Headers.ContentEncoding.Add(_compressionMethod.ToString().ToLowerInvariant());
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            if (_compressionMethod == CompressionMethod.GZip)
            {
                using (var gzipStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
                {
                    await _originalContent.CopyToAsync(gzipStream);
                }
            }
            else if (_compressionMethod == CompressionMethod.Deflate)
            {
                using (var deflateStream = new DeflateStream(stream, CompressionMode.Compress, leaveOpen: true))
                {
                    await _originalContent.CopyToAsync(deflateStream);
                }
            }
        }
    }
}