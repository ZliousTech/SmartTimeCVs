namespace SmartTimeCVs.Web.Controllers
{
    /// <summary>
    /// Controller for managing interview schedules
    /// </summary>
    public class InterviewController : Controller
    {
        private readonly IInterviewSchedulingService _schedulingService;
        private readonly ILogger<InterviewController> _logger;
        private readonly ApplicationDbContext _context;

        public InterviewController(
            IInterviewSchedulingService schedulingService,
            ILogger<InterviewController> logger,
            ApplicationDbContext context)
        {
            _schedulingService = schedulingService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Display all interview schedules
        /// </summary>
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Interview & Test Schedules";
            ViewData["Path"] = "Interview & Test Schedules";
            ViewData["Icon"] = "event";
            ViewData["ControllerName"] = "Interview";

            var schedules = await _schedulingService.GetAllSchedulesAsync(GlobalVariablesService.CompanyId);
            return View(schedules);
        }

        /// <summary>
        /// Display all test schedules
        /// </summary>
        public async Task<IActionResult> TestSchedules()
        {
            ViewData["Title"] = "Test & Schedules";
            ViewData["Path"] = "Test & Schedules";
            ViewData["Icon"] = "assignment";
            ViewData["ControllerName"] = "Interview-Test";

            var schedules = await _schedulingService.GetTestSchedulesAsync(GlobalVariablesService.CompanyId);
            return View(schedules);
        }

        /// <summary>
        /// Display interview results
        /// </summary>
        public async Task<IActionResult> InterviewResults()
        {
            ViewData["Title"] = "Interview Results";
            ViewData["Path"] = "Interview Results";
            ViewData["Icon"] = "fact_check";
            ViewData["ControllerName"] = "Interview-Result";

            var results = await _schedulingService.GetInterviewResultsAsync(GlobalVariablesService.CompanyId);
            return View(results);
        }

        /// <summary>
        /// Display test results
        /// </summary>
        public async Task<IActionResult> TestResults()
        {
            ViewData["Title"] = "Test Results";
            ViewData["Path"] = "Test Results";
            ViewData["Icon"] = "grading";
            ViewData["ControllerName"] = "Interview-TestResult";

            var results = await _schedulingService.GetTestResultsAsync(GlobalVariablesService.CompanyId);
            return View(results);
        }

        /// <summary>
        /// Display final results (consolidated)
        /// </summary>
        public async Task<IActionResult> FinalResults()
        {
            ViewData["Title"] = "Final Results";
            ViewData["Path"] = "Final Results";
            ViewData["Icon"] = "assignment_turned_in"; // Good icon for final results
            ViewData["ControllerName"] = "Interview-FinalResult"; // Custom key for highlighting if needed

            var schedules = await _schedulingService.GetFinalResultsAsync(GlobalVariablesService.CompanyId);
            return View(schedules);
        }

        /// <summary>
        /// Set interview result (AJAX POST)
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetInterviewResult([FromBody] SetResultViewModel model)
        {
            try
            {
                var schedule = await _schedulingService.GetScheduleByIdAsync(model.ScheduleId);
                if (schedule == null)
                    return Json(new { success = false, message = "Schedule not found." });

                // Update directly via DbContext
                var entity = await _context.InterviewSchedule.FindAsync(model.ScheduleId);
                if (entity == null)
                    return Json(new { success = false, message = "Schedule not found." });

                entity.InterviewResult = (InterviewResult)model.Result;
                entity.InterviewResultNote = model.Note;
                entity.LastUpdatedOn = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Interview result saved successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting interview result for schedule {ScheduleId}", model.ScheduleId);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        /// <summary>
        /// Set test result (AJAX POST)
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetTestResult([FromBody] SetResultViewModel model)
        {
            try
            {
                var entity = await _context.InterviewSchedule.FindAsync(model.ScheduleId);
                if (entity == null)
                    return Json(new { success = false, message = "Schedule not found." });

                entity.TestResult = (TestResult)model.Result;
                entity.TestResultNote = model.Note;
                entity.LastUpdatedOn = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Test result saved successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting test result for schedule {ScheduleId}", model.ScheduleId);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
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
        /// Get modal content for editing interview only (AJAX)
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
        /// Get modal content for editing test only (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetEditTestModal(int id)
        {
            var model = await _schedulingService.GetEditScheduleViewModelAsync(id);
            
            if (model == null)
                return NotFound();

            return PartialView("_EditTestOnlyModal", model);
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
        /// Update test schedule only (AJAX POST)
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateTestSchedule([FromBody] ScheduleInterviewViewModel model)
        {
            try
            {
                // Clear validation errors for interview-only fields since we're only updating test data
                ModelState.Remove("InterviewDate");
                ModelState.Remove("InterviewTime");
                ModelState.Remove("InterviewLocation");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                // Load the existing schedule to preserve interview fields
                if (!model.Id.HasValue)
                    return Json(new { success = false, message = "Schedule ID is required." });

                var existingSchedule = await _schedulingService.GetScheduleByIdAsync(model.Id.Value);
                if (existingSchedule == null)
                    return Json(new { success = false, message = "Schedule not found." });

                // Preserve interview fields, update only test fields
                model.InterviewDate = existingSchedule.InterviewDate;
                model.InterviewTime = existingSchedule.InterviewTime;
                model.InterviewLocation = existingSchedule.InterviewLocation;

                await _schedulingService.UpdateScheduleAsync(model);

                _logger.LogInformation(
                    "Test schedule {ScheduleId} updated successfully",
                    model.Id
                );

                return Json(new 
                { 
                    success = true, 
                    message = "Test details updated successfully!",
                    redirectUrl = Url.Action("TestSchedules", "Interview")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test schedule {ScheduleId}", model.Id);
                
                return Json(new 
                { 
                    success = false, 
                    message = "An error occurred while updating the test. Please try again." 
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
