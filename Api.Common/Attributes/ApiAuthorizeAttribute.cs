using Api.Data.Models;
using Api.Data.Models.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Api.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiAuthorizeAttribute : ServiceFilterAttribute
    {
        public ApiAuthorizeAttribute()
            : base(typeof(ApiAuthorizeFilter))
        {

        }
    }

    public class ApiAuthorizeFilter : IAuthorizationFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public ApiAuthorizeFilter(UserManager<ApplicationUser> userManager, RoleManager<Role> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
            if (allowAnonymous)
                return;

            if (_userManager.Users.Count() == 0 && context.ActionDescriptor.EndpointMetadata.OfType<HttpPutAttribute>().Any(md => md.Template == "create-user"))
                return;

            var user = (ApplicationUser)context.HttpContext.Items["User"];

            if (user == null)
            {
                context.Result = new UnauthorizedObjectResult(new { Type = "https://tools.ietf.org/html/rfc7235#section-3.1", Title = "Unauthorized", StatusCode = 401, Detail = "Unauthorized access" });
                return;
            }
            var roleNames = _userManager.GetRolesAsync(user).Result;
            var roles = _roleManager.Roles.Where(r => roleNames.Contains(r.Name!)).ToList();
            if (!roles.Any(r => r.Transactions.Any(t => t.TransactionUrl == context.HttpContext.Request.Path.Value)))
                context.Result = new UnauthorizedObjectResult(new { Type = "https://tools.ietf.org/html/rfc7235#section-3.1", Title = "Unauthorized", StatusCode = 401, Detail = "Nemáte oprávnění pro tuto transakci" });
        }
    }
}
