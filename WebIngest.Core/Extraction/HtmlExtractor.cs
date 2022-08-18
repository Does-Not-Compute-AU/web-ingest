using System.Collections.Generic;
using System.Linq;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;

namespace WebIngest.Core.Extraction
{
    public class HtmlExtractor : ResultExtractor
    {
        public static IDictionary<string, object>[] ValuesFromHtml(
            Mapping mapping,
            ContentTypeConfiguration configuration,
            OriginTypeConfiguration originConfig,
            string html
        )
        {
            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(html);

            // do html-selectors for non-literals
            var objectValues = new List<IDictionary<string, object>>();
            foreach (var prop in mapping.PropertyMappings.Where(p => !p.SelectorIsLiteral))
            {
                List<string> extractedResults = null;
                // get all nodes that match selector

                var selector = prop.TransformSelectorVars(originConfig);
                var htmlNodes = htmlDoc.QuerySelectorAll(selector);
                extractedResults = htmlNodes
                    .Select(x =>
                        x.Attributes["value"]?.Value.NullIfEmpty() ??
                        x.TextContent.NullIfEmpty() ??
                        x.Attributes["content"]?.Value.NullIfEmpty()
                    )
                    .ToList(); // add them to the values list

                if (extractedResults.Any())
                    for (int i = 0; i < extractedResults.Count; i++)
                    {
                        if (objectValues.ElementAtOrDefault(i) == null)
                            objectValues.Add(new Dictionary<string, object>());
                        objectValues[i][prop.DataTypeProperty] = ParseValue(mapping, prop, extractedResults[i]);
                    }
            }

            // apply literals to all discovered objects
            var propertyLiterals =
                mapping
                    .PropertyMappings
                    .Where(p => p.SelectorIsLiteral)
                    .ToDictionary(
                        p => p.DataTypeProperty,
                        p => ParseValue(mapping, p, p.TransformSelectorVars(originConfig))
                    );

            foreach (var obj in objectValues)
            foreach (var key in propertyLiterals.Keys)
                obj[key] = propertyLiterals[key];

            return objectValues.ToArray();
        }
    }
}