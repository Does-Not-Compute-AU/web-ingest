using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using WebIngest.Common.Extensions;
using WebIngest.Common.Filters;
using WebIngest.Common.Interfaces;
using WebIngest.Common.Models.OriginConfiguration;

namespace WebIngest.Common.Models
{

    public class DataOrigin : DatedEntity, INamedEntity 
    {
        [Key] public int Id { get; set; }

        [Display, Required] public string Name { get; set; }

        [Display] public string Schedule { get; set; }

        [Required] public int Workers { get; set; }

        [Required] public OriginType OriginType { get; set; }

        public OriginTypeConfiguration OriginTypeConfiguration { get; set; } = new();
        [Required] public ContentType ContentType { get; set; }

        public ContentTypeConfiguration ContentTypeConfiguration { get; set; } = new();

        [JsonIgnore] public virtual List<Mapping> Mappings { get; set; }

        public string GetBackgroundServerMutex()
        {
            return QueryFilters.RequiresBackgroundServer(this) 
                ? Regex.Replace($"{GetSanitizedName()}-{Workers}-{Schedule.CreateMd5()}", @"\s+", "").ToLower()
                : null;
        }

        private string GetSanitizedName() => Regex.Replace(Name,@"[\.]", "_");
    }
}