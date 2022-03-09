using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;

namespace WebIngest.Common.FileExtensions
{
    public static class CsvFileHelpers
    {
        public static async Task<IList<IDictionary<string, object>>> ReadCsvAsync(
            string fileText,
            int? numRecords = null)
        {
            return await ReadCsvAsync(new StringReader(fileText), numRecords);
        }

        public static async Task<IList<IDictionary<string, object>>> ReadCsvAsync(
            Stream fileStream,
            int? numRecords = null)
        {
            return await ReadCsvAsync(new StreamReader(fileStream), numRecords);
        }

        public static async Task<IList<IDictionary<string, object>>> ReadCsvAsync(
            TextReader reader,
            int? numRecords = null)
        {
            var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = null
            };
            using var csv = new CsvReader(reader, csvConfig);

            var records = new List<IDictionary<string, object>>();
            var iterator = 0;
            await foreach (IDictionary<string, object> record in csv.GetRecordsAsync<dynamic>())
            {
                iterator++;
                records.Add(record);
                if (iterator == numRecords)
                    break;
            }

            return records;
        }
    }
}