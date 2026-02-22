using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTimeCVs.Web.Core.Dtos;
using SmartTimeCVs.Web.Core.Services;
using SmartTimeCVs.Web.Core.ViewModels;
using SmartTimeCVs.Web.Data;

namespace SmartTimeCVs.Web.Controllers
{
    [AllowAnonymous]
    public class CandidatePortalController : Controller
    {
        private readonly IJobOfferService _jobOfferService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CandidatePortalController> _logger;

        public CandidatePortalController(IJobOfferService jobOfferService, ApplicationDbContext context, ILogger<CandidatePortalController> logger)
        {
            _jobOfferService = jobOfferService;
            _context = context;
            _logger = logger;
        }

        private async Task LoadCompanyDataAsync(int appId)
        {
            try
            {
                var application = await _context.JobApplication.FindAsync(appId);
                if (application != null && !string.IsNullOrEmpty(application.CompanyId))
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetFromJsonAsync<SmartTimeCompanyDTO>
                                    ($"https://smarttimeapi.zlioustech.com/api/Company/GetCompanyLogoHomePageText/{application.CompanyId}");

                    var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
                    ViewBag.CompanyName = currentCulture.Contains("en") ? response?.Data?.CompanyNameEn : response?.Data?.CompanyNameNative;
                    ViewBag.HomePageHtml = currentCulture.Contains("en") ? response?.Data?.HomePageTextEn : response?.Data?.HomePageTextNative;
                    ViewBag.CompanyLogo = response?.Data?.CompanyLogo;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load company data for app {AppId}", appId);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Login(int appId)
        {
            if (appId <= 0) return NotFound();
            await LoadCompanyDataAsync(appId);
            return View(new CandidateLoginViewModel { AppId = appId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(CandidateLoginViewModel model)
        {
            await LoadCompanyDataAsync(model.AppId);
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var offer = await _jobOfferService.GetOfferByApplicationIdAsync(model.AppId);
                if (offer == null)
                {
                    ModelState.AddModelError("", "Job offer not found or has been revoked.");
                    return View(model);
                }

                bool isValid = await _jobOfferService.ValidateCandidateMobileAsync(offer.Id, model.MobileNumber);
                if (isValid)
                {
                    var offerViewModel = await _jobOfferService.GetOfferViewModelAsync(model.AppId);
                    if (offerViewModel == null) return NotFound();

                    ViewBag.MobileNumber = model.MobileNumber;
                    return View("ViewOffer", offerViewModel);
                }

                ModelState.AddModelError("", "The mobile number provided does not match our records.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during candidate login for appId {AppId}", model.AppId);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitResponse(int offerId, string mobileNumber, bool isAccepted)
        {
            if (offerId <= 0 || string.IsNullOrWhiteSpace(mobileNumber))
                return BadRequest("Invalid request data.");

            var offer = await _jobOfferService.GetOfferViewModelAsync(offerId); // we need appId for layout

            bool isValid = await _jobOfferService.ValidateCandidateMobileAsync(offerId, mobileNumber);
            if (!isValid)
                return Unauthorized("Mobile number verification failed.");

            // Load layout data if possible
            var actualOffer = await _context.JobOffer.FindAsync(offerId);
            if (actualOffer != null) await LoadCompanyDataAsync(actualOffer.JobApplicationId);

            var success = await _jobOfferService.RespondToOfferAsync(offerId, isAccepted);
            
            if (success)
            {
                ViewBag.IsAccepted = isAccepted;
                return View("OfferResponded");
            }
            
            return StatusCode(500, "An error occurred while saving your response. Please contact HR.");
        }
    }
}
