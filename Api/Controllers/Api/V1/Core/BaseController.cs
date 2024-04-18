using Api.Common.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Api.V1.Core
{
    [Route("api/v1")]
    [ApiAuthorize]
    public class BaseController : ControllerBase
    {
    }
}
