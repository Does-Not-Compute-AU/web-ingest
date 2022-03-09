using System;
using System.Globalization;
using WebIngest.Common.Models;

namespace WebIngest.Core.Extraction
{
    public abstract class ResultExtractor
    {
        public static object ParseValue(Mapping mapping, PropertyMapping propMapping, string value)
        {
            if (!string.IsNullOrEmpty(propMapping.RegexTransform?.FindPattern))
                value = propMapping.RegexTransform.DoRegexReplace(value);

            var propType = mapping.DataType.PropertyTypeOf(propMapping.DataTypeProperty);
            object result = ParsePropertyType(propType, value);

            return result;
        }

        private static object ParsePropertyType(PropertyType property, string value)
        {
            return property switch
            {
                PropertyType.TEXT => value,
                PropertyType.LONGTEXT => value,
                PropertyType.NUMBER => ParseDecimal(value),
                PropertyType.MONEY => ParseDecimal(value),
                _ => throw new NotSupportedException()
            };
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