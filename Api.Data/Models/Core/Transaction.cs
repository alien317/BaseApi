using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models.Core
{
    public class Transaction : BaseEntity
    {
        [MaxLength(50)]
        public required string Name { get; set; }

        [MaxLength(20)]
        public required string Code { get; set; }

        [MaxLength(100)]
        public string? TransactionUrl { get; set; }
        public int? Order { get; set; }
        public bool ShowInMenu { get; set; } = false;
        public int ApplicationId { get; set; }

        [ForeignKey(nameof(ApplicationId))]
        public required Application Application { get; set; }

        public ICollection<Role> Roles { get; set; } = [];
     
    }
}
