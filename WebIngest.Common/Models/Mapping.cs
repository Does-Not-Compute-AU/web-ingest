using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebIngest.Common.Interfaces;

namespace WebIngest.Common.Models
{
    public class Mapping : DatedEntity, INamedEntity
    {
        [Key] public int Id { get; set; }

        public string Name => DataOriginName + " -> " + DataTypeName;

        [ForeignKey("DataOrigin")] public int DataOriginId { get; set; }

        public virtual DataOrigin DataOrigin { get; set; }

        [ForeignKey("DataType")] public int DataTypeId { get; set; }
        public virtual DataType DataType { get; set; }

        [Display] public string DataOriginName => DataOrigin.Name;

        [Display] public string DataTypeName => DataType.Name;

        public List<PropertyMapping> PropertyMappings { get; set; } = new();
    }
}