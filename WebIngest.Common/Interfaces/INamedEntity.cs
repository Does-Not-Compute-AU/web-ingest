using System.ComponentModel.DataAnnotations;

namespace WebIngest.Common.Interfaces
{
    public interface INamedEntity
    {
        [Display, Required] public int Id { get; }
        [Display, Required] public string Name { get; }
    }
}