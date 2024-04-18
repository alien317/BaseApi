using Api.Common.Models.ApiRequests;
using Api.Common.Models.ApiResponses;
using Api.Common.Models.DTOs.Core;
using Refit;

namespace Api.Common.Services.APIs
{
    public interface IMainApi
    {
        [Get("/get-user")]
        Task<ApiResponse<UserDTO>> GetUser(GetUserRequest request);

        [Get("/transactions-list")]
        Task<ApiResponse<List<TransactionDTO>>> GetTransactions();

        [Get("/get-username")]
        Task<ApiResponse<AuthenticateResponse>> GetUsername();
    }
}
