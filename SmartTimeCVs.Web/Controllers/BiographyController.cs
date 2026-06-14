using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using SmartTimeCVs.Web.Core.Dtos;
using SmartTimeCVs.Web.Core.Enums;
using Common;
using Common.Base;

namespace SmartTimeCVs.Web.Controllers
{
    //[Authorize]

    public class BiographyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<SharedResource> _localizer;

        private readonly List<string> _allowedExtensions = new() { ".jpg", ".jpeg", ".png" };
        private readonly List<string> _allowedAttachmentExtensions = new() { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };
        private const int _maxAllowedSize = 5242880; // 5 MB.

        public BiographyController(ApplicationDbContext context, IMapper mapper, IWebHostEnvironment webHostEnvironment, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            string CompanyGuidID;
            string IsCompanyRequest = "";
            if (HttpContext.User.Identity!.IsAuthenticated)
            {
                CompanyGuidID = HttpContext.User.Claims.First(c => c.Type == "CompanyGuidID").Value.ToString();
                IsCompanyRequest = HttpContext.User.Claims.First(c => c.Type == "IsCompanyRequest").Value.ToString();
            }
            else return RedirectToAction("Logout", "Account");
            if (IsCompanyRequest == "True") return RedirectToAction("Logout", "Account");

            using var httpClient = new HttpClient();
            var response = await httpClient.GetFromJsonAsync<SmartTimeCompanyDTO>
                            ($"https://smarttimeapi.zlioustech.com/api/Company/GetCompanyLogoHomePageText/{CompanyGuidID}");

            var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
            ViewBag.CompanyName = currentCulture.Contains("en") ? response?.Data?.CompanyNameEn : response?.Data?.CompanyNameNative;
            ViewBag.HomePageHtml = currentCulture.Contains("en") ? response?.Data?.HomePageTextEn : response?.Data?.HomePageTextNative;
            ViewBag.CompanyLogo = response?.Data?.CompanyLogo;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomer(string mobileNumber)
        {
            try
            {
                var customer = await _context.JobApplication
                        .Include(j => j.JobOffer)
                        .FirstOrDefaultAsync(j =>
                            j.CompanyId == GlobalVariablesService.CompanyId &&
                            !j.IsFromCompanySetup &&
                            j.MobileNumber.Equals(mobileNumber));

                if (customer is null)
                {
                    return RedirectToAction(nameof(Create), new { mobileNumber = mobileNumber });
                }
                else if (customer.JobOffer != null && customer.JobOffer.Status == JobOfferStatus.Sent)
                {
                    // Candidate has a pending Job Offer → redirect to view it
                    return RedirectToAction("ViewOfferFromBiography", "CandidatePortal", new { appId = customer.Id });
                }
                else
                {
                    return RedirectToAction(nameof(Edit), new { id = customer.Id });
                }
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStep(int step, JobApplicationViewModel model)
        {
            if (step < 1 || step > 6)
                return JsonError("Invalid step.", "validation", 400);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                ModelState.Clear();
                await HydrateModelIdFromExistingDraftAsync(model);
                ValidateSaveStep(step, model);

                if (!ModelState.IsValid)
                    return JsonError(GetFirstModelError() ?? "Please check the required fields.", "validation", 400);

                var duplicateError = await GetDuplicateFieldErrorAsync(model, step == 1);
                if (duplicateError is not null)
                    return JsonError(duplicateError, "validation", 400);

                var jobApplication = await ResolveDraftApplicationAsync(model, step);
                if (jobApplication is null)
                {
                    if (!string.IsNullOrWhiteSpace(model.MobileNumber))
                    {
                        var hasSubmittedApplication = await _context.JobApplication.AnyAsync(j =>
                            j.CompanyId == GlobalVariablesService.CompanyId &&
                            !j.IsFromCompanySetup &&
                            j.MobileNumber == model.MobileNumber &&
                            !j.IsDraft);

                        if (hasSubmittedApplication)
                            return JsonError("An application with this mobile number already exists.", "validation", 400);
                    }

                    return JsonError("Please complete the first step before continuing.", "validation", 400);
                }

                switch (step)
                {
                    case 1:
                        await ApplyStep1Async(jobApplication, model);
                        break;
                    case 2:
                        await ApplyStep2Async(jobApplication, model);
                        break;
                    case 3:
                        ApplyStep3(jobApplication, model);
                        break;
                    case 4:
                        ApplyStep4(jobApplication, model);
                        break;
                    case 5:
                        await ApplyStep5Async(jobApplication, model);
                        break;
                    case 6:
                        await ApplyStep6Async(jobApplication, model);
                        break;
                }

                if (!ModelState.IsValid)
                    return JsonError(GetFirstModelError() ?? "Please check the required fields.", "validation", 400);

                jobApplication.IsDraft = true;
                jobApplication.LastCompletedStep = step;
                jobApplication.LastUpdatedOn = DateTime.Now;

                if (jobApplication.Id == 0)
                    await _context.JobApplication.AddAsync(jobApplication);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, id = jobApplication.Id, message = Messages.Saved });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return JsonError("Server error. Please try again in a moment.", "server", 500);
            }
        }

