using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace SmartTimeCVs.Web.Controllers
{
    public class LanguageSwitcher : Controller
    {
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(culture))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true
                    });
            }

            return LocalRedirect(returnUrl ?? "/");
        }
    }
}
