using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using SmartTimeCVs.Web.Data;
using SmartTimeCVs.Web.Core.Models;
using SmartTimeCVs.Web.Core.ViewModels;
using SmartTimeCVs.Web.Helpers;
using SmartTimeCVs.Web.Core.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartTimeCVs.Web.Core.Enums;
using System.Threading;
using Common.Base;
using SmartTimeCVs.Web.Core.Dtos;
using System.Net.Http;
using System.Text.Json;

namespace SmartTimeCVs.Web.Controllers
{
    public class NewCompanySetupController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly Microsoft.Extensions.Localization.IStringLocalizer<SharedResource> _localizer;
        private readonly IConfiguration _configuration;

        private readonly List<string> _allowedExtensions = new() { ".jpg", ".jpeg", ".png" };
        private readonly List<string> _allowedAttachmentExtensions = new() { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };
        private const int _maxAllowedSize = 5242880; // 5 MB.

        public NewCompanySetupController(ApplicationDbContext context, IMapper mapper, IWebHostEnvironment webHostEnvironment, Microsoft.Extensions.Localization.IStringLocalizer<SharedResource> localizer, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
            _configuration = configuration;
        }

        // Admin View: List of employees registered through this setup
        public async Task<IActionResult> Index()
        {
            try
            {
                var employees = await _context.JobApplication
                    .Where(p => p.CompanyId == GlobalVariablesService.CompanyId && p.IsFromCompanySetup)
                    .OrderByDescending(p => p.Id)
                    .ToListAsync();

                var viewModel = _mapper.Map<List<JobApplicationViewModel>>(employees);

                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                ViewData["RegistrationLink"] = $"{baseUrl}/NewCompanySetup/Register?companyId={GlobalVariablesService.CompanyId}";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveEmployee(int id)
        {
            try
            {
                var employee = await _context.JobApplication.FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.CompanyId == GlobalVariablesService.CompanyId &&
                    x.IsFromCompanySetup);
                if (employee == null) return NotFound();
                if (employee.CandidateStatus == CandidateStatus.Hired || employee.CandidateStatus == CandidateStatus.Rejected)
                    return BadRequest(new { success = false, message = "Invalid state for approval." });

                employee.CandidateStatus = CandidateStatus.Hired;
                employee.LastUpdatedOn = DateTime.Now;
                // Align with Personal Information / newcomers: hiring metadata for the external API (same JobApplication row).
                employee.HiringDate ??= DateTime.Now;
                employee.IsImported = false;

                _context.JobApplication.Update(employee);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectEmployee(int id)
        {
            try
            {
                var employee = await _context.JobApplication.FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.CompanyId == GlobalVariablesService.CompanyId &&
                    x.IsFromCompanySetup);
                if (employee == null) return NotFound();
                if (employee.CandidateStatus == CandidateStatus.Hired || employee.CandidateStatus == CandidateStatus.Rejected)
                    return BadRequest(new { success = false, message = "Invalid state for rejection." });

                employee.CandidateStatus = CandidateStatus.Rejected;
                employee.LastUpdatedOn = DateTime.Now;

                _context.JobApplication.Update(employee);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                var employee = await _context.JobApplication.FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.CompanyId == GlobalVariablesService.CompanyId &&
                    x.IsFromCompanySetup);
                if (employee == null) return NotFound();
                if (employee.CandidateStatus != CandidateStatus.Rejected)
                    return BadRequest(new { success = false, message = "Only rejected registrations can be deleted." });

                _context.JobApplication.Remove(employee);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Employee View: Initial Registration (Sign Up)
        [HttpGet]
        public async Task<IActionResult> Register(string companyId)
        {
            if (string.IsNullOrEmpty(companyId))
            {
                return BadRequest("Company ID is required.");
            }

            var model = new RegisterViewModel { CompanyId = companyId };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var emailExists = await _context.JobApplication
                    .AnyAsync(p => p.Email == model.Email && p.CompanyId == model.CompanyId);
                
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                var userExists = await _context.JobApplication
                    .AnyAsync(p => p.SystemUserName == model.UserName);

                if (userExists)
                {
                    ModelState.AddModelError("UserName", "This user name is already taken.");
                    return View(model);
                }

                try
                {
                    // Call external API safely
                    var isAvailableExternally = await CheckExternalUserNameAvailabilityAsync(model.UserName);
                    if (!isAvailableExternally)
                    {
                        ModelState.AddModelError("UserName", "This user name is already registered in the main company system.");
                        return View(model);
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError(string.Empty, "We are currently experiencing server issues. Please try again later.");
                    return View(model);
                }

                var application = new JobApplication
                {
                    Email = model.Email,
                    SystemUserName = model.UserName,
                    SystemPassword = model.Password,
                    CompanyId = model.CompanyId,
                    IsFromCompanySetup = true,
                    IsImported = false,
                    FullName = model.FullName, 
                    ImageUrl = "",
                    PlaceOfBirth = "",
                    Address = "",
                    NationalID = "Temp-" + Guid.NewGuid().ToString().Substring(0, 8), 
                    MobileNumber = "",
                    Nationality = "",
                    HighSchoolName = "",
                    GenderId = 1,
                    MaritalStatusId = 1,
                    EnglishLevelId = 1,
                    ComputerSkillsLevelId = 1,
                    CreatedOn = DateTime.Now,
                    LastUpdatedOn = DateTime.Now
                };

                _context.JobApplication.Add(application);
                await _context.SaveChangesAsync();

                return RedirectToAction("CompleteProfile", new { id = application.Id });
            }

            return View(model);
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> VerifyUserName(string userName)
        {
            // First check local DB
            var userExists = await _context.JobApplication
                .AnyAsync(p => p.SystemUserName == userName);

            if (userExists)
            {
                return Json("This user name is already taken locally.");
            }

            try
            {
                // Then check external API
                var isAvailableExternally = await CheckExternalUserNameAvailabilityAsync(userName);
                if (!isAvailableExternally)
                {
                    return Json("This user name is already registered in the main company system.");
                }
            }
            catch (Exception)
            {
                return Json("We are currently experiencing server issues. Please try again later.");
            }

            return Json(true);
        }

        private async Task<bool> CheckExternalUserNameAvailabilityAsync(string userName)
        {
            var enableCheck = _configuration.GetValue<bool>("ExternalApiSettings:EnableUserCheck");
            if (!enableCheck)
            {
                // Feature toggle is off, assume available to proceed without blocking
                return true; 
            }

            var apiUrl = _configuration.GetValue<string>("ExternalApiSettings:CheckUserNameUrl");
            if (string.IsNullOrEmpty(apiUrl)) return true;

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10); // Don't hang if API is down

                var requestBody = new { userName = userName };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseString);
                    
                    // Parse: "data": {"isAvailable": false}
                    if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement) && 
                        dataElement.TryGetProperty("isAvailable", out var isAvailableElement))
                    {
                        return isAvailableElement.GetBoolean();
                    }
                }
                
