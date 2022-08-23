using System.Linq;
using System.Text.RegularExpressions;
using WebIngest.Common.Extensions;

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
        public string ReplacePattern { get; set; } = string.Empty;

        public string DoRegexReplace(string input)
        {
            return Regex.Replace(input, FindPattern, ReplacePattern);
        }

        /// <summary>
        /// Regex match-many
        /// </summary>
        public string MatchPattern { get; set; }

        /// <summary>
        /// Join character for string-concatenating the matches
        /// </summary>
        public string MatchResultSeparator { get; set; } = string.Empty;

        public string DoRegexMatch(string input)
        {
            return string.IsNullOrEmpty(input)
                ? null
                : Regex.Matches(input, MatchPattern)
                    .Select(x => x.Value)
                    .StringJoin(MatchResultSeparator);
        }
    }
}