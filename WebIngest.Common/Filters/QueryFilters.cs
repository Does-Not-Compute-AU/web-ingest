using System;
using WebIngest.Common.Models;

namespace WebIngest.Common.Filters
{
    public static class QueryFilters
    {
        public static readonly Func<DataOrigin, bool> RequiresBackgroundServer = 
            origin =>
                origin.Name != GlobalConstants.DefaultOriginName
                && !string.IsNullOrEmpty(origin.Schedule)
                && origin.Workers > 0;
    }
}