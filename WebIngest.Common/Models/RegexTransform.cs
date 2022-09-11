using System.Linq;
using System.Text.RegularExpressions;
using WebIngest.Common.Extensions;

namespace WebIngest.Common.Models
{
    public class RegexTransform
    {
        public string DoTransform(string input)
        {
            if (!string.IsNullOrEmpty(FindPattern))
                input = DoRegexReplace(input);
            if (!string.IsNullOrEmpty(MatchPattern))
                input = DoRegexMatch(input);
            return input;
        }
        /// <summary>
        /// Regex find selector
        /// </summary>
        public string FindPattern { get; set; }

        /// <summary>
        /// Regex replace with
        /// </summary>
        public string ReplacePattern { get; set; } = string.Empty;

        private string DoRegexReplace(string input)
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

        private string DoRegexMatch(string input)
        {
            return string.IsNullOrEmpty(input)
                ? null
                : Regex.Matches(input, MatchPattern)
                    .Select(x => x
                        .Groups
                        .Values
                        .Last()
                    )
                    .StringJoin(MatchResultSeparator);
        }
    }
}