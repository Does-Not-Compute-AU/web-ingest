using WebIngest.Common.Models.OriginConfiguration.Types;

namespace WebIngest.Common.Models.OriginConfiguration
{
    public class ContentTypeConfiguration
    {
        public CsvConfiguration CsvConfiguration { get; set; } = new();
    }
}