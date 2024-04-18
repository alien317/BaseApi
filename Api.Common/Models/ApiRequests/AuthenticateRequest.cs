using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Api.Common.Models.ApiRequests
{
    public class AuthenticateRequest
    {
        [Required]
        [EmailAddress]
        [DisplayName("Email")]
        public string Username { get; set; }

        [Required]
        [MinLength(8)]
        [DisplayName("Heslo")]
        public string Password { get; set; }
    }
}
