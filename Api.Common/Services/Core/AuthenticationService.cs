using Api.Data.Models.Core;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;


namespace Api.Common.Services.Core
{
    public class AuthenticationService
    {

        public JwtSecurityToken? JwtToken { get; private set; }
        public string? Username { get; private set; }
        public string? RefreshToken { get; set; }

        public void SetCredentials(string jwtToken, string username, string refreshToken)
        {
            JwtToken = new JwtSecurityToken(jwtToken);
            Username = username;
            RefreshToken = refreshToken;
        }

        public void ClearCredentials()
        {
            JwtToken = null;
            Username = null;
            RefreshToken = null;
        }

        public bool IsTokenExpired()
        {
            if (JwtToken == null)
                return true;

            return JwtToken.ValidTo < DateTime.UtcNow;
        }

    }
}