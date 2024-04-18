using System.ComponentModel.DataAnnotations;

namespace Api.Common.Models.DTOs.Core
{
    public class UserDTO
    {
        public int? Id { get; set; }
        [EmailAddress]
        public string? UserName { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        [Phone]
        public string? PhoneNumber { get; set; }
        public bool? EmailConfirmed { get; set; } = null;
        public bool? PhoneNumberConfirmed { get; set; } = null;
    }
}
