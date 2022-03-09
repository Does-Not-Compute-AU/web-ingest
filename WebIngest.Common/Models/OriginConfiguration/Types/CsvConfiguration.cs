namespace WebIngest.Common.Models.OriginConfiguration.Types
{
    public class CsvConfiguration
    {
        public string Encoding { get; set; }
        public bool ContainsHeaders { get; set; }
        public string[] Headers { get; set; }
    }
}