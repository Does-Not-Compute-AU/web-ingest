using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebIngest.Common.Extensions
{
    public static class StreamExtensions
    {
        public static string ReadAllText(this Stream fileStream)
        {
            var result = new StringBuilder();
            using (var reader = new StreamReader(fileStream))
            {
                while (reader.Peek() >= 0)
                    result.AppendLine(reader.ReadLine()); 
            }
            return result.ToString();
        }
        
        public static async Task<byte[]> ReadFully(this Stream input)
        {
            await using var ms = new MemoryStream();
            await input.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}