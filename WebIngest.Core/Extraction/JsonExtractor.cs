using System.Collections.Generic;
using WebIngest.Common.Models;
using Newtonsoft.Json.Linq;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models.OriginConfiguration;

namespace WebIngest.Core.Extraction
{
    public class JsonExtractor : ResultExtractor
    {
        public static IDictionary<string, object>[] ValuesFromJson(
            Mapping mapping, 
            ContentTypeConfiguration configuration,
            OriginTypeConfiguration originConfig,
            string json)
        {
            //convert json string to dynamic object
            dynamic dataObj = json.FromJson();
            IEnumerable<dynamic> objects = dataObj.GetType() == typeof(JArray)
                ? dataObj
                : new[] { dataObj };

            var allValues = new List<IDictionary<string, object>>();
            foreach (var obj in objects)
            {
                var objectValues = new Dictionary<string, object>();
                
                //loop through property mappings, look for value in dataObj at mapping key
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