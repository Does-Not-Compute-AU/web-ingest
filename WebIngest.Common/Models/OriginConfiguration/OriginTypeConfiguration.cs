using System;
using System.Collections.Generic;
using WebIngest.Common.Models.OriginConfiguration.Types;

namespace WebIngest.Common.Models.OriginConfiguration
{
    public class OriginTypeConfiguration
    {
        public HttpConfiguration HttpConfiguration { get; set; } = new();
        public SiteMapCrawlerConfiguration SiteMapCrawlerConfiguration { get; set; } = new();
        public ScriptedConfiguration ScriptedConfiguration { get; set; } = new();
        public IDictionary<string, object> PassThroughVars { get; set; } = new Dictionary<string, object>();

        public OriginType GetConfigurationType()
        {
            if (HttpConfiguration != null)
                return OriginType.HTTP;
            if (ScriptedConfiguration != null)
                return OriginType.Scripted;
            if (SiteMapCrawlerConfiguration != null)
                return OriginType.SiteMapCrawler;

            throw new NotImplementedException();
        }
    }
}