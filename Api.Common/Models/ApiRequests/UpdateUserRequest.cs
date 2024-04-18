using Api.Common.Models.DTOs.Core;

namespace Api.Common.Models.ApiRequests
{
    public class UpdateUserRequest : BaseUserRequest
    {
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
        public UserDTO? User { get; set; }
    }
}
