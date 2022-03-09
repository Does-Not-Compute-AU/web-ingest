using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;


namespace WebIngest.Common.Extensions
{
    public static class JsonExtensions
    {
        public static string ToJson(this object @this, bool typeHandlingNotDefault = false)
        {
            if (typeHandlingNotDefault)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };
                return JsonConvert.SerializeObject(@this, settings);
            }
            return JsonConvert.SerializeObject(@this);
        }
        public static string ToJsonPretty(this object @this)
        {
            return SerializeObjectPretty(@this);
        }

        public static dynamic FromJson(this string @this) => JsonConvert.DeserializeObject<dynamic>(@this);
        
        public static T FromJson<T>(this string @this, bool typeHandlingNotDefault = false)
        {
            if (typeHandlingNotDefault)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                };
                return JsonConvert.DeserializeObject<T>(@this, settings);
            }
            return JsonConvert.DeserializeObject<T>(@this);
        }
        
        // copy of JsonConvert.SerializeObject but with 4 char indentation
        public static string SerializeObjectPretty<T>(T value)
        {
            StringBuilder sb = new StringBuilder(256);
            StringWriter sw = new StringWriter(sb, CultureInfo.InvariantCulture);

            var jsonSerializer = JsonSerializer.CreateDefault();
            using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.IndentChar = ' ';
                jsonWriter.Indentation = 4;

                jsonSerializer.Serialize(jsonWriter, value, typeof(T));
            }

            return sw.ToString();
        }
    }
}