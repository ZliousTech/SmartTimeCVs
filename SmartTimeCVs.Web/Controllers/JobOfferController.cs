using Microsoft.AspNetCore.Mvc;
using SmartTimeCVs.Web.Core.Services;
using SmartTimeCVs.Web.Core.ViewModels;
using SmartTimeCVs.Web.Core.Enums;

namespace SmartTimeCVs.Web.Controllers
{
    public class JobOfferController : Controller
    {
        private readonly IJobOfferService _jobOfferService;
        private readonly ILogger<JobOfferController> _logger;

        public JobOfferController(
            IJobOfferService jobOfferService, 
            ILogger<JobOfferController> logger)
        {
            _jobOfferService = jobOfferService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetOfferModal(int jobApplicationId)
        {
            var model = await _jobOfferService.GetOfferViewModelAsync(jobApplicationId);
            if (model == null) return NotFound();
            return PartialView("_JobOfferModal", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveOffer([FromBody] JobOfferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            try
            {
                var offer = await _jobOfferService.SaveOfferAsync(model);
                return Json(new { success = true, message = "Offer saved successfully!", offerId = offer.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving offer");
                return Json(new { success = false, message = "Error saving offer." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendOffer(int id, NotificationType notificationType)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _jobOfferService.SendOfferAsync(id, notificationType, baseUrl);
            if (result)
                return Json(new { success = true, message = "Offer sent successfully!" });
            else
                return Json(new { success = false, message = "Failed to send offer." });
        }

        [HttpPost]
        public async Task<IActionResult> RespondToOffer(int id, bool accepted)
        {
            var result = await _jobOfferService.RespondToOfferAsync(id, accepted);
            if (result)
                return Json(new { success = true, message = accepted ? "Offer accepted!" : "Offer rejected." });
            else
                return Json(new { success = false, message = "Failed to update offer status." });
        }

        [HttpGet]
        public async Task<IActionResult> PrintOffer(int appId)
        {
            var model = await _jobOfferService.GetOfferViewModelAsync(appId);
            if (model == null) return NotFound();
            return View("PrintOffer", model);
        }
    }
}
