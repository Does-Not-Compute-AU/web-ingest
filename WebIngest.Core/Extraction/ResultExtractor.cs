using System;
using System.Globalization;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models;

namespace WebIngest.Core.Extraction
{
    public abstract class ResultExtractor
    {
        public static object ParseValue(Mapping mapping, PropertyMapping propMapping, string value)
        {
            value = propMapping.RegexTransform?.DoTransform(value);
            var propType = mapping.DataType.PropertyTypeOf(propMapping.DataTypeProperty);
            object result = ParsePropertyType(propType, value);

            return result;
        }

        private static object ParsePropertyType(PropertyType property, string value)
        {
            return property switch
            {
                PropertyType.TEXT => ParseString(value),
                PropertyType.LONGTEXT => ParseString(value),
                PropertyType.NUMBER => ParseDecimal(value),
                PropertyType.MONEY => ParseDecimal(value),
                _ => throw new NotSupportedException()
            };
        }

        private static string ParseString(string value)
        {
            return value.Trim().NullIfEmpty();
        }
        

        private static decimal ParseDecimal(string value)
        {
            try
            {
                value = value.Replace("$", "");
                return decimal.Parse(value, NumberStyles.Any);
            }
            catch(Exception e)
            {
                throw new Exception($"{e.Message} | Value: {value}");
            }
        }
    }
}