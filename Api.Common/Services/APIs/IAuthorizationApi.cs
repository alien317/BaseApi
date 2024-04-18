using Refit;
using System.Threading.Tasks;
using Api.Common.Models.ApiResponses;
using Api.Common.Models.ApiRequests;

namespace Api.Common.Services.APIs
{
    public interface IAuthorizationApi
    {
        [Post("/login")]
        Task<ApiResponse<AuthenticateResponse>> Authenticate(AuthenticateRequest request);

        [Post("/validate-token")]
        Task<ApiResponse<AuthenticateResponse>> ValidateToken(string? token);

        [Post("/refresh-token")]
        Task<ApiResponse<AuthenticateResponse>> RefreshApiToken();
    }
}
