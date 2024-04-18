using AutoMapper;
using Api.Common.Attributes;
using Api.Common.Models.ApiRequests;
using Api.Common.Models.ApiRequests;
using Api.Common.Models.DTOs.Core;
using Api.Common.Services.Core;
using Api.Data.Models;
using Api.Data.Models.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace Api.Controllers.Api.V1.Core
{
    [ControllerName("Správa rolí a uživatelů")]
    public class UsersAndRolesManagementController : BaseController
    {
        private readonly IUsersAndRolesManagementService _usersAndRolesManagementService;
        private readonly ITransactionsService _transactionsService;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public UsersAndRolesManagementController(IUsersAndRolesManagementService usersAndRolesManagementService, ITransactionsService transactionsService,
            IMapper mapper, ILogger<UsersAndRolesManagementController> logger)
        {
            _usersAndRolesManagementService = usersAndRolesManagementService;
            _transactionsService = transactionsService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Vytvoření nového uživatele. Bez autorizace lze vytvořit pouze prvního uživatele
        /// </summary>
        /// <param name="authenticateRequest"></param>
        /// <response code="204">Požadavek byl proveden úspěšně, odpověď nemá žádné tělo.</response>
        /// <response code="400">Chyba požadavku.</response>
        /// <response code="401">Neautorizováno. Mimo prvního uživatele, lze vytvářet uživatele pouze s přihlášením.</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("create-user"), ActionName("Přidání nového uživatele")]
        public async Task<IActionResult> CreateUser([FromBody] AuthenticateRequest authenticateRequest)
        {
            if (string.IsNullOrEmpty(authenticateRequest.Username)) return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1", title: "BadRequest", statusCode: StatusCodes.Status400BadRequest, detail: "Pole email je povinné.");

            if (string.IsNullOrEmpty(authenticateRequest.Password)) return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1", title: "BadRequest", statusCode: StatusCodes.Status400BadRequest, detail: "Pole heslo je povinné.");

            if (!ModelState.IsValid)
            {
                string errorMessage = string.Empty;
                foreach (var error in ModelState)
                {
                    foreach (var message in error.Value.Errors) errorMessage += $"{message.ErrorMessage};";
                }
                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1", title: "BadRequest", statusCode: StatusCodes.Status400BadRequest, detail: errorMessage.ToString());
            }

            try
            {
                await _usersAndRolesManagementService.CreateUser(authenticateRequest.Username, authenticateRequest.Password);
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Můj účet
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Vrátí informace o aktuálně přihlášeném uživateli</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        [HttpGet("my-user"), ActionName("Můj účet")]
        public IActionResult GetLoggedUser()
        {
            try
            {
                return Ok(_mapper.Map<UserDTO>(HttpContext.Items["User"]));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při načítání uživatele: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Informace o uživateli
        /// </summary>
        /// <param name="request">Požadavek na uživatele - Možno zadat Id, nebo uživatelské jméno</param>
        /// <returns></returns>
        /// <response code="200">Vrátí informace o uživateli.</response>
        /// <response code="401">Neautorizováno. Mimo prvního uživatele, lze vytvářet uživatele pouze s přihlášením.</response>
        /// <response code="404">Uživatel se zadaným Id, nebo emailem nebyl nalezen.</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        [HttpGet("get-user"), ActionName("Informace o uživateli")]
        public async Task<IActionResult> GetUser(GetUserRequest request)
        {
            try
            {
                var user = await _usersAndRolesManagementService.GetUser(request);
                if (user != null) return Ok(user);

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4", title: "NotFound", statusCode: StatusCodes.Status404NotFound, detail: "Uživatel s tímto emailem, nebo Id nebyl nalezen.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při načítání uživatele: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Upravit můj účet
        /// </summary>
        /// <param name="request"></param>
        /// <response code="200">Vrátí upravenou instanci uživatele.</response>
        /// <response code="401">Chybí oprávnění pro úpravu uživatele</response>
        /// <response code="404">Uživatel se zadaným Id, nebo emailem nebyl nalezen.</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>
        [HttpPost("update-my-user")]
        public async Task<IActionResult> UpdateLoggedUser([FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = HttpContext.Items["User"] as ApplicationUser;

                if (request.User?.Id != _mapper.Map<UserDTO>(user).Id) return Unauthorized();

                return Ok(await _usersAndRolesManagementService.UpdateUser(request));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při úpravě uživatele: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Upraví informace o uživateli
        /// </summary>
        /// <param name="request"></param>
        /// <response code="200">Vrátí upravenou instanci uživatele.</response>
        /// <response code="401">Chybí oprávnění pro úpravu uživatele</response>
        /// <response code="404">Uživatel se zadaným Id, nebo emailem nebyl nalezen.</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>
        [HttpPost("update-user")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _usersAndRolesManagementService.UpdateUser(request);
                if (user != null) return Ok(user);

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4", title: "NotFound", statusCode: StatusCodes.Status404NotFound, detail: "Uživatel s tímto emailem, nebo Id nebyl nalezen.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při úpravě uživatele: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Můj seznam rolí
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Vrátí seznam rolí aktuálně přihlášeném uživateli</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        [HttpGet("my-roles"), ActionName("Můj seznam rolí")]
        public async Task<IActionResult> GetLoggedUserRoles()
        {
            try
            {
                var user = HttpContext.Items["User"] as ApplicationUser;
                return Ok(await _usersAndRolesManagementService.GetUserRoles(user));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při načítání uživatele: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Odstraní uživatele
        /// </summary>
        /// <param name="request"></param>
        /// <response code="204">Uživatel byl odstraněn z databáze uživatelů</response>
        /// <response code="400">Nebyl zadán email uživatele pro odstranění</response>
        /// <response code="401">Chybí oprávnění pro odstranění uživatele</response>
        /// <response code="404">Uživatel se zadaným Id, nebo emailem nebyl nalezen.</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete("delete-user"), ActionName("Smazání uživatele")]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteUserRequest request)
        {
            if (request.UserName == null) return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1", title: "BadRequest", statusCode: StatusCodes.Status400BadRequest, detail: "Eamil uživatele pro odstranění musí být zadán.");
            try
            {
                await _usersAndRolesManagementService.DeleteUser(request.UserName);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při odstraňování uživatele: {ex}");

                if (ex.GetType() == typeof(ArgumentException)) return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4", title: "NotFound", statusCode: StatusCodes.Status404NotFound, detail: ex.Message);

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Stažení seznamu uživatelů
        /// </summary>
        /// <response code="200">Vrátí seznam všech uživatelů</response>
        /// <response code="401">Chybí oprávnění ke stažení seznamu uživatelů</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>
        [HttpGet("users-list"), ActionName("Seznam uživatelů")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                return Ok(await _usersAndRolesManagementService.GetAllUsers());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při stahování seznamu uživatelů: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Vytvoření nové role
        /// </summary>
        /// <param name="createRoleRequest"></param>
        /// <response code="204">Požadavek byl proveden úspěšně, odpověď nemá žádné tělo.</response>
        /// <response code="400">Chyba požadavku.</response>
        /// <response code="401">Chybí oprávnění k vytvoření role.</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("create-role"), ActionName("Vytvoření nové uživatelské role")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest createRoleRequest)
        {
            if (!ModelState.IsValid)
            {
                string errorMessage = string.Empty;
                foreach (var error in ModelState)
                {
                    foreach (var message in error.Value.Errors) errorMessage += $"{message.ErrorMessage};";
                }
                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1", title: "BadRequest", statusCode: StatusCodes.Status400BadRequest, detail: errorMessage.ToString());
            }

            try
            {
                await _usersAndRolesManagementService.CreateRole(createRoleRequest.RoleName);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při vytváření role: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        /// <summary>
        /// Stažení seznamu rolí
        /// </summary>
        /// <response code="200">Vrátí seznam všech rolí</response>
        /// <response code="401">Chybí oprávnění ke stažení seznamu rolí</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>
        [HttpGet("roles-list"), ActionName("Seznam rolí")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                return Ok(await _usersAndRolesManagementService.GetAllRoles());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při stahování rolí: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }

        // <summary>
        // Vrátí seznam všech rolí pro zadaného uživatele uživatele
        // </summary>
        // <returns></returns>
        //[HttpGet("user-roles")]
        //public async Task<IActionResult> GetUserRoles(string username)
        //{
        //    var user = await _usersAndRolesManagementService.getu
        //    var roles = await _usersAndRolesManagementService.GetUserRoles(user);
        //    return Ok();
        //}

        //public async Task<IActionResult> AssignTransaction([FromBody]AssignTransactionRequest request)
        //{
        //    return NoContent();
        //}
    }
}
