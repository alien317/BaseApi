using Api.Common.Models.Configuration;
using Api.Data.Models.Core;
using Api.Data.Repositories.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Api.Common.Utils
{
    public interface IJwtUtils
    {
        string GenerateJwtToken(ApplicationUser user);
        string? ValidateJwtToken(string? token);
        Task<RefreshToken> GenerateRefreshToken(ApplicationUser user, string ipAddress);
        Task<RefreshToken> RotateRefreshToken(ApplicationUser user, RefreshToken refreshToken, string ipAddress);
        Task RevokeRefreshToken(RefreshToken token, string ipAddress, string? reason = null, string? replacedByToken = null);
        Task RevokeDescendantRefreshTokens(RefreshToken refreshToken, ApplicationUser user, string ipAddress, string reason);
    }

    public class JwtUtils : IJwtUtils
    {
        private readonly RefreshTokenConfiguration? _tokenSettings;
        private readonly IApplicationUserRepository _userRepository;

        public JwtUtils(IOptions<JwtConfiguration> appSettings, IApplicationUserRepository userRepository)
        {
            _tokenSettings = appSettings.Value.RefreshToken;
            _userRepository = userRepository;
        }

        public string GenerateJwtToken(ApplicationUser user)
        {
            JwtSecurityTokenHandler tokenHandler = new();
            byte[] key = Encoding.ASCII.GetBytes(_tokenSettings.Secret);
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(new[] { new Claim("name", user.UserName) }),
                Expires = DateTime.Now.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public string? ValidateJwtToken(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            JwtSecurityTokenHandler tokenHandler = new();
            byte[] key = Encoding.ASCII.GetBytes(_tokenSettings.Secret);
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;
                string userName = jwtToken.Claims.First(x => x.Type == "name").Value;

                return userName;
            }
            catch
            {
                return null;
            }
        }

        public async Task<RefreshToken> GenerateRefreshToken(ApplicationUser user, string ipAddress)
        {
            RefreshToken refreshToken = new()
            {
                UserId = user.Id,
                Token = await CreateUniqueRefreshToken(),
                DateExpires = DateTime.Now.AddDays(7),
                DateCreated = DateTime.Now,
                CreatedByIp = ipAddress
            };

            return refreshToken;
        }

        public async Task<RefreshToken> RotateRefreshToken(ApplicationUser user, RefreshToken refreshToken, string ipAddress)
        {
            RefreshToken newRefreshToken = await GenerateRefreshToken(user, ipAddress);
            await RevokeRefreshToken(refreshToken, ipAddress, "Nahrazeno novým tokenem", newRefreshToken.Token);
            return newRefreshToken;
        }

        public Task RevokeRefreshToken(RefreshToken token, string ipAddress, string? reason = null, string? replacedByToken = null)
        {
            token.DateRevoked = DateTime.Now;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason;
            token.ReplacedByToken = replacedByToken;

            return Task.CompletedTask;
        }

        public async Task RevokeDescendantRefreshTokens(RefreshToken refreshToken, ApplicationUser user, string ipAddress, string reason)
        {
            if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
            {
                RefreshToken? childToken = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken.ReplacedByToken);

                if (childToken != null)
                {
                    if (childToken.IsActive) await RevokeRefreshToken(childToken, ipAddress, reason);
                    await RevokeDescendantRefreshTokens(childToken, user, ipAddress, reason);
                }
            }
        }

        private async Task<string> CreateUniqueRefreshToken()
        {
            string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            bool tokenIsUnique = await _userRepository.FindByRefreshToken(token) == null;

            if (!tokenIsUnique)
                return await CreateUniqueRefreshToken();

            return token;
        }
    }
}
