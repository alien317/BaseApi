using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models.Core
{
    public class Application : BaseEntity
    {
        [MaxLength(50)]
        public required string Name { get; set; }

        [MaxLength(20)]
        public required string Code { get; set; }

        public int Order { get; set; }

        public int Type { get; set; } = 0;

        public int ModuleId { get; set; }

        [ForeignKey(nameof(ModuleId))]
        public required Module Module { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = [];
    }
}
