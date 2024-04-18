using System.ComponentModel.DataAnnotations;

namespace Api.Data.Models.Core
{
    public class Module : BaseEntity
    {
        [MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(20)]
        public string Code { get; set; }

        public virtual List<Application> Applications { get; set; } = [];    
    }
}
