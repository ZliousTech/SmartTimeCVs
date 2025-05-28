using Microsoft.AspNetCore.Authorization;
using SmartTimeCVs.Web.Core.Enums;

namespace SmartTimeCVs.Web.Controllers
{
    [Authorize]
    public class JobApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly List<string> _allowedExtensions = new() { ".jpg", ".jpeg", ".png" };
        private const int _maxAllowedSize = 5242880; // 5 MB.

        public JobApplicationController(ApplicationDbContext context, IMapper mapper, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var jobApplications = _context.JobApplication
                .Include(p => p.WorkExperience)
                .Where(p => p.CompanyId == GlobalVariablesService.CompanyId)
                .OrderByDescending(p => p.Id)
                .AsNoTracking()
                .ToList();

            var viewModel = _mapper.Map<IEnumerable<JobApplicationViewModel>>(jobApplications);

            return View(viewModel);
        }

        public IActionResult Create()
        {
            return View("Form", PopulateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobApplicationViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!ModelState.IsValid)
                    return View("Form", PopulateViewModel(model));

                // Process ImageFile
                if (model.ImageFile != null)
                {
                    var imageFileName = await ProcessFileAsync(model.ImageFile, "CVs", "ImageFile");
                    if (!ModelState.IsValid)
                        return View("Form", PopulateViewModel(model));
                    model.ImageUrl = imageFileName;
                }

                // Process AttachmentFile
                if (model.AttachmentFile != null)
                {
                    var attachmentFileName = await ProcessFileAsync(model.AttachmentFile, "attachments", "AttachmentFile");
                    if (!ModelState.IsValid)
                        return View("Form", PopulateViewModel(model));
                    model.AttachmentUrl = attachmentFileName;
                }

                // Map ViewModel to Entity
                var jobApplication = _mapper.Map<JobApplication>(model);
                jobApplication.CompanyId = GlobalVariablesService.CompanyId;
                jobApplication.CreatedOn = DateTime.Now;
                jobApplication.LastUpdatedOn = DateTime.Now;

                // Map and link WorkExperiences
                var workExperiences = _mapper.Map<List<WorkExperience>>(model.WorkExperiences);
                jobApplication.WorkExperience = workExperiences;

                // Add and save
                _context.JobApplication.Add(jobApplication);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Message"] = Messages.Saved;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "An error occurred while saving the data.");
                return View("Form", PopulateViewModel(model));
            }
        }


        public IActionResult Edit(int id)
        {
            var jobApp = _context.JobApplication.Find(id);

            if (jobApp is null)
                return NotFound();

            var realtedWorkExperience = _context.WorkExperience.Where(e => e.JobApplicationId == id).ToList();

            var viewModel = _mapper.Map<JobApplicationViewModel>(jobApp);

            if (realtedWorkExperience is not null)
                viewModel.WorkExperiences = _mapper.Map<List<WorkExperienceViewModel>>(realtedWorkExperience);

            return View("Form", PopulateViewModel(viewModel));
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
                    return View("Form", PopulateViewModel(model));
                }

                var jobApplication = await _context.JobApplication
                    .Include(j => j.WorkExperience)
                    .FirstOrDefaultAsync(j => j.Id == model.Id);

                if (jobApplication == null)
                    return NotFound();

                model.CompanyId = GlobalVariablesService.CompanyId;

                // Handle image file
                if (model.ImageFile != null)
                {
                    var imageFileName = await ProcessFileAsync(model.ImageFile, "CVs", "ImageFile");
                    if (!ModelState.IsValid)
                        return View("Form", PopulateViewModel(model));

                    model.ImageUrl = imageFileName;
                }
                else
                {
                    model.ImageUrl = jobApplication.ImageUrl;
                }

                // Handle attachment file
                if (model.AttachmentFile != null)
                {
                    var attachmentFileName = await ProcessFileAsync(model.AttachmentFile, "attachments", "AttachmentFile");
                    if (!ModelState.IsValid)
                        return View("Form", PopulateViewModel(model));

                    model.AttachmentUrl = attachmentFileName;
                }
                else
                {
                    model.AttachmentUrl = jobApplication.AttachmentUrl;
                }

                // Update job application data using AutoMapper
                _mapper.Map(model, jobApplication);
                jobApplication.LastUpdatedOn = DateTime.Now;

                // Remove existing work experiences from DB
                var existingWorkExperiences = await _context.WorkExperience
                    .Where(w => w.JobApplicationId == jobApplication.Id)
                    .ToListAsync();

                _context.WorkExperience.RemoveRange(existingWorkExperiences);

                // Add new work experiences
                var newWorkExperiences = _mapper.Map<List<WorkExperience>>(model.WorkExperiences);
                newWorkExperiences.ForEach(e => e.JobApplicationId = jobApplication.Id);

                jobApplication.WorkExperience = newWorkExperiences;

                _context.Update(jobApplication);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Message"] = Messages.Updated;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "An error occurred while updating the data.");
                return View("Form", PopulateViewModel(model));
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleStatus(int id)
        {
            var jobApplication = _context.JobApplication.Find(id);

            if (jobApplication is null)
                return NotFound();

            var relatedWorkExperience = _context.WorkExperience.Where(e => e.JobApplicationId == id).ToList();

            jobApplication.IsDeleted = !jobApplication.IsDeleted;
            jobApplication.LastUpdatedOn = DateTime.Now;

            relatedWorkExperience.ForEach(e =>
            {
                e.IsDeleted = !e.IsDeleted;
                e.LastUpdatedOn = DateTime.Now;
            });


            _context.JobApplication.Update(jobApplication);
            _context.WorkExperience.UpdateRange(relatedWorkExperience);
            _context.SaveChanges();

            return Ok(jobApplication.LastUpdatedOn?.ToTableDate());
        }

        public IActionResult AllowName(JobApplicationViewModel model)
        {
            var item = _context.JobApplication
                .SingleOrDefault(i => i.FullName.Trim().ToLower() == model.FullName.Trim().ToLower() && i.CompanyId == GlobalVariablesService.CompanyId);
            var isAllowed = item is null || item.Id.Equals(model.Id);

            return Json(isAllowed);
        }


        #region Private Area.

        private JobApplicationViewModel PopulateViewModel(JobApplicationViewModel? model = null)
        {
            ViewBag.GenderList = InitEnumList<GenderTypeEnum>();
            ViewBag.MaritalStatusList = InitEnumList<MaritalStatusTypeEnum>();
            ViewBag.NationalityList = InitNationalityList();
            ViewBag.ApplyingForList = InitApplyingForList();

            var levels = InitEnumList<LevelsEnum>();
            ViewBag.EnglishLevelList = levels;
            ViewBag.OtherLanguageLevelList = levels;
            ViewBag.ComputerSkillsLevelList = levels;

            return model ?? new JobApplicationViewModel();
        }

        private List<SelectListItem> InitEnumList<E>()
            where E : Enum
        {
            List<SelectListItem> enumValues = new List<SelectListItem>();

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
                new SelectListItem { Value = "Egypt", Text = "Egypt" },
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
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "Teacher", Text = "Teacher" },
                new SelectListItem { Value = "AssistantTeacher", Text = "Assistant Teacher" },
                new SelectListItem { Value = "SchoolCounselor", Text = "School Counselor" },
                new SelectListItem { Value = "AdminAssistant", Text = "Administrative Assistant" },
                new SelectListItem { Value = "Janitor", Text = "Janitor" },
                new SelectListItem { Value = "Security", Text = "Security Guard" },
                new SelectListItem { Value = "Principal", Text = "Principal" },
                new SelectListItem { Value = "VicePrincipal", Text = "Vice Principal" },
                new SelectListItem { Value = "Librarian", Text = "Librarian" },
                new SelectListItem { Value = "ITSupport", Text = "IT Support" }
            };
        }

        private async Task<string?> ProcessFileAsync(IFormFile file, string folderName, string fieldName)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!_allowedExtensions.Contains(ext))
            {
                ModelState.AddModelError(fieldName, "Invalid file extension.");
                return null;
            }

            if (file.Length > _maxAllowedSize)
            {
                ModelState.AddModelError(fieldName, "File size exceeds 5 MB.");
                return null;
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
