using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using System.Diagnostics;

namespace SmartTimeCVs.Web.Controllers
{
    //[Authorize]

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
		private readonly IStringLocalizer<SharedResource> _localizer;

		public HomeController(ILogger<HomeController> logger, IStringLocalizer<SharedResource> localizer)
        {
            _logger = logger;
            _localizer = localizer;
        }

        public IActionResult Index()
        {

			//string CompanyGuidID;
			//string IsCompanyRequest = "";
			//if (HttpContext.User.Identity!.IsAuthenticated)
			//{
			//    CompanyGuidID = HttpContext.User.Claims.First(c => c.Type == "CompanyGuidID").Value.ToString();
			//    IsCompanyRequest = HttpContext.User.Claims.First(c => c.Type == "IsCompanyRequest").Value.ToString();
			//}
			//else return RedirectToAction("Logout", "Account");
			//if (IsCompanyRequest == "False") return RedirectToAction("Logout", "Account");

			return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { Exception = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
