using System;
using System.Text.RegularExpressions;

namespace WebIngest.Common.Models
{
    public class RegexTransform
    {
        /// <summary>
        /// Regex find selector
        /// </summary>
        public string FindPattern { get; set; }

        /// <summary>
        /// Regex replace with
        /// </summary>
        public string ReplacePattern { get; set; } = String.Empty;

        public string DoRegexReplace(string input)
        {
            return Regex.Replace(input, FindPattern, ReplacePattern);
        }
    }
}