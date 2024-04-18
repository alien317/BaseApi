using Api.Common.Services.APIs;
using Api.Common.Services.Core;
using Api.Common.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Web;

namespace Api.Common.Middleware
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IAuthorizationApi _crmAuthorizationApi, ICookiesService cookiesService)
        {
            string? token = cookiesService.GetCookie(context, CookieNames.JWT_Token);

            bool accessTokenValid = false;

            try
            {
                JwtSecurityTokenHandler tokenHandler = new();
                JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);

                if (jwtToken != null && jwtToken.ValidTo.ToLocalTime() > DateTime.Now.AddMinutes(5))
                {
                    var response = await _crmAuthorizationApi.ValidateToken(token);
                    if (response.IsSuccessStatusCode)
                    {
                        accessTokenValid = true;
                    }
                }
            }
            catch
            {
                accessTokenValid = false;
            }

            if (!accessTokenValid)
            {
                try
                {
                    NameValueCollection? queryString = HttpUtility.ParseQueryString(cookiesService.GetCookie(context, CookieNames.Refresh_Token) ?? string.Empty);
                    string? refreshToken = queryString["refreshToken"];

                    if (!string.IsNullOrEmpty(refreshToken) && bool.TryParse(queryString["persist"], out bool persistent))
                    {
                        var refreshTokenCookie = new Cookie("refreshToken", refreshToken);
                        var response = await _crmAuthorizationApi.RefreshApiToken();

                        if (response.IsSuccessStatusCode)
                        {
                            JwtSecurityTokenHandler tokenHandler = new();
                            JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(response.Content.Token);
                            cookiesService.SetCookie(context, CookieNames.JWT_Token, response.Content.Token, jwtToken.ValidTo.ToLocalTime());
                            try
                            {
                                var cookieHeaders = response.Headers.Where(h => h.Key == "Set-Cookie");

                                foreach (var cookieHeader in cookieHeaders)
                                {
                                    if (SetCookieHeaderValue.TryParse(cookieHeader.Value.Single(), out SetCookieHeaderValue? responseRefreshTokenCookie))
                                    {
                                        if (responseRefreshTokenCookie.Name != "_crm.refreshToken") continue;


                                        queryString = HttpUtility.ParseQueryString(string.Empty);

                                        queryString["refreshToken"] = responseRefreshTokenCookie.Value.Value;
                                        queryString["persist"] = persistent.ToString();

                                        cookiesService.SetCookie(context, CookieNames.Refresh_Token, queryString.ToString()!, persistent ? refreshTokenCookie.Expires : null);
                                    }
                                }
                                accessTokenValid = true;
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }
                catch
                {
                    accessTokenValid = false;
                }
            }

            //if (!accessTokenValid && context.Request.Path != "/login")
            //{
            //    //context.Response.Redirect("/login");
            //    return;
            //}

            await _next(context);
        }
    }
}
