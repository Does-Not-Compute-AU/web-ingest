using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WebIngest.Common.Extensions
{
    public static class PrimitiveTypeExtensions
    {
        public static bool ContainsAny(this string @this, params string[] checkfor)
        {
            return checkfor.Any(check => @this.Contains(check));
        }

        public static bool EndsWithAny(this string @this, params string[] checkfor)
        {
            return checkfor.Any(check => @this.EndsWith(check));
        }
        
        public static bool IsNullOrEmpty(this string @this)
        {
            return String.IsNullOrEmpty(@this);
        }
        
        public static string RegexReplace(this string @this, string pattern, string replacement = "")
        {
            return Regex.Replace(@this, pattern, replacement);
        }

        public static string RemoveWhitespace(this string @this)
        {
            return @this.RegexReplace(@"\s+", string.Empty);
        }

        public static string RemoveHtmlWhitespace(this string @this)
        {
            return @this.RegexReplace(@">(\s)<", string.Empty);
        }

        public static string NullIfEmpty(this string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }

        public static string NullIfWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        public static T ParseEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static string CreateMd5(this string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }

        public static string PrintTruncate(this int num) => PrintTruncate(Convert.ToDecimal(num));
        public static string PrintTruncate(this long num) => PrintTruncate(Convert.ToDecimal(num));

        public static string PrintTruncate(this decimal num) => num switch
        {
            > 999999999999999 => num.ToString("0,,,,,.#QD", CultureInfo.InvariantCulture),
            > 999999999999 => num.ToString("0,,,,.#T", CultureInfo.InvariantCulture),
            > 999999999 => num.ToString("0,,,.#B", CultureInfo.InvariantCulture),
            > 999999 => num.ToString("0,,.#M", CultureInfo.InvariantCulture),
            > 999 => num.ToString("0,.#K", CultureInfo.InvariantCulture),
            _ => num.ToString(CultureInfo.InvariantCulture)
        };
    }
}