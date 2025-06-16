using Microsoft.AspNetCore.Authorization;

namespace SmartTimeCVs.Web.Controllers
{
    [Authorize]

    public class JobApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public JobApplicationController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
            if (IsCompanyRequest == "False") return RedirectToAction("Logout", "Account");

            try
            {
                var jobApplications = await _context
                    .JobApplication.Where(p => p.CompanyId == GlobalVariablesService.CompanyId)
                    .OrderByDescending(p => p.Id)
                    .AsNoTracking()
                    .ToListAsync();

                var viewModel = _mapper.Map<List<JobApplicationViewModel>>(jobApplications);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }

        public IActionResult View(int id)
        {
            try
            {
                var job = _context.JobApplication.FirstOrDefault(x => x.Id == id);

                var viewModel = _mapper.Map<JobApplicationViewModel>(job);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var jobApplication = await _context.JobApplication.FindAsync(id);

                if (jobApplication is null)
                    return NotFound();

                var relatedUniversities = await _context
                    .University.Where(e => e.JobApplicationId == id)
                    .ToListAsync();
                var relatedCourses = await _context
                    .Course.Where(e => e.JobApplicationId == id)
                    .ToListAsync();
                var relatedWorkExperiences = await _context
                    .WorkExperience.Where(e => e.JobApplicationId == id)
                    .ToListAsync();
                var relatedAttachment = await _context
                    .AttachmentFile.Where(e => e.JobApplicationId == id)
                    .ToListAsync();

                jobApplication.IsDeleted = !jobApplication.IsDeleted;
                jobApplication.LastUpdatedOn = DateTime.Now;

                relatedUniversities.ForEach(u =>
                {
                    u.IsDeleted = !u.IsDeleted;
                    u.LastUpdatedOn = DateTime.Now;
                });
                relatedCourses.ForEach(c =>
                {
                    c.IsDeleted = !c.IsDeleted;
                    c.LastUpdatedOn = DateTime.Now;
                });
                relatedWorkExperiences.ForEach(e =>
                {
                    e.IsDeleted = !e.IsDeleted;
                    e.LastUpdatedOn = DateTime.Now;
                });
                relatedAttachment.ForEach(e =>
                {
                    e.IsDeleted = !e.IsDeleted;
                    e.LastUpdatedOn = DateTime.Now;
                });

                _context.JobApplication.Update(jobApplication);
                _context.University.UpdateRange(relatedUniversities);
                _context.Course.UpdateRange(relatedCourses);
                _context.WorkExperience.UpdateRange(relatedWorkExperiences);
                _context.AttachmentFile.UpdateRange(relatedAttachment);
                await _context.SaveChangesAsync();

                return Ok(jobApplication.LastUpdatedOn?.ToTableDate());
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }
    }
}
