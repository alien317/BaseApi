using Api.Common.Models.ApiRequests;
using Api.Common.Models.ApiResponses;
using Api.Common.Models.Configuration;
using Api.Common.Utils;
using Api.Data.Models;
using Api.Data.Models.Core;
using Api.Data.Repositories.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Web;

namespace Api.Common.Services.Core
{
    public interface IApiAuthenticationService
    {
        Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress);
        Task<AuthenticateResponse> RefreshToken(string token, string ipAddress);
        Task<AuthenticateResponse?> ValidateToken(string token);
        Task RevokeToken(string token, string ipAddress);
    }

    public class ApiAuthenticationService : IApiAuthenticationService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtUtils _jwtUtils;
        private readonly IApplicationUserRepository _userRepository;
        private readonly JwtConfiguration _config;
        private readonly ILogger _logger;

        public ApiAuthenticationService(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,
        IUserStore<ApplicationUser> userStore, IJwtUtils jwtUtils, IApplicationUserRepository userRepository,
        IOptions<JwtConfiguration> options, ILogger<ApiAuthenticationService> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtUtils = jwtUtils;
            _userRepository = userRepository;
            _config = options.Value;
            _logger = logger;
        }

        public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var user = await _userManager.FindByNameAsync(model.Username);

            if (user == null)
                throw new UnauthorizedAccessException("Neplatný pokus o přihlášení.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
                throw new UnauthorizedAccessException("Neplatný pokus o přihlášení.");

            string jwtToken = string.Empty;
            RefreshToken refreshToken = new();

            try
            {
                jwtToken = _jwtUtils.GenerateJwtToken(user);
                refreshToken = await _jwtUtils.GenerateRefreshToken(user, ipAddress);

                await _userRepository.RemoveOldRefreshTokens(user, _config.RefreshToken?.RefreshTokenTTL ?? 0);
                user.RefreshTokens.Add(refreshToken);
                await _userRepository.Update(user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Došlo k chybě při pokusu o přihlášení z IP {ipAddress} : {ex}");
                throw new Exception("Došlo k chybě při požadavku o přihlášení");
            }

            return new AuthenticateResponse()
            {
                Username = user.UserName,
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                DateExpires = refreshToken.DateExpires
            };
        }

        public async Task<AuthenticateResponse?> ValidateToken(string token)
        {
            string? username = _jwtUtils.ValidateJwtToken(token);
            ApplicationUser? user = await _userManager.FindByEmailAsync(username ?? string.Empty);
            if (user != null)
                return new AuthenticateResponse()
                {
                    Username = user.UserName,
                    Token = token,
                };
            return null;
        }

        public async Task<AuthenticateResponse> RefreshToken(string token, string ipAddress)
        {
            var user = await _userRepository.FindByRefreshToken(token);
            if (user == null) throw new NullReferenceException("Uživatel pro tento token nebyl nalezen.");

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);
            if (refreshToken.IsRevoked)
            {
                await _jwtUtils.RevokeDescendantRefreshTokens(refreshToken, user, ipAddress, $"Attempted reuse of revoked ancestor token: {token}");
                await _userRepository.Update(user);
            }

            if (!refreshToken.IsActive) throw new Exception("Neplatný token");

            var newRefreshToken = await _jwtUtils.RotateRefreshToken(user, refreshToken, ipAddress);
            user.RefreshTokens.Add(newRefreshToken);

            await _userRepository.RemoveOldRefreshTokens(user, _config.RefreshToken?.RefreshTokenTTL ?? 0);
            await _userRepository.Update(user);

            var jwtToken = _jwtUtils.GenerateJwtToken(user);

            return new AuthenticateResponse()
            {
                Username = user.UserName,
                Token = jwtToken,
                RefreshToken = newRefreshToken.Token,
                DateExpires = newRefreshToken.DateExpires
            };
        }

        public async Task RevokeToken(string token, string ipAddress)
        {
            var user = await _userRepository.FindByRefreshToken(token) ?? await _userRepository.FindByRefreshToken(HttpUtility.UrlDecode(token));
            if (user == null) throw new NullReferenceException("Uživatel pro tento token nebyl nalezen.");

            var refreshToken = user.RefreshTokens.Single(t => t.Token == token || t.Token == HttpUtility.UrlDecode(token));
            if (!refreshToken.IsActive) throw new Exception("Neplatný token");

            await _jwtUtils.RevokeRefreshToken(refreshToken, ipAddress, "Revoked without replacement");
            await _userRepository.Update(user);
        }
    }
}
