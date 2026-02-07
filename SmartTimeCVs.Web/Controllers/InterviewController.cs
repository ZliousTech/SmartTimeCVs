namespace SmartTimeCVs.Web.Controllers
{
    /// <summary>
    /// Controller for managing interview schedules
    /// </summary>
    public class InterviewController : Controller
    {
        private readonly IInterviewSchedulingService _schedulingService;
        private readonly ILogger<InterviewController> _logger;

        public InterviewController(
            IInterviewSchedulingService schedulingService,
            ILogger<InterviewController> logger)
        {
            _schedulingService = schedulingService;
            _logger = logger;
        }

        /// <summary>
        /// Display all interview schedules
        /// </summary>
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Interview Schedules";
            ViewData["Path"] = "Interview Schedules";
            ViewData["Icon"] = "event";
            ViewData["ControllerName"] = "Interview";

            var schedules = await _schedulingService.GetAllSchedulesAsync(GlobalVariablesService.CompanyId);
            return View(schedules);
        }

        /// <summary>
        /// Get modal content for scheduling interview (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetScheduleModal(int jobApplicationId)
        {
            var model = await _schedulingService.GetScheduleViewModelAsync(jobApplicationId);
            
            if (model == null)
                return NotFound();

            return PartialView("_ScheduleInterviewModal", model);
        }

        /// <summary>
        /// Schedule an interview (AJAX POST)
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ScheduleInterview([FromBody] ScheduleInterviewViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var schedule = await _schedulingService.ScheduleInterviewAsync(model);

                _logger.LogInformation(
                    "Interview scheduled successfully for JobApplication {JobApplicationId}",
                    model.JobApplicationId
                );

                return Json(new 
                { 
                    success = true, 
                    message = "Interview scheduled successfully!",
                    scheduleId = schedule.Id,
                    redirectUrl = Url.Action("Index", "Interview")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling interview for JobApplication {JobApplicationId}", model.JobApplicationId);
                
                return Json(new 
                { 
                    success = false, 
                    message = "An error occurred while scheduling the interview. Please try again." 
                });
            }
        }

        /// <summary>
        /// Get modal content for editing interview (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEditScheduleModal(int id)
        {
            var model = await _schedulingService.GetEditScheduleViewModelAsync(id);
            
            if (model == null)
                return NotFound();

            return PartialView("_EditScheduleModal", model);
        }

        /// <summary>
        /// Update an interview schedule (AJAX POST)
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateSchedule([FromBody] ScheduleInterviewViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                await _schedulingService.UpdateScheduleAsync(model);

                _logger.LogInformation(
                    "Interview schedule {ScheduleId} updated successfully",
                    model.Id
                );

                return Json(new 
                { 
                    success = true, 
                    message = "Interview details updated successfully!",
                    redirectUrl = Url.Action("Index", "Interview")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating interview schedule {ScheduleId}", model.Id);
                
                return Json(new 
                { 
                    success = false, 
                    message = "An error occurred while updating the interview. Please try again." 
                });
            }
        }

        /// <summary>
        /// View details of a specific schedule
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var schedule = await _schedulingService.GetScheduleByIdAsync(id);
            
            if (schedule == null)
                return NotFound();

            return View(schedule);
        }
    }
}
