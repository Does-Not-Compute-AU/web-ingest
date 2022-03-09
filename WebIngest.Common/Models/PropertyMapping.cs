using WebIngest.Common.Models.OriginConfiguration;

namespace WebIngest.Common.Models
{
    public class PropertyMapping
    {
        /// <summary>
        /// Name of column in mapped DataType
        /// </summary>
        public string DataTypeProperty { get; set; }

        /// <summary>
        /// Selector with which to extract value from data for this column
        /// </summary>
        public string Selector { get; set; }

        private static readonly char literalIndicator = '@';
        
        /// <summary>
        /// Whether or not the selector value is actually a selector, or a pass-through literal
        /// </summary>
        public bool SelectorIsLiteral => Selector?.StartsWith(literalIndicator) == true;

        /// <summary>
        /// Returns this mappings selector, but with any recognised embedded variables parsed out
        /// </summary>
        /// <param name="passThroughVars">A map of the variables that this selector might contain</param>
        /// <returns></returns>
        public string TransformSelectorVars(OriginTypeConfiguration originConfig)
        {
            if (originConfig == null)
                return Selector;
            
            var newSelector = Selector;
            foreach (var varName in originConfig.PassThroughVars.Keys)
                newSelector = newSelector.Replace("{{" + varName + "}}",  originConfig.PassThroughVars[varName].ToString());
                
            return SelectorIsLiteral ? newSelector.TrimStart(literalIndicator) : newSelector;
        }

        public RegexTransform RegexTransform { get; set; } = new();
    }
}