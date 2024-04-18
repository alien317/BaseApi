namespace Api.Common.Models.ApiResponses
{
    public class AuthenticateResponse
    {
        public string Username { get; set; }
        public string Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? DateExpires { get; set; }
    }
}
