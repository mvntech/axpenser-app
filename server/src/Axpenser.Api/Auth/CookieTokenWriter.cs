namespace Axpenser.Api.Auth
{
    public class CookieTokenWriter
    {
        public const string AccessTokenCookieName = "access_token";

        public static void WriteAccessTokenCookie(HttpResponse response, string jwt)
        {
            response.Cookies.Append(AccessTokenCookieName, jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });
        }

        public static void ClearAccessTokenCookie(HttpResponse response)
        {
            response.Cookies.Delete(AccessTokenCookieName);
        }
    }
}
