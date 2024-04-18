using Api.Common.Attributes;
using Api.Common.Models.ApiRequests;
using Api.Common.Models.ApiResponses;
using Api.Common.Services.Core;
using Api.Common.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Api.V1.Core
{
    [ControllerName("Správa přihlašování")]
    public class AuthenticationController : BaseController
    {
        #region Private members
        private readonly IApiAuthenticationService _authenticationService;
        private readonly ILogger _logger;
        #endregion

        #region Ctor
        public AuthenticationController(IApiAuthenticationService authenticationService, ILogger<AuthenticationController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Přihlášení uživatele
        /// </summary>
        /// <param name="authenticateRequest"></param>
        /// <response code="200">Vrátí přihlášeného uživatele a JWT token pro další komunikaci s API, včetně doby jeho vypršení. Refresh token je vracen jako cookie.</response>
        /// <response code="401">Neplatný pokus o přihlášení k API. Email, nebo heslo jsou neplatné, nebo nejsou vyplněné.</response>
        /// <response code="500">Obecná chyba serveru. Více informací je k nalezení v logu aplikace.</response>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthenticateRequest authenticateRequest)
        {
            AuthenticateResponse? response = null;
            string reasonMessage = string.Empty;
            int? statusCode = 200;

            try
            {
                response = await _authenticationService.Authenticate(authenticateRequest, GetRequestIp());
                SetTokenCookie(response.RefreshToken ?? string.Empty, response.DateExpires ?? DateTime.Now);

                response.RefreshToken = null;
                response.DateExpires = null;
            }
            catch (Exception ex)
            {
                reasonMessage = ex.Message;
                var type = ex.GetType();
                statusCode = ex.GetType() == typeof(UnauthorizedAccessException) ? 401 : 500;
            }

            if (response == null)
            {
                string errorType = statusCode == 500 ? "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1" : "https://tools.ietf.org/html/rfc7235#section-3.1";
                string errorTitle = statusCode == 500 ? "InternalServerError" : "Unauthorized";

                return Problem(type: errorType, title: errorTitle, statusCode: statusCode, detail: reasonMessage);
            }
            return Ok(response);
        }

        /// <summary>
        /// Obnovení JWT tokenu pomocí refresh tokenu. Refresh token musí být v požadavku v cookie
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Vrátí přihlášeného uživatele a JWT token pro další komunikaci s API, včetně doby jeho vypršení. Refresh token je vracen jako cookie.</response>
        /// <response code="401">Neplatný pokus o přihlášení k API. Email, nebo heslo jsou neplatné, nebo nejsou vyplněné.</response>
        /// <response code="500">Obecná chyba serveru. Více informací je k nalezení v logu aplikace.</response>
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = HttpContext.Request.Cookies[CookieNames.Refresh_Token] ?? HttpContext.Request.Cookies["refreshToken"];

            if (refreshToken == null) return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1", title: "BadRequest", statusCode: StatusCodes.Status400BadRequest, detail: "Missing refresh token in request cookie");

            try
            {

                AuthenticateResponse? response = await _authenticationService.RefreshToken(refreshToken, GetRequestIp());
                SetTokenCookie(response.RefreshToken ?? string.Empty, response.DateExpires ?? DateTime.Now);

                response.RefreshToken = null;
                response.DateExpires = null;
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            };
        }

        /// <summary>
        /// Validace JWT tokenu.
        /// </summary>
        /// <param name="token"></param>
        /// <response code="200">Vrátí AuthorizeResponse s uživateským jménem, platným JWT tokenem a refresh tokenem.</response>
        /// <response code="401">Neautorizováno, pokud v těle požadavku token chybí, nebo je token neplatný.</response>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("validate-token")]
        public async Task<IActionResult> ValidateToken(string? token)
        {
            if (string.IsNullOrEmpty(token)) return Unauthorized();
            var response = await _authenticationService.ValidateToken(token);
            if (response != null) return Ok(response);
            return Unauthorized();
        }

        /// <summary>
        /// Zrušení platnosti refresh tokenu
        /// </summary>
        /// <param name="token"></param>
        /// <response code="204">Platnost refresh tokenu byla ukončena.</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>   
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] string token)
        {
            try
            {
                await _authenticationService.RevokeToken(token, GetRequestIp());
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při zpracování požadavku na zrušení tokenu {token}: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Vrací uživatelské jméno aktuálně přihlášeného uživatele. Pro potřeby klientské aplikace
        /// </summary>
        /// <response code="200">Vrátí AuthorizeResponse s uživateským jménem, platným JWT tokenem a refresh tokenem.</response>
        /// <response code="401">Neautorizováno, pokud v těle požadavku token chybí, nebo je token neplatný.</response>
        /// <returns></returns>
        [HttpGet("get-username")]
        public async Task<IActionResult> GetUsername()
        {
            var token = Request.Headers.Authorization.First()?.Split(' ').Last();
            if (string.IsNullOrEmpty(token)) return Unauthorized();
            var response = await _authenticationService.ValidateToken(token);
            if (response != null) return Ok(response);
            return Unauthorized();
        }
        #endregion

        #region Private methods
        private void SetTokenCookie(string token, DateTime DateExpires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateExpires
            };
            Response.Cookies.Append(CookieNames.Refresh_Token, token, cookieOptions);
        }

        private string GetRequestIp()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"].ToString() ?? string.Empty;
            else
            {
                string result = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty;
                return result != null && result != "0.0.0.1" ? result : "127.0.0.1";
            }
        }
        #endregion
    }
}
