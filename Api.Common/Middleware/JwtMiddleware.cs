using Api.Common.Models.Configuration;
using Api.Common.Utils;
using Api.Data.Models.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Api.Common.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, UserManager<ApplicationUser> userManager, IJwtUtils jwtUtils)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var userName = jwtUtils.ValidateJwtToken(token);
            if (userName != null)
            {
                context.Items["User"] = await userManager.FindByNameAsync(userName);
            }

            await _next(context);
        }
    }
}
