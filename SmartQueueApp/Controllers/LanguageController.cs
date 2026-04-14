using Microsoft.AspNetCore.Mvc;
using SmartQueueApp.Services;

namespace SmartQueueApp.Controllers
{
    public class LanguageController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Switch(string lang, string returnUrl = "/")
        {
            var validLang = lang == "hr" ? "hr" : "en";

            Response.Cookies.Append(LanguageService.CookieName, validLang,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax
                });

            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return Redirect("/");
        }
    }
}