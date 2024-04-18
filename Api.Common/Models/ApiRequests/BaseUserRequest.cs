using System.ComponentModel.DataAnnotations;

namespace Api.Common.Models.ApiRequests
{
    public class BaseUserRequest
    {
        public int? Id { get; set; }
        [EmailAddress]
        public string? UserName { get; set; }
    }
}
