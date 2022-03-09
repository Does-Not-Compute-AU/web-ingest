using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WebIngest.Common.Extensions;
using WebIngest.Common.Interfaces;

namespace WebIngest.Common.Models
{
    public class DataType : DatedEntity, INamedEntity
    {
        [Key] public int Id { get; set; }

        [Display, Required] public string Name { get; set; }

        [Display, Required] public List<DataTypeProperty> Properties { get; set; } = new();

        public PropertyType PropertyTypeOf(string propertyName) =>
            Properties
                .First(x => x.PropertyName == propertyName)
                .PropertyType;

        public bool Equals(DataType other)
        {
            return Id == other.Id 
                   && Name == other.Name 
                   && Properties.ToJson() == other.Properties.ToJson();
        }
    }

    public class DataTypeProperty
    {
        public DataTypeProperty()
        {
            PropertyType = PropertyType.TEXT;
        }

        public string PropertyName { get; set; }
        public PropertyType PropertyType { get; set; }

        public static PropertyType DetectPropertyType(object dataObject)
        {
            return PropertyType.TEXT;
        }
    }

    public enum PropertyType
    {
        TEXT,
        LONGTEXT,
        MONEY,
        NUMBER
    }
}