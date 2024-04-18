using Api.Common.Attributes;
using Api.Common.Services.Core;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Api.V1.Core
{
    [ControllerName("Správa transakcí")]
    public class TransactionsController : BaseController
    {
        private readonly ITransactionsService _transactionsService;
        private readonly ILogger _logger;

        public TransactionsController(ITransactionsService transactionsService, ILogger<TransactionsController> logger)
        {
            _transactionsService = transactionsService;
            _logger = logger;
        }

        /// <summary>
        /// Stažení všech transakcí
        /// </summary>
        /// <response code="200">Vrátí seznam všech transakcí</response>
        /// <response code="401">Chybí oprávnění ke stažení seznamu rolí</response>
        /// <response code="500">Při požadavku došlo k chybě na serveru.</response>
        /// <returns></returns>
        [HttpGet("transactions-list")]
        [ActionName("Stažení seznamu všech transakcí")]
        public async Task<IActionResult> GetTransactions()
        {
            try
            {
                return Ok(await _transactionsService.GetAllTransactions());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Chyba při stahování transakcí: {ex}");

                return Problem(type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1", title: "InternalServerError", statusCode: StatusCodes.Status500InternalServerError, detail: "Error on server.");
            }
        }
    }
}