        public IActionResult Create(bool isFromJobApplicationView = false, string mobileNumber = "")
        {
            try
            {
                HandleSidebarsViewAndReturnToController(isFromJobApplicationView);

                var initJobApplicationWithMobileNumber = !string.IsNullOrWhiteSpace(mobileNumber) ? new JobApplicationViewModel { MobileNumber = mobileNumber } : null;

                ViewBag.ResumeStep = 0;
                ViewBag.EnableStepSave = true;
                return View("Form", PopulateViewModel(initJobApplicationWithMobileNumber));
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobApplicationViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                Console.WriteLine(">>> CREATE POST - Method entered");

                await HydrateModelIdFromExistingDraftAsync(model);
                if (model.Id > 0)
                    return await Edit(model);

                if (!ModelState.IsValid)
                {
                    foreach (var entry in ModelState.Where(e => e.Value!.Errors.Any()))
                    {
                        foreach (var err in entry.Value!.Errors)
                        {
                            Console.WriteLine($">>> VALIDATION ERROR - Field: [{entry.Key}] Error: [{err.ErrorMessage}]");
                        }
                    }
                    return BadRequest(new { success = false, errorType = "validation", message = "Please check the required fields." });
                }

                Console.WriteLine(">>> CREATE POST - ModelState is VALID, proceeding...");

                #region Process ImageFile.

                if (model.ImageFile != null)
                {
                    var imageFileName = await ProcessFileAsync(model.ImageFile, "profileImages", "ImageFile");
                    if (!ModelState.IsValid)
                        return JsonValidationErrorFromModelState();
                    model.ImageUrl = imageFileName;
                }

                #endregion Process ImageFile.

                #region Process CV AttachmentFile.

                if (model.AttachmentFile != null)
                {
                    var attachmentFileName = await ProcessFileAsync(model.AttachmentFile, "cvAttachments", "AttachmentFiles", true);
                    if (!ModelState.IsValid)
                        return JsonValidationErrorFromModelState();
                    model.AttachmentUrl = attachmentFileName;
                }

                #endregion Process CV AttachmentFile.

                #region Process Work Experience Attachments.

                foreach (var workExperience in model.WorkExperiences)
                {
                    var workExperienceAttachment = workExperience.AttachmentFile;
                    if (workExperienceAttachment != null)
                    {
                        var attachmentFileName = await ProcessFileAsync(workExperienceAttachment, "workExperienceAttachments", "WorkExperienceAttachmentFile", true);
                        if (!ModelState.IsValid)
                            return JsonValidationErrorFromModelState();
                        workExperience.AttachmentUrl = attachmentFileName;
                    }
                }

                #endregion Process Work Experience Attachments.

                #region Process University Attachments.

                foreach (var university in model.Universities)
                {
                    var universityAttachment = university.AttachmentFile;
                    if (universityAttachment != null)
                    {
                        var attachmentFileName = await ProcessFileAsync(universityAttachment, "universityAttachments", "UniversityAttachmentFile", true);
                        if (!ModelState.IsValid)
                            return JsonValidationErrorFromModelState();
                        university.AttachmentUrl = attachmentFileName;
                    }
                }

                #endregion Process University Attachments.

                #region Process Attachments.

                foreach (var attachment in model.AttachmentFiles)
                {
                    var attachmentFile = attachment.AttachmentFile;
                    if (attachmentFile != null)
                    {
                        var attachmentFileName = await ProcessFileAsync(attachmentFile, "attachments", "AttachmentFiles", true);
                        if (!ModelState.IsValid)
                            return JsonValidationErrorFromModelState();
                        attachment.AttachmentUrl = attachmentFileName;
                    }
                }

                #endregion Process Attachments.

                // Map ViewModel to Entity
                var jobApplication = _mapper.Map<JobApplication>(model);
                jobApplication.CompanyId = GlobalVariablesService.CompanyId;
                jobApplication.CreatedOn = DateTime.Now;
                jobApplication.LastUpdatedOn = DateTime.Now;
                jobApplication.IsDraft = false;
                jobApplication.LastCompletedStep = 7;
                jobApplication.CandidateStatus = CandidateStatus.Applied;

                // Map and link Universites
                var universites = _mapper.Map<List<University>>(model.Universities);
                jobApplication.Univesity = universites;

                // Map and link Courses
                var courses = _mapper.Map<List<Course>>(model.Courses);
                jobApplication.Course = courses;

                // Map and link WorkExperiences
                var workExperiences = _mapper.Map<List<WorkExperience>>(model.WorkExperiences);
                jobApplication.WorkExperience = workExperiences;

                // Map and link Attachments
                var attachments = _mapper.Map<List<AttachmentFile>>(model.AttachmentFiles);
                jobApplication.AttachmentFiles = attachments;

                // Add and save
                await _context.JobApplication.AddAsync(jobApplication);
                Console.WriteLine(">>> CREATE POST - About to SaveChanges...");
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                Console.WriteLine(">>> CREATE POST - SAVED SUCCESSFULLY!");

                return Json(new { success = true, message = Messages.Saved });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($">>> CREATE POST - EXCEPTION: {ex.Message}");
                Console.WriteLine($">>> CREATE POST - STACK TRACE: {ex.StackTrace}");
                return StatusCode(500, new { success = false, errorType = "server", message = "Server error. Please try again in a moment." });
            }
        }

