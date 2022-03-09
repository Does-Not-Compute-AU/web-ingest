using System.Collections.Generic;
using System.Linq;
using WebIngest.Common.FileExtensions;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;

namespace WebIngest.Core.Extraction
{
    public class CsvExtractor : ResultExtractor
    {
        public static IDictionary<string, object>[] ValuesFromCsv(
            Mapping mapping,
            ContentTypeConfiguration configuration,
            OriginTypeConfiguration originConfig,
            string csvData)
        {
            var csvResults = CsvFileHelpers.ReadCsvAsync(csvData).Result.ToArray();

            var allValues = new List<IDictionary<string, object>>();
            foreach (var obj in csvResults)
            {
                var objectValues = new Dictionary<string, object>();
                foreach (var prop in mapping.PropertyMappings)
                {
                    var parsedSelector = prop.TransformSelectorVars(originConfig);
                    string val = prop.SelectorIsLiteral ? parsedSelector : obj[parsedSelector].ToString();
                    
                    objectValues.Add(prop.DataTypeProperty, ParseValue(mapping, prop, val));
                }
                allValues.Add(objectValues);
            }

            return allValues.ToArray();
        }
    }
}