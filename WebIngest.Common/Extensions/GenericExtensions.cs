using System.Collections.Generic;
using System.Linq;

namespace WebIngest.Common.Extensions
{
    public static class GenericExtensions
    {
        
        public static IEnumerable<object> DynamicOrderBy(this IEnumerable<object> @this, string property)
        {
            return @this.OrderBy(x => 
                x.GetType()
                    .GetProperty(property)
                    ?.GetValue(x, null));
        }
	
        public static IEnumerable<object> DynamicOrderByDescending(this IEnumerable<object> @this, string property)
        {
            return @this.OrderByDescending(x => 
                x.GetType()
                    .GetProperty(property)
                    ?.GetValue(x, null));
        }

        public static void SetProperty(this object @this, string propertyName, object propertyValue)
        {
            @this.GetType().GetProperty(propertyName).SetValue(@this, propertyValue);
        }
    }
}