        public async Task<IActionResult> Edit(int id, bool isFromJobApplicationView = false)
        {
            try
            {
                HandleSidebarsViewAndReturnToController(isFromJobApplicationView);

                var jobApp = await _context.JobApplication.FindAsync(id);

                if (jobApp is null)
                    return NotFound();

                if (jobApp.IsFromCompanySetup)
                    return NotFound();

                var realtedUniversites = await _context.University.Where(u => u.JobApplicationId == id).ToListAsync();
                var realtedCourses = await _context.Course.Where(c => c.JobApplicationId == id).ToListAsync();
                var realtedWorkExperiences = await _context.WorkExperience.Where(e => e.JobApplicationId == id).ToListAsync();
                var realtedAttachments = await _context.AttachmentFile.Where(e => e.JobApplicationId == id).ToListAsync();

                var viewModel = _mapper.Map<JobApplicationViewModel>(jobApp);

                if (realtedUniversites is not null)
                    viewModel.Universities = _mapper.Map<List<UniversityViewModel>>(realtedUniversites);

                if (realtedCourses is not null)
                    viewModel.Courses = _mapper.Map<List<CourseViewModel>>(realtedCourses);

                if (realtedWorkExperiences is not null)
                    viewModel.WorkExperiences = _mapper.Map<List<WorkExperienceViewModel>>(realtedWorkExperiences);

                if (realtedAttachments is not null)
                    viewModel.AttachmentFiles = _mapper.Map<List<AttachmentFileViewModel>>(realtedAttachments);

                ViewBag.ResumeStep = jobApp.IsDraft && jobApp.LastCompletedStep > 0
                    ? Math.Min(jobApp.LastCompletedStep, 6)
                    : 0;
                ViewBag.EnableStepSave = jobApp.IsDraft;

                return View("Form", PopulateViewModel(viewModel));
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobApplicationViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, errorType = "validation", message = "Please check the required fields." });
                }

                var jobApplication = await _context.JobApplication
                    .Include(j => j.Univesity)
                    .Include(j => j.Course)
                    .Include(j => j.WorkExperience)
                    .Include(j => j.AttachmentFiles)
                    .FirstOrDefaultAsync(j => j.Id == model.Id);

                if (jobApplication == null)
                    return NotFound();

                if (jobApplication.IsFromCompanySetup)
                    return NotFound();

                model.CompanyId = GlobalVariablesService.CompanyId;

                #region Handle image file.

                if (model.ImageFile != null)
                {
                    var imageFileName = await ProcessFileAsync(model.ImageFile, "profileImages", "ImageFile");
                    if (!ModelState.IsValid)
                        return JsonValidationErrorFromModelState();

                    model.ImageUrl = imageFileName;
                }
                else
                {
                    model.ImageUrl = jobApplication.ImageUrl;
                }

                #endregion Handle image file.

                #region Handle CV attachment file.

                if (model.AttachmentFile != null)
                {
                    var attachmentFileName = await ProcessFileAsync(model.AttachmentFile, "cvAttachments", "AttachmentFiles", true);
                    if (!ModelState.IsValid)
                        return JsonValidationErrorFromModelState();

                    model.AttachmentUrl = attachmentFileName;
                }
                else
                {
                    model.AttachmentUrl = jobApplication.AttachmentUrl;
                }

                #endregion Handle CV attachment file.

                #region Process Work Experience Attachments.

                var workExperienceList = model.WorkExperiences.ToList();
                var originalWorkExperienceList = jobApplication.WorkExperience.ToList();

                for (int workExperienceIndex = 0; workExperienceIndex < workExperienceList.Count; workExperienceIndex++)
                {
                    var workExperienceAttachment = workExperienceList[workExperienceIndex].AttachmentFile;

                    if (workExperienceAttachment != null)
                    {
                        var attachmentFileName = await ProcessFileAsync(workExperienceAttachment, "workExperienceAttachments", "WorkExperienceAttachmentFile", true);

                        if (!ModelState.IsValid)
                            return JsonValidationErrorFromModelState();

                        workExperienceList[workExperienceIndex].AttachmentUrl = attachmentFileName;
                    }
                    else if (workExperienceIndex < originalWorkExperienceList.Count)
                    {
                        workExperienceList[workExperienceIndex].AttachmentUrl = originalWorkExperienceList[workExperienceIndex].AttachmentUrl;
                    }
                }

                model.WorkExperiences = workExperienceList;

                #endregion Process Work Experience Attachments.

                #region Process University Attachments.

                var universityList = model.Universities.ToList();
                var originalUniversityList = jobApplication.Univesity.ToList();

                for (int universityIndex = 0; universityIndex < universityList.Count; universityIndex++)
                {
                    var universityAttachment = universityList[universityIndex].AttachmentFile;

                    if (universityAttachment != null)
                    {
                        var attachmentFileName = await ProcessFileAsync(universityAttachment, "universityAttachments", "UniversityAttachmentFile", true);

                        if (!ModelState.IsValid)
                            return JsonValidationErrorFromModelState();

                        universityList[universityIndex].AttachmentUrl = attachmentFileName;
                    }
                    else if (universityIndex < originalUniversityList.Count)
                    {
                        universityList[universityIndex].AttachmentUrl = originalUniversityList[universityIndex].AttachmentUrl;
                    }
                }

                model.Universities = universityList;

                #endregion Process University Attachments.

                #region Process Attachments.

                var attachmentList = model.AttachmentFiles.ToList();
                var originalAttachmentList = jobApplication.AttachmentFiles.ToList();

                for (int attachmentIndex = 0; attachmentIndex < attachmentList.Count; attachmentIndex++)
                {
                    var attachment = attachmentList[attachmentIndex].AttachmentFile;

                    if (attachment != null)
                    {
                        var attachmentFileName = await ProcessFileAsync(attachment, "attachments", "AttachmentFiles", true);

                        if (!ModelState.IsValid)
                            return JsonValidationErrorFromModelState();

                        attachmentList[attachmentIndex].AttachmentUrl = attachmentFileName;
                    }
                    else if (attachmentIndex < originalAttachmentList.Count)
                    {
                        attachmentList[attachmentIndex].AttachmentUrl = originalAttachmentList[attachmentIndex].AttachmentUrl;
                    }
                }

                model.AttachmentFiles = attachmentList;

                #endregion Process Attachments.

                // Update job application data using AutoMapper
                _mapper.Map(model, jobApplication);
                jobApplication.LastUpdatedOn = DateTime.Now;

                if (jobApplication.IsDraft)
                {
                    jobApplication.IsDraft = false;
                    jobApplication.LastCompletedStep = 7;
                    jobApplication.CandidateStatus = CandidateStatus.Applied;
                }

                #region Prepare universities for update.

                // Remove existing work experiences from DB
                var existingUniversites = await _context.University
                    .Where(w => w.JobApplicationId == jobApplication.Id)
                    .ToListAsync();

                _context.University.RemoveRange(existingUniversites);

                // Add new work experiences
                var newUniversites = _mapper.Map<List<University>>(model.Universities);
                newUniversites.ForEach(u => { u.JobApplicationId = jobApplication.Id; u.LastUpdatedOn = DateTime.Now; });

                jobApplication.Univesity = newUniversites;

                #endregion Prepare universities for update.

                #region Prepare courses for update.

                // Remove existing work experiences from DB
                var existingCourses = await _context.Course
                    .Where(w => w.JobApplicationId == jobApplication.Id)
                    .ToListAsync();

                _context.Course.RemoveRange(existingCourses);

                // Add new work experiences
                var newCourses = _mapper.Map<List<Course>>(model.Courses);
                newCourses.ForEach(c => { c.JobApplicationId = jobApplication.Id; c.LastUpdatedOn = DateTime.Now; });

                jobApplication.Course = newCourses;

                #endregion Prepare courses for update.

                #region Prepare work experiences for update.

                // Remove existing work experiences from DB
                var existingWorkExperiences = await _context.WorkExperience
                    .Where(w => w.JobApplicationId == jobApplication.Id)
                    .ToListAsync();

                _context.WorkExperience.RemoveRange(existingWorkExperiences);

                // Add new work experiences
                var newWorkExperiences = _mapper.Map<List<WorkExperience>>(model.WorkExperiences);
                newWorkExperiences.ForEach(e => { e.JobApplicationId = jobApplication.Id; e.LastUpdatedOn = DateTime.Now; });

                jobApplication.WorkExperience = newWorkExperiences;

                #endregion Prepare work experiences for update.

                #region Prepare attachments for update.

                // Remove existing attachments from DB
                var existingAttachments = await _context.AttachmentFile
                    .Where(a => a.JobApplicationId == jobApplication.Id)
                    .ToListAsync();

                _context.AttachmentFile.RemoveRange(existingAttachments);

                // Add new attachments
                var newAttachments = _mapper.Map<List<AttachmentFile>>(model.AttachmentFiles);
                newAttachments.ForEach(e => { e.JobApplicationId = jobApplication.Id; e.LastUpdatedOn = DateTime.Now; });

                jobApplication.AttachmentFiles = newAttachments;

                #endregion Prepare attachments for update.

                _context.Update(jobApplication);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = Messages.Updated });

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, errorType = "server", message = "Server error. Please try again in a moment." });
            }
        }

        public async Task<IActionResult> AllowName(JobApplicationViewModel model)
        {
            var item = await _context.JobApplication
                .SingleOrDefaultAsync(j => j.FullName.Trim().ToLower() == model.FullName.Trim().ToLower() &&
                                           j.CompanyId == GlobalVariablesService.CompanyId);

            var isAllowed = item is null || item.Id.Equals(model.Id);

            return Json(isAllowed);
        }

        public async Task<IActionResult> AllowEmail(JobApplicationViewModel model)
        {
            var item = await _context.JobApplication
                .SingleOrDefaultAsync(j => j.Email.Trim().ToLower() == model.Email.Trim().ToLower() &&
                                           j.CompanyId == GlobalVariablesService.CompanyId);

            var isAllowed = item is null || item.Id.Equals(model.Id);

            return Json(isAllowed);
        }

        public async Task<IActionResult> AllowNationalID(JobApplicationViewModel model)
        {
            var item = await _context.JobApplication
                .SingleOrDefaultAsync(j => j.NationalID.Trim().ToLower() == model.NationalID.Trim().ToLower());

            var isAllowed = item is null || item.Id.Equals(model.Id);

            return Json(isAllowed);
        }

        [HttpGet]
        public IActionResult GetJobTitlesByCategory(string categoryGuidID)
        {
            var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
            var titles = SysBase.GetJobTitles(categoryGuidID);
            var list = titles.Select(title => new
            {
                value = title.JobTitleGuidID!.ToString(),
                text = currentCulture.Contains("en") ? title.JobTitleNameEn! : title.JobTitleNameNative!
            });

            return Json(list);
        }

        #region Private Area.

        private void HandleSidebarsViewAndReturnToController(bool isFromJobApplicationView)
        {
            if (isFromJobApplicationView)
            {
                ViewData["HideSidebars"] = false;
                ViewData["ControllerName"] = "JobApplication";
            }
            else
            {
                ViewData["HideSidebars"] = true;
                ViewData["ControllerName"] = "Biography";
            }
        }

        private JobApplicationViewModel PopulateViewModel(JobApplicationViewModel? model = null)
        {
            ViewBag.GenderList = InitEnumList<GenderTypeEnum>();
            ViewBag.MaritalStatusList = InitEnumList<MaritalStatusTypeEnum>();
            ViewBag.NationalityList = InitNationalityList();
            ViewBag.ApplyingForList = InitApplyingForList();
            //ViewBag.JobTitleList = InitJobTitlesList();

            var levels = InitEnumList<LevelsEnum>();
            ViewBag.EnglishLevelList = levels;
            ViewBag.OtherLanguageLevelList = InitEnumList<LevelsEnum>(false);
            ViewBag.ComputerSkillsLevelList = levels;

            return model ?? new JobApplicationViewModel();
        }

        private List<SelectListItem> InitEnumList<E>(bool isReqiured = true)
            where E : Enum
        {
            List<SelectListItem> enumValues = new List<SelectListItem>();

            if (!isReqiured)
            {
                enumValues.Add(new SelectListItem
                {
                    Text = "--Please Select--",
                    Value = "",
                    Selected = true
                });
            }

            foreach (var value in Enum.GetValues(typeof(E)))
            {
                enumValues.Add(new SelectListItem
                {
                    Text = value.ToString(),
                    Value = ((int)value).ToString()
                });
            }

            return enumValues;
        }

        private List<SelectListItem> InitNationalityList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Afghanistan", Text = "Afghanistan" },
                new SelectListItem { Value = "Albania", Text = "Albania" },
                new SelectListItem { Value = "Algeria", Text = "Algeria" },
                new SelectListItem { Value = "Andorra", Text = "Andorra" },
                new SelectListItem { Value = "Angola", Text = "Angola" },
                new SelectListItem { Value = "Argentina", Text = "Argentina" },
                new SelectListItem { Value = "Armenia", Text = "Armenia" },
                new SelectListItem { Value = "Australia", Text = "Australia" },
                new SelectListItem { Value = "Austria", Text = "Austria" },
                new SelectListItem { Value = "Azerbaijan", Text = "Azerbaijan" },
                new SelectListItem { Value = "Bahamas", Text = "Bahamas" },
                new SelectListItem { Value = "Bahrain", Text = "Bahrain" },
                new SelectListItem { Value = "Bangladesh", Text = "Bangladesh" },
                new SelectListItem { Value = "Belarus", Text = "Belarus" },
                new SelectListItem { Value = "Belgium", Text = "Belgium" },
                new SelectListItem { Value = "Belize", Text = "Belize" },
                new SelectListItem { Value = "Benin", Text = "Benin" },
                new SelectListItem { Value = "Bhutan", Text = "Bhutan" },
                new SelectListItem { Value = "Bolivia", Text = "Bolivia" },
                new SelectListItem { Value = "Bosnia and Herzegovina", Text = "Bosnia and Herzegovina" },
                new SelectListItem { Value = "Botswana", Text = "Botswana" },
                new SelectListItem { Value = "Brazil", Text = "Brazil" },
                new SelectListItem { Value = "Brunei", Text = "Brunei" },
                new SelectListItem { Value = "Bulgaria", Text = "Bulgaria" },
                new SelectListItem { Value = "Burkina Faso", Text = "Burkina Faso" },
                new SelectListItem { Value = "Burundi", Text = "Burundi" },
                new SelectListItem { Value = "Cambodia", Text = "Cambodia" },
                new SelectListItem { Value = "Cameroon", Text = "Cameroon" },
                new SelectListItem { Value = "Canada", Text = "Canada" },
                new SelectListItem { Value = "Central African Republic", Text = "Central African Republic" },
                new SelectListItem { Value = "Chad", Text = "Chad" },
                new SelectListItem { Value = "Chile", Text = "Chile" },
                new SelectListItem { Value = "China", Text = "China" },
                new SelectListItem { Value = "Colombia", Text = "Colombia" },
                new SelectListItem { Value = "Comoros", Text = "Comoros" },
                new SelectListItem { Value = "Congo (DRC)", Text = "Congo (DRC)" },
                new SelectListItem { Value = "Congo (Republic)", Text = "Congo (Republic)" },
                new SelectListItem { Value = "Costa Rica", Text = "Costa Rica" },
                new SelectListItem { Value = "Croatia", Text = "Croatia" },
                new SelectListItem { Value = "Cuba", Text = "Cuba" },
                new SelectListItem { Value = "Cyprus", Text = "Cyprus" },
                new SelectListItem { Value = "Czech Republic", Text = "Czech Republic" },
                new SelectListItem { Value = "Denmark", Text = "Denmark" },
                new SelectListItem { Value = "Djibouti", Text = "Djibouti" },
                new SelectListItem { Value = "Dominica", Text = "Dominica" },
                new SelectListItem { Value = "Dominican Republic", Text = "Dominican Republic" },
                new SelectListItem { Value = "Ecuador", Text = "Ecuador" },
                new SelectListItem { Value = "Egypt", Text = "Egypt", Selected = true },
                new SelectListItem { Value = "El Salvador", Text = "El Salvador" },
                new SelectListItem { Value = "Equatorial Guinea", Text = "Equatorial Guinea" },
                new SelectListItem { Value = "Eritrea", Text = "Eritrea" },
                new SelectListItem { Value = "Estonia", Text = "Estonia" },
                new SelectListItem { Value = "Eswatini", Text = "Eswatini" },
                new SelectListItem { Value = "Ethiopia", Text = "Ethiopia" },
                new SelectListItem { Value = "Fiji", Text = "Fiji" },
                new SelectListItem { Value = "Finland", Text = "Finland" },
                new SelectListItem { Value = "France", Text = "France" },
                new SelectListItem { Value = "Gabon", Text = "Gabon" },
                new SelectListItem { Value = "Gambia", Text = "Gambia" },
                new SelectListItem { Value = "Georgia", Text = "Georgia" },
                new SelectListItem { Value = "Germany", Text = "Germany" },
                new SelectListItem { Value = "Ghana", Text = "Ghana" },
                new SelectListItem { Value = "Greece", Text = "Greece" },
                new SelectListItem { Value = "Grenada", Text = "Grenada" },
                new SelectListItem { Value = "Guatemala", Text = "Guatemala" },
                new SelectListItem { Value = "Guinea", Text = "Guinea" },
                new SelectListItem { Value = "Guinea-Bissau", Text = "Guinea-Bissau" },
                new SelectListItem { Value = "Guyana", Text = "Guyana" },
                new SelectListItem { Value = "Haiti", Text = "Haiti" },
                new SelectListItem { Value = "Honduras", Text = "Honduras" },
                new SelectListItem { Value = "Hungary", Text = "Hungary" },
                new SelectListItem { Value = "Iceland", Text = "Iceland" },
                new SelectListItem { Value = "India", Text = "India" },
                new SelectListItem { Value = "Indonesia", Text = "Indonesia" },
                new SelectListItem { Value = "Iran", Text = "Iran" },
                new SelectListItem { Value = "Iraq", Text = "Iraq" },
                new SelectListItem { Value = "Ireland", Text = "Ireland" },
                new SelectListItem { Value = "Israel", Text = "Israel" },
                new SelectListItem { Value = "Italy", Text = "Italy" },
                new SelectListItem { Value = "Jamaica", Text = "Jamaica" },
                new SelectListItem { Value = "Japan", Text = "Japan" },
                new SelectListItem { Value = "Jordan", Text = "Jordan" },
                new SelectListItem { Value = "Kazakhstan", Text = "Kazakhstan" },
                new SelectListItem { Value = "Kenya", Text = "Kenya" },
                new SelectListItem { Value = "Kiribati", Text = "Kiribati" },
                new SelectListItem { Value = "Kuwait", Text = "Kuwait" },
                new SelectListItem { Value = "Kyrgyzstan", Text = "Kyrgyzstan" },
                new SelectListItem { Value = "Laos", Text = "Laos" },
                new SelectListItem { Value = "Latvia", Text = "Latvia" },
                new SelectListItem { Value = "Lebanon", Text = "Lebanon" },
                new SelectListItem { Value = "Lesotho", Text = "Lesotho" },
                new SelectListItem { Value = "Liberia", Text = "Liberia" },
                new SelectListItem { Value = "Libya", Text = "Libya" },
                new SelectListItem { Value = "Liechtenstein", Text = "Liechtenstein" },
                new SelectListItem { Value = "Lithuania", Text = "Lithuania" },
                new SelectListItem { Value = "Luxembourg", Text = "Luxembourg" },
                new SelectListItem { Value = "Madagascar", Text = "Madagascar" },
                new SelectListItem { Value = "Malawi", Text = "Malawi" },
                new SelectListItem { Value = "Malaysia", Text = "Malaysia" },
                new SelectListItem { Value = "Maldives", Text = "Maldives" },
                new SelectListItem { Value = "Mali", Text = "Mali" },
                new SelectListItem { Value = "Malta", Text = "Malta" },
                new SelectListItem { Value = "Marshall Islands", Text = "Marshall Islands" },
                new SelectListItem { Value = "Mauritania", Text = "Mauritania" },
                new SelectListItem { Value = "Mauritius", Text = "Mauritius" },
                new SelectListItem { Value = "Mexico", Text = "Mexico" },
                new SelectListItem { Value = "Micronesia", Text = "Micronesia" },
                new SelectListItem { Value = "Moldova", Text = "Moldova" },
                new SelectListItem { Value = "Monaco", Text = "Monaco" },
                new SelectListItem { Value = "Mongolia", Text = "Mongolia" },
                new SelectListItem { Value = "Montenegro", Text = "Montenegro" },
                new SelectListItem { Value = "Morocco", Text = "Morocco" },
                new SelectListItem { Value = "Mozambique", Text = "Mozambique" },
                new SelectListItem { Value = "Myanmar", Text = "Myanmar" },
                new SelectListItem { Value = "Namibia", Text = "Namibia" },
                new SelectListItem { Value = "Nauru", Text = "Nauru" },
                new SelectListItem { Value = "Nepal", Text = "Nepal" },
                new SelectListItem { Value = "Netherlands", Text = "Netherlands" },
                new SelectListItem { Value = "New Zealand", Text = "New Zealand" },
                new SelectListItem { Value = "Nicaragua", Text = "Nicaragua" },
                new SelectListItem { Value = "Niger", Text = "Niger" },
                new SelectListItem { Value = "Nigeria", Text = "Nigeria" },
                new SelectListItem { Value = "North Korea", Text = "North Korea" },
                new SelectListItem { Value = "North Macedonia", Text = "North Macedonia" },
                new SelectListItem { Value = "Norway", Text = "Norway" },
                new SelectListItem { Value = "Oman", Text = "Oman" },
                new SelectListItem { Value = "Pakistan", Text = "Pakistan" },
                new SelectListItem { Value = "Palau", Text = "Palau" },
                new SelectListItem { Value = "Palestine", Text = "Palestine" },
                new SelectListItem { Value = "Panama", Text = "Panama" },
                new SelectListItem { Value = "Papua New Guinea", Text = "Papua New Guinea" },
                new SelectListItem { Value = "Paraguay", Text = "Paraguay" },
                new SelectListItem { Value = "Peru", Text = "Peru" },
                new SelectListItem { Value = "Philippines", Text = "Philippines" },
                new SelectListItem { Value = "Poland", Text = "Poland" },
                new SelectListItem { Value = "Portugal", Text = "Portugal" },
                new SelectListItem { Value = "Qatar", Text = "Qatar" },
                new SelectListItem { Value = "Romania", Text = "Romania" },
                new SelectListItem { Value = "Russia", Text = "Russia" },
                new SelectListItem { Value = "Rwanda", Text = "Rwanda" },
                new SelectListItem { Value = "Saint Kitts and Nevis", Text = "Saint Kitts and Nevis" },
                new SelectListItem { Value = "Saint Lucia", Text = "Saint Lucia" },
                new SelectListItem { Value = "Saint Vincent and the Grenadines", Text = "Saint Vincent and the Grenadines" },
                new SelectListItem { Value = "Samoa", Text = "Samoa" },
                new SelectListItem { Value = "San Marino", Text = "San Marino" },
                new SelectListItem { Value = "Sao Tome and Principe", Text = "Sao Tome and Principe" },
                new SelectListItem { Value = "Saudi Arabia", Text = "Saudi Arabia" },
                new SelectListItem { Value = "Senegal", Text = "Senegal" },
                new SelectListItem { Value = "Serbia", Text = "Serbia" },
                new SelectListItem { Value = "Seychelles", Text = "Seychelles" },
                new SelectListItem { Value = "Sierra Leone", Text = "Sierra Leone" },
                new SelectListItem { Value = "Singapore", Text = "Singapore" },
                new SelectListItem { Value = "Slovakia", Text = "Slovakia" },
                new SelectListItem { Value = "Slovenia", Text = "Slovenia" },
                new SelectListItem { Value = "Solomon Islands", Text = "Solomon Islands" },
                new SelectListItem { Value = "Somalia", Text = "Somalia" },
                new SelectListItem { Value = "South Africa", Text = "South Africa" },
                new SelectListItem { Value = "South Korea", Text = "South Korea" },
                new SelectListItem { Value = "South Sudan", Text = "South Sudan" },
                new SelectListItem { Value = "Spain", Text = "Spain" },
                new SelectListItem { Value = "Sri Lanka", Text = "Sri Lanka" },
                new SelectListItem { Value = "Sudan", Text = "Sudan" },
                new SelectListItem { Value = "Suriname", Text = "Suriname" },
                new SelectListItem { Value = "Sweden", Text = "Sweden" },
                new SelectListItem { Value = "Switzerland", Text = "Switzerland" },
                new SelectListItem { Value = "Syria", Text = "Syria" },
                new SelectListItem { Value = "Taiwan", Text = "Taiwan" },
                new SelectListItem { Value = "Tajikistan", Text = "Tajikistan" },
                new SelectListItem { Value = "Tanzania", Text = "Tanzania" },
                new SelectListItem { Value = "Thailand", Text = "Thailand" },
                new SelectListItem { Value = "Timor-Leste", Text = "Timor-Leste" },
                new SelectListItem { Value = "Togo", Text = "Togo" },
                new SelectListItem { Value = "Tonga", Text = "Tonga" },
                new SelectListItem { Value = "Trinidad and Tobago", Text = "Trinidad and Tobago" },
                new SelectListItem { Value = "Tunisia", Text = "Tunisia" },
                new SelectListItem { Value = "Turkey", Text = "Turkey" },
                new SelectListItem { Value = "Turkmenistan", Text = "Turkmenistan" },
                new SelectListItem { Value = "Tuvalu", Text = "Tuvalu" },
                new SelectListItem { Value = "Uganda", Text = "Uganda" },
                new SelectListItem { Value = "Ukraine", Text = "Ukraine" },
                new SelectListItem { Value = "United Arab Emirates", Text = "United Arab Emirates" },
                new SelectListItem { Value = "United Kingdom", Text = "United Kingdom" },
                new SelectListItem { Value = "United States", Text = "United States" },
                new SelectListItem { Value = "Uruguay", Text = "Uruguay" },
                new SelectListItem { Value = "Uzbekistan", Text = "Uzbekistan" },
                new SelectListItem { Value = "Vanuatu", Text = "Vanuatu" },
                new SelectListItem { Value = "Vatican City", Text = "Vatican City" },
                new SelectListItem { Value = "Venezuela", Text = "Venezuela" },
                new SelectListItem { Value = "Vietnam", Text = "Vietnam" },
                new SelectListItem { Value = "Yemen", Text = "Yemen" },
                new SelectListItem { Value = "Zambia", Text = "Zambia" },
                new SelectListItem { Value = "Zimbabwe", Text = "Zimbabwe" }
            };
        }

        private List<SelectListItem> InitApplyingForList()
        {
            var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
            string CompanyGuidID = GlobalVariablesService.CompanyId;

            var categories = SysBase.GetJobCategories(CompanyGuidID);
            var list = new List<SelectListItem>();

            foreach (var cat in categories)
            {
                list.Add(new SelectListItem
                {
                    Value = cat.JobCategoryGuidID!.ToString(),
                    Text = currentCulture.Contains("en") ? cat.JobCategoryNameEn! : cat.JobCategoryNameNative!
                });
            }

            return list;

            //return new List<SelectListItem>
            //{
            //    new SelectListItem { Value = "Teacher", Text = "Teacher" },
            //    new SelectListItem { Value = "Assistant Teacher", Text = "Assistant Teacher" },
            //    new SelectListItem { Value = "School Counselor", Text = "School Counselor" },
            //    new SelectListItem { Value = "Admin Assistant", Text = "Administrative Assistant" },
            //    new SelectListItem { Value = "Janitor", Text = "Janitor" },
            //    new SelectListItem { Value = "Security", Text = "Security Guard" },
            //    new SelectListItem { Value = "Principal", Text = "Principal" },
            //    new SelectListItem { Value = "Vice Principal", Text = "Vice Principal" },
            //    new SelectListItem { Value = "Librarian", Text = "Librarian" },
            //    new SelectListItem { Value = "IT Support", Text = "IT Support" }
            //};
        }

        private async Task HydrateModelIdFromExistingDraftAsync(JobApplicationViewModel model)
        {
            if (model.Id > 0 || string.IsNullOrWhiteSpace(model.MobileNumber))
                return;

            var existingDraft = await _context.JobApplication
                .AsNoTracking()
                .FirstOrDefaultAsync(j =>
                    j.CompanyId == GlobalVariablesService.CompanyId &&
                    !j.IsFromCompanySetup &&
                    j.MobileNumber == model.MobileNumber &&
                    j.IsDraft);

            if (existingDraft is not null)
                model.Id = existingDraft.Id;
        }

        private IActionResult JsonValidationErrorFromModelState() =>
            BadRequest(new { success = false, errorType = "validation", message = GetFirstModelError() ?? "Please check the required fields." });

        private IActionResult JsonError(string message, string errorType, int statusCode) =>
            StatusCode(statusCode, new { success = false, errorType, message });

        private string? GetFirstModelError() =>
            ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

        private void ValidateSaveStep(int step, JobApplicationViewModel model)
        {
            switch (step)
            {
                case 1:
                    RequireField(model.FullName, nameof(model.FullName));
                    RequireField(model.Email, nameof(model.Email));
                    RequireField(model.PlaceOfBirth, nameof(model.PlaceOfBirth));
                    RequireField(model.Address, nameof(model.Address));
                    RequireField(model.NationalID, nameof(model.NationalID));
                    RequireField(model.MobileNumber, nameof(model.MobileNumber));
                    RequireField(model.Nationality, nameof(model.Nationality));
                    RequireField(model.ApplyingFor, nameof(model.ApplyingFor));
                    RequireField(model.JobTitle, nameof(model.JobTitle));

                    if (!model.DateOfBirth.HasValue)
                        ModelState.AddModelError(nameof(model.DateOfBirth), "Date of Birth is required.");

                    if (model.ExpectedSalary < 1)
                        ModelState.AddModelError(nameof(model.ExpectedSalary), "Expected Salary is required.");

                    if (model.Id == 0 && model.ImageFile is null)
                        ModelState.AddModelError(nameof(model.ImageFile), "Profile image is required.");
                    break;

                case 2:
                    RequireField(model.HighSchoolName, nameof(model.HighSchoolName));

                    if (model.HighSchoolGraduationYear <= 0)
                        ModelState.AddModelError(nameof(model.HighSchoolGraduationYear), "High School Graduation Year is required.");

                    for (var i = 0; i < model.Universities.Count; i++)
                    {
                        var university = model.Universities[i];
                        if (string.IsNullOrWhiteSpace(university.UniversityName))
                            ModelState.AddModelError($"Universities[{i}].UniversityName", "University Name is required.");
                        if (string.IsNullOrWhiteSpace(university.Collage))
                            ModelState.AddModelError($"Universities[{i}].Collage", "Collage / Specialization Name is required.");
                        if (university.UniversityGraduationYear <= 0)
                            ModelState.AddModelError($"Universities[{i}].UniversityGraduationYear", "University Graduation Year is required.");
                    }
                    break;

                case 3:
                    if (model.EnglishLevelId <= 0)
                        ModelState.AddModelError(nameof(model.EnglishLevelId), "English Level is required.");

                    if (model.ComputerSkillsLevelId <= 0)
                        ModelState.AddModelError(nameof(model.ComputerSkillsLevelId), "Computer Skills Level is required.");

                    for (var i = 0; i < model.Courses.Count; i++)
                    {
                        var course = model.Courses[i];
                        if (string.IsNullOrWhiteSpace(course.CourseName))
                            ModelState.AddModelError($"Courses[{i}].CourseName", "Course Name is required.");
                        if (string.IsNullOrWhiteSpace(course.CourseAddress))
                            ModelState.AddModelError($"Courses[{i}].CourseAddress", "Course Address is required.");
                        if (!course.From.HasValue)
                            ModelState.AddModelError($"Courses[{i}].From", "From date is required.");
                        if (!course.To.HasValue)
                            ModelState.AddModelError($"Courses[{i}].To", "To date is required.");
                    }
                    break;

                case 4:
                    RequireField(model.CurrentEmployerName, nameof(model.CurrentEmployerName));
                    RequireField(model.CurrentEmployerAddress, nameof(model.CurrentEmployerAddress));
                    RequireField(model.CurrentJobDescription, nameof(model.CurrentJobDescription));
                    RequireField(model.ReasonForLeavingCurrent, nameof(model.ReasonForLeavingCurrent));

                    if (!model.CurrentFrom.HasValue)
                        ModelState.AddModelError(nameof(model.CurrentFrom), "From date is required.");
                    if (!model.CurrentTo.HasValue)
                        ModelState.AddModelError(nameof(model.CurrentTo), "To date is required.");
                    if (!model.ReadyToJoinFrom.HasValue)
                        ModelState.AddModelError(nameof(model.ReadyToJoinFrom), "Ready To Join From is required.");
                    break;

                case 5:
                    for (var i = 0; i < model.WorkExperiences.Count; i++)
                    {
                        var experience = model.WorkExperiences[i];
                        if (string.IsNullOrWhiteSpace(experience.EmployerName))
                            ModelState.AddModelError($"WorkExperiences[{i}].EmployerName", "Employer Name is required.");
                        if (!experience.From.HasValue)
                            ModelState.AddModelError($"WorkExperiences[{i}].From", "From date is required.");
                        if (!experience.To.HasValue)
                            ModelState.AddModelError($"WorkExperiences[{i}].To", "To date is required.");
                        if (string.IsNullOrWhiteSpace(experience.ReasonForLeaving))
                            ModelState.AddModelError($"WorkExperiences[{i}].ReasonForLeaving", "Reason for Leaving is required.");
                    }
                    break;

                case 6:
                    break;
            }
        }

        private void RequireField(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                ModelState.AddModelError(fieldName, $"{fieldName} is required.");
        }

        private async Task<string?> GetDuplicateFieldErrorAsync(JobApplicationViewModel model, bool checkAll)
        {
            if (!checkAll)
                return null;

            if (!string.IsNullOrWhiteSpace(model.FullName))
            {
                var nameExists = await _context.JobApplication.AnyAsync(j =>
                    j.CompanyId == GlobalVariablesService.CompanyId &&
                    !j.IsFromCompanySetup &&
                    j.FullName.Trim().ToLower() == model.FullName.Trim().ToLower() &&
                    j.Id != model.Id);

                if (nameExists)
                    return "Full Name is already used.";
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailExists = await _context.JobApplication.AnyAsync(j =>
                    j.CompanyId == GlobalVariablesService.CompanyId &&
                    !j.IsFromCompanySetup &&
                    j.Email.Trim().ToLower() == model.Email.Trim().ToLower() &&
                    j.Id != model.Id);

                if (emailExists)
                    return "Email is already used.";
            }

            if (!string.IsNullOrWhiteSpace(model.NationalID))
            {
                var nationalIdExists = await _context.JobApplication.AnyAsync(j =>
                    j.NationalID.Trim().ToLower() == model.NationalID.Trim().ToLower() &&
                    j.Id != model.Id);

                if (nationalIdExists)
                    return "National Number is already used.";
            }

            return null;
        }

        private async Task<JobApplication?> ResolveDraftApplicationAsync(JobApplicationViewModel model, int step)
        {
            if (model.Id > 0)
            {
                return await _context.JobApplication
                    .Include(j => j.Univesity)
                    .Include(j => j.Course)
                    .Include(j => j.WorkExperience)
                    .Include(j => j.AttachmentFiles)
                    .FirstOrDefaultAsync(j =>
                        j.Id == model.Id &&
                        j.CompanyId == GlobalVariablesService.CompanyId &&
                        !j.IsFromCompanySetup &&
                        j.IsDraft);
            }

            if (!string.IsNullOrWhiteSpace(model.MobileNumber))
            {
                var existingDraft = await _context.JobApplication
                    .Include(j => j.Univesity)
                    .Include(j => j.Course)
                    .Include(j => j.WorkExperience)
                    .Include(j => j.AttachmentFiles)
                    .FirstOrDefaultAsync(j =>
                        j.CompanyId == GlobalVariablesService.CompanyId &&
                        !j.IsFromCompanySetup &&
                        j.MobileNumber == model.MobileNumber);

                if (existingDraft is not null)
                {
                    if (!existingDraft.IsDraft)
                        return null;

                    model.Id = existingDraft.Id;
                    return existingDraft;
                }
            }

            if (step != 1)
                return null;

            return new JobApplication
            {
                CompanyId = GlobalVariablesService.CompanyId,
                CreatedOn = DateTime.Now,
                ImageUrl = "ProfileImagePlaceholder.jpg",
                HighSchoolName = string.Empty,
                IsDraft = true
            };
        }

        private async Task ApplyStep1Async(JobApplication jobApplication, JobApplicationViewModel model)
        {
            if (model.ImageFile != null)
            {
                var imageFileName = await ProcessFileAsync(model.ImageFile, "profileImages", "ImageFile");
                if (!ModelState.IsValid)
                    return;

                jobApplication.ImageUrl = imageFileName;
            }
            else if (string.IsNullOrWhiteSpace(jobApplication.ImageUrl))
            {
                jobApplication.ImageUrl = "ProfileImagePlaceholder.jpg";
            }

            jobApplication.FullName = model.FullName;
            jobApplication.Email = model.Email;
            jobApplication.DateOfBirth = model.DateOfBirth;
            jobApplication.PlaceOfBirth = model.PlaceOfBirth;
            jobApplication.GenderId = model.GenderId;
            jobApplication.Address = model.Address;
            jobApplication.Latitude = model.Latitude;
            jobApplication.Longitude = model.Longitude;
            jobApplication.NationalID = model.NationalID;
            jobApplication.MobileNumber = model.MobileNumber;
            jobApplication.Nationality = model.Nationality;
            jobApplication.MaritalStatusId = model.MaritalStatusId;
            jobApplication.ExpectedSalary = model.ExpectedSalary;
            jobApplication.ApplyingFor = model.ApplyingFor;
            jobApplication.JobTitle = model.JobTitle;
        }

        private async Task ApplyStep2Async(JobApplication jobApplication, JobApplicationViewModel model)
        {
            jobApplication.HighSchoolName = model.HighSchoolName;
            jobApplication.HighSchoolGraduationYear = model.HighSchoolGraduationYear;

            var universityList = model.Universities.ToList();
            var originalUniversityList = jobApplication.Univesity.ToList();

            for (var universityIndex = 0; universityIndex < universityList.Count; universityIndex++)
            {
                var universityAttachment = universityList[universityIndex].AttachmentFile;

                if (universityAttachment != null)
                {
                    var attachmentFileName = await ProcessFileAsync(universityAttachment, "universityAttachments", "UniversityAttachmentFile", true);
                    if (!ModelState.IsValid)
                        return;

                    universityList[universityIndex].AttachmentUrl = attachmentFileName;
                }
                else if (universityIndex < originalUniversityList.Count)
                {
                    universityList[universityIndex].AttachmentUrl = originalUniversityList[universityIndex].AttachmentUrl;
                }
            }

            await ReplaceUniversitiesAsync(jobApplication, universityList);
        }

        private void ApplyStep3(JobApplication jobApplication, JobApplicationViewModel model)
        {
            jobApplication.EnglishLevelId = model.EnglishLevelId;
            jobApplication.OtherLanguage = model.OtherLanguage;
            jobApplication.OtherLanguageLevelId = model.OtherLanguageLevelId;
            jobApplication.ComputerSkillsLevelId = model.ComputerSkillsLevelId;

            ReplaceCourses(jobApplication, model.Courses);
        }

        private void ApplyStep4(JobApplication jobApplication, JobApplicationViewModel model)
        {
            jobApplication.CurrentEmployerName = model.CurrentEmployerName;
            jobApplication.CurrentEmployerAddress = model.CurrentEmployerAddress;
            jobApplication.CurrentJobDescription = model.CurrentJobDescription;
            jobApplication.CurrentSalary = model.CurrentSalary;
            jobApplication.CurrentFrom = model.CurrentFrom;
            jobApplication.CurrentTo = model.CurrentTo;
            jobApplication.ReasonForLeavingCurrent = model.ReasonForLeavingCurrent;
            jobApplication.ReadyToJoinFrom = model.ReadyToJoinFrom;
        }

        private async Task ApplyStep5Async(JobApplication jobApplication, JobApplicationViewModel model)
        {
            var workExperienceList = model.WorkExperiences.ToList();
            var originalWorkExperienceList = jobApplication.WorkExperience.ToList();

            for (var workExperienceIndex = 0; workExperienceIndex < workExperienceList.Count; workExperienceIndex++)
            {
                var workExperienceAttachment = workExperienceList[workExperienceIndex].AttachmentFile;

                if (workExperienceAttachment != null)
                {
                    var attachmentFileName = await ProcessFileAsync(workExperienceAttachment, "workExperienceAttachments", "WorkExperienceAttachmentFile", true);
                    if (!ModelState.IsValid)
                        return;

                    workExperienceList[workExperienceIndex].AttachmentUrl = attachmentFileName;
                }
                else if (workExperienceIndex < originalWorkExperienceList.Count)
                {
                    workExperienceList[workExperienceIndex].AttachmentUrl = originalWorkExperienceList[workExperienceIndex].AttachmentUrl;
                }
            }

            await ReplaceWorkExperiencesAsync(jobApplication, workExperienceList);
        }

        private async Task ApplyStep6Async(JobApplication jobApplication, JobApplicationViewModel model)
        {
            var attachmentList = model.AttachmentFiles.ToList();
            var originalAttachmentList = jobApplication.AttachmentFiles.ToList();

            for (var attachmentIndex = 0; attachmentIndex < attachmentList.Count; attachmentIndex++)
            {
                var attachment = attachmentList[attachmentIndex].AttachmentFile;

                if (attachment != null)
                {
                    var attachmentFileName = await ProcessFileAsync(attachment, "attachments", "AttachmentFiles", true);
                    if (!ModelState.IsValid)
                        return;

                    attachmentList[attachmentIndex].AttachmentUrl = attachmentFileName;
                }
                else if (attachmentIndex < originalAttachmentList.Count)
                {
                    attachmentList[attachmentIndex].AttachmentUrl = originalAttachmentList[attachmentIndex].AttachmentUrl;
                    attachmentList[attachmentIndex].Id = originalAttachmentList[attachmentIndex].Id;
                }
            }

            await ReplaceAttachmentFilesAsync(jobApplication, attachmentList);
        }

        private async Task ReplaceUniversitiesAsync(JobApplication jobApplication, List<UniversityViewModel> universities)
        {
            if (jobApplication.Id > 0)
            {
                var existingUniversities = await _context.University
                    .Where(w => w.JobApplicationId == jobApplication.Id)
                    .ToListAsync();

                _context.University.RemoveRange(existingUniversities);
            }

            var newUniversities = _mapper.Map<List<University>>(universities);
            newUniversities.ForEach(u =>
            {
                u.JobApplicationId = jobApplication.Id;
                u.LastUpdatedOn = DateTime.Now;
            });

            jobApplication.Univesity = newUniversities;
        }

        private void ReplaceCourses(JobApplication jobApplication, List<CourseViewModel> courses)
        {
            if (jobApplication.Id > 0)
            {
                var existingCourses = _context.Course
                    .Where(w => w.JobApplicationId == jobApplication.Id)
                    .ToList();

                _context.Course.RemoveRange(existingCourses);
            }

            var newCourses = _mapper.Map<List<Course>>(courses);
            newCourses.ForEach(c =>
            {
                c.JobApplicationId = jobApplication.Id;
                c.LastUpdatedOn = DateTime.Now;
            });

            jobApplication.Course = newCourses;
        }

        private async Task ReplaceWorkExperiencesAsync(JobApplication jobApplication, List<WorkExperienceViewModel> workExperiences)
        {
            if (jobApplication.Id > 0)
            {
                var existingWorkExperiences = await _context.WorkExperience
                    .Where(w => w.JobApplicationId == jobApplication.Id)
                    .ToListAsync();

                _context.WorkExperience.RemoveRange(existingWorkExperiences);
            }

            var newWorkExperiences = _mapper.Map<List<WorkExperience>>(workExperiences);
            newWorkExperiences.ForEach(e =>
            {
                e.JobApplicationId = jobApplication.Id;
                e.LastUpdatedOn = DateTime.Now;
            });

            jobApplication.WorkExperience = newWorkExperiences;
        }

        private async Task ReplaceAttachmentFilesAsync(JobApplication jobApplication, List<AttachmentFileViewModel> attachmentFiles)
        {
            if (jobApplication.Id > 0)
            {
                var existingAttachments = await _context.AttachmentFile
                    .Where(a => a.JobApplicationId == jobApplication.Id)
                    .ToListAsync();

                _context.AttachmentFile.RemoveRange(existingAttachments);
            }

            var newAttachments = _mapper.Map<List<AttachmentFile>>(attachmentFiles);
            newAttachments.ForEach(a =>
            {
                a.JobApplicationId = jobApplication.Id;
                a.LastUpdatedOn = DateTime.Now;
            });

            jobApplication.AttachmentFiles = newAttachments;
        }

        private async Task<string> ProcessFileAsync(IFormFile file, string folderName, string fieldName, bool isAttachemnt = false)
        {
            var allowedExtensionsAccordingToType = isAttachemnt ? _allowedAttachmentExtensions : _allowedExtensions;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensionsAccordingToType.Contains(ext))
            {
                ModelState.AddModelError(fieldName, "Invalid file extension.");
                return string.Empty;
            }

            if (file.Length > _maxAllowedSize)
            {
                ModelState.AddModelError(fieldName, "File size exceeds 5 MB.");
                return string.Empty;
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", folderName);
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + ext;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        #endregion
    }
}


#region For testing
/* SOF Job Categories and titles */
//List<JobCategories> jobCategories = [];
//jobCategories = SysBase.GetJobCategories("88aa34452c5141e8b7b68443cad9b7e7");

//foreach (JobCategories jobCategory in jobCategories)
//{
//    string BJCGuidID = jobCategory.JobCategoryGuidID!;
//    string BJCNameEn = jobCategory.JobCategoryNameEn!;
//    string BJCNameNative = jobCategory.JobCategoryNameNative!;
//}

//List<JobTitles> jobTitles = [];
//jobTitles = SysBase.GetJobTitles("be2a8f9cd0444391aa4ba6ec32c87446");

//foreach (JobTitles jobTitle in jobTitles)
//{
//    string BJTGuidID = jobTitle.JobTitleGuidID!;
//    string BJTNameEn = jobTitle.JobTitleNameEn!;
//    string BJTNameNative = jobTitle.JobTitleNameNative!;
//}
/* EOF Job Categories and titles */
#endregion