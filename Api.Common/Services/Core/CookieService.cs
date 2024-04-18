using Microsoft.AspNetCore.Http;

namespace Api.Common.Services.Core
{
    public interface ICookiesService
    {
        void SetCookie(HttpContext context, string key, string value, DateTimeOffset? expires);
        string? GetCookie(HttpContext context, string key);
        void DeleteCookie(HttpContext context, string key);
    }

    public class CookiesService : ICookiesService
    {
        public CookiesService(IHttpContextAccessor httpContextAccessor)
        {
        }

        public void SetCookie(HttpContext context, string key, string value, DateTimeOffset? expires)
        {
            var cookieOptions = new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
            };

            if (expires.HasValue) cookieOptions.Expires = expires.Value;

            context?.Response.Cookies.Append(key, value, cookieOptions);
        }

        public string? GetCookie(HttpContext context, string key)
        {
            return context?.Request.Cookies[key];
        }

        public void DeleteCookie(HttpContext context, string key)
        {
            context?.Response.Cookies.Delete(key);
        }
    }
}
