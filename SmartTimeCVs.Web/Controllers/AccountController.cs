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
            try
            {
                /** SOF Check the short link **/
                var request = HttpContext.Request;
                var fullUrl = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
                string? bsslValue = HttpContext.Request.Query["bssl"];

                if(!String.IsNullOrWhiteSpace(bsslValue))
                {
                    CompanyGuidID = SysBase.CheckShortLink_SmartTimeCVs(bsslValue);
                }
                /** EOF Check the short link **/


                /** Start the action registration to register the company guid id **/
                if (CompanyGuidID is null || PrtCode is null)
                {
                    HttpContext.SignOutAsync();
                }

                if (SysBase.PtrCode(CompanyGuidID, PrtCode))
                {
                    if (!string.IsNullOrWhiteSpace(CompanyGuidID))
                    {
                        GlobalVariablesService.CompanyId = CompanyGuidID;
                    }

                    var claims = new List<Claim>
                    {
                        //new Claim(ClaimTypes.Sid, _CompanyGuidID),
                        new(ClaimTypes.Sid, CompanyGuidID!),
                        new("CompanyGuidID", CompanyGuidID!),
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    var props = new AuthenticationProperties();
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props).Wait();
                }
                else
                {
                    HttpContext.SignOutAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                HttpContext.SignOutAsync();
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
