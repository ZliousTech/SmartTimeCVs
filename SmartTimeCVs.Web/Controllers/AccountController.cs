using Common.Base;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SmartTimeCVs.Web.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Login()
        {
            return Redirect("https://smarttime.zlioustech.com/web/login");
        }

        public IActionResult Logout()
        {
            HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult Register([FromQuery] string CompanyGuidID, [FromQuery] string PrtCode)
        {
            //https://localhost:7061/Account/Register/?bssl=sGGf3jxh (Light House)
            //https://localhost:7061/Account/Register/?bssl=PjVCjr4Q (Royal Eagle)
            //https://localhost:7061/Account/Register/?bssl=31l37ziA (Nefertari)

            bool IsCompanyRequest = true;
            bool UseHomePageText = true;
            bool IsAllowBiographiesFeature = true;

            try
            {
                /** SOF Check the short link **/
                var request = HttpContext.Request;
                var fullUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
                string? bsslValue = HttpContext.Request.Query["bssl"];

                if (!String.IsNullOrWhiteSpace(bsslValue))
                {
                    string[] CompanyGuidIDAndUseHomePageText = SysBase.CheckShortLink_SmartTimeCVs(bsslValue).Split('|');
                    CompanyGuidID = CompanyGuidIDAndUseHomePageText[0];
                    UseHomePageText =  bool.Parse(CompanyGuidIDAndUseHomePageText[1]);
                    IsAllowBiographiesFeature = bool.Parse(CompanyGuidIDAndUseHomePageText[2]);
                    IsCompanyRequest = false;
                }
                /** EOF Check the short link **/

                /** Start the action registration to register the company guid id **/
                if (CompanyGuidID is null || !IsAllowBiographiesFeature) return RedirectToAction("Logout");

                if (SysBase.PtrCode(CompanyGuidID, PrtCode))
                {
                    if (!string.IsNullOrWhiteSpace(CompanyGuidID))
                    {
                        GlobalVariablesService.CompanyId = CompanyGuidID;
                    }

                    var claims = new List<Claim>
                    {
                        new("CompanyGuidID", CompanyGuidID!),
                        new("IsCompanyRequest", IsCompanyRequest.ToString()),
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    var props = new AuthenticationProperties();
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props).Wait();
                }
                else
                {
                    return RedirectToAction("Logout");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return RedirectToAction("Logout");
            }

            if(UseHomePageText)
                return IsCompanyRequest ? RedirectToAction("Index", "Home") : RedirectToAction("Index", "Biography");
            else
                return IsCompanyRequest ? RedirectToAction("Index", "Home") : RedirectToAction("Index", "SBiography");
        }
    }
}