                // If API fails or returns non-success code, fail-closed (block registration)
                // so we don't allow duplicates if the external system is down.
                throw new Exception("We are currently experiencing server issues. Please try again later.");
            }
            catch
            {
                // In case of timeout or network error, we fail-closed.
                throw new Exception("We are currently experiencing server issues. Please try again later.");
            }
        }

        // Employee View: Complete Biography
        [HttpGet]
        public async Task<IActionResult> CompleteProfile(int id, string? companyId = null)
        {
            var application = await _context.JobApplication.FindAsync(id);
            if (application == null) return NotFound();
            if (!application.IsFromCompanySetup) return NotFound();

            // Fallback for older records or URL params
            if (string.IsNullOrEmpty(application.CompanyId) && !string.IsNullOrEmpty(companyId))
            {
                application.CompanyId = companyId;
                _context.Update(application);
                await _context.SaveChangesAsync();
            }

            var viewModel = _mapper.Map<JobApplicationViewModel>(application);

            // Clear placeholders so the user sees an empty field
            if (viewModel.FullName != null && viewModel.FullName.StartsWith("Pending-")) viewModel.FullName = "";
            if (viewModel.NationalID != null && viewModel.NationalID.StartsWith("Temp-")) viewModel.NationalID = "";

            return View(PopulateViewModel(viewModel, application.CompanyId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(JobApplicationViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var application = await _context.JobApplication
                    .Include(j => j.Univesity)
                    .Include(j => j.Course)
                    .Include(j => j.WorkExperience)
                    .Include(j => j.AttachmentFiles)
                    .FirstOrDefaultAsync(j => j.Id == model.Id);

                if (application == null) return NotFound();
                if (!application.IsFromCompanySetup)
                    return Json(new { success = false, message = "Invalid application." });

                // Validation (These are not asked in the simplified New Company Setup view)
                ModelState.Remove("ExpectedSalary");
                ModelState.Remove("HighSchoolName");
                ModelState.Remove("EnglishLevelId");
                ModelState.Remove("ComputerSkillsLevelId");
                ModelState.Remove("GenderId"); // Since we might use different names or just want to be safe
                
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = "Validation failed: " + errors });
                }

                // Server-side duplicate checks to ensure unique constraints are not violated
                var nameExists = await _context.JobApplication
                    .AnyAsync(j => j.FullName.Trim().ToLower() == model.FullName.Trim().ToLower() && 
                                   j.CompanyId == model.CompanyId && 
                                   j.Id != model.Id);
                if (nameExists) return Json(new { success = false, message = "This Full Name is already registered." });

                var nationalIdExists = await _context.JobApplication
                    .AnyAsync(j => j.NationalID == model.NationalID && 
                                   j.CompanyId == model.CompanyId && 
                                   j.Id != model.Id);
                if (nationalIdExists) return Json(new { success = false, message = "This National ID is already registered." });

                var mobileExists = await _context.JobApplication
                    .AnyAsync(j => j.MobileNumber == model.MobileNumber && 
                                   j.CompanyId == model.CompanyId && 
                                   j.Id != model.Id);
                if (mobileExists) return Json(new { success = false, message = "This Mobile Number is already registered." });

                // Ensure Image is provided
                if (model.ImageFile == null && string.IsNullOrEmpty(application.ImageUrl))
                {
                    return Json(new { success = false, message = "Profile image is required." });
                }

                _mapper.Map(model, application);

                // Process Files (Image, Universities, etc.)
                if (model.ImageFile != null)
                {
                    var fileName = await ProcessFileAsync(model.ImageFile, "profileImages", "ImageFile");
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        application.ImageUrl = fileName;
                    }
                }
                
                application.IsFromCompanySetup = true;
                application.LastUpdatedOn = DateTime.Now;
                application.ExpectedSalary = application.ExpectedSalary > 0 ? application.ExpectedSalary : 1; 

                // Safety checks for required fields that might be missing from this simplified view
                application.HighSchoolName ??= "";
                application.ImageUrl ??= "";
                application.PlaceOfBirth ??= "";
                application.Address ??= "";
                application.Nationality ??= "";
                application.MobileNumber ??= "";
                application.NationalID ??= "";

                if (application.GenderId == 0) application.GenderId = 1;
                if (application.MaritalStatusId == 0) application.MaritalStatusId = 1;
                if (application.EnglishLevelId == 0) application.EnglishLevelId = 1;
                if (application.ComputerSkillsLevelId == 0) application.ComputerSkillsLevelId = 1;

                // Re-link dynamic items only if they are provided
                if (model.Universities != null && model.Universities.Any())
                {
                    _context.University.RemoveRange(application.Univesity); // Clear existing
                    var newUniversities = _mapper.Map<List<University>>(model.Universities);
                    foreach(var u in newUniversities) { u.JobApplicationId = application.Id; u.LastUpdatedOn = DateTime.Now; }
                    application.Univesity = newUniversities;
                }

                if (model.Courses != null && model.Courses.Any())
                {
                    _context.Course.RemoveRange(application.Course); // Clear existing
                    var newCourses = _mapper.Map<List<Course>>(model.Courses);
                    foreach(var c in newCourses) { c.JobApplicationId = application.Id; c.LastUpdatedOn = DateTime.Now; }
                    application.Course = newCourses;
                }

                _context.Update(application);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Profile completed successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                return Json(new { success = false, message = ex.Message + " " + innerMsg });
            }
        }

        public IActionResult Success()
        {
            return View();
        }

        #region Private Methods
        private JobApplicationViewModel PopulateViewModel(JobApplicationViewModel? model = null, string? companyId = null)
        {
            ViewBag.GenderList = InitEnumList<GenderTypeEnum>();
            ViewBag.MaritalStatusList = InitEnumList<MaritalStatusTypeEnum>();
            ViewBag.NationalityList = InitNationalityList();
            ViewBag.ApplyingForList = InitApplyingForList(companyId);

            var levels = InitEnumList<LevelsEnum>();
            ViewBag.EnglishLevelList = levels;
            ViewBag.OtherLanguageLevelList = InitEnumList<LevelsEnum>(false);
            ViewBag.ComputerSkillsLevelList = levels;

            return model ?? new JobApplicationViewModel();
        }

        private List<SelectListItem> InitEnumList<E>(bool isReqiured = true) where E : Enum
        {
            List<SelectListItem> enumValues = new List<SelectListItem>();
            if (!isReqiured)
                enumValues.Add(new SelectListItem { Text = "--Please Select--", Value = "", Selected = true });

            foreach (var value in Enum.GetValues(typeof(E)))
            {
                enumValues.Add(new SelectListItem { Text = _localizer[value.ToString()].Value, Value = ((int)value).ToString() });
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

        private List<SelectListItem> InitApplyingForList(string? companyId = null)
        {
            var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
            string CompanyGuidID = companyId ?? GlobalVariablesService.CompanyId;
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
        }

        private async Task<string> ProcessFileAsync(IFormFile file, string folderName, string fieldName, bool isAttachemnt = false)
        {
            var allowedExtensionsAccordingToType = isAttachemnt ? _allowedAttachmentExtensions : _allowedExtensions;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensionsAccordingToType.Contains(ext)) return string.Empty;
            if (file.Length > _maxAllowedSize) return string.Empty;

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
