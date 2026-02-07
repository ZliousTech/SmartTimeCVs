using Microsoft.AspNetCore.Authorization;

namespace SmartTimeCVs.Web.Controllers
{
    //[Authorize]

    public class JobApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public JobApplicationController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(bool shortlistedOnly = false)
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

            try
            {
                var jobApplications = await _context
                    .JobApplication.Where(p => p.CompanyId == GlobalVariablesService.CompanyId)
                    .OrderByDescending(p => p.Id)
                    .Where(p => !p.IsShortListed && !p.IsExcluded && !p.IsHolding)
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

        public async Task<IActionResult> ShortListedIndex()
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

            try
            {
                var jobApplications = await _context
                    .JobApplication.Where(p => p.CompanyId == GlobalVariablesService.CompanyId)
                    .OrderByDescending(p => p.Id)
                    .Where(p => p.IsShortListed == true)
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

        public async Task<IActionResult> ExcludedIndex()
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

            try
            {
                var jobApplications = await _context
                    .JobApplication.Where(p => p.CompanyId == GlobalVariablesService.CompanyId)
                    .OrderByDescending(p => p.Id)
                    .Where(p => p.IsExcluded == true)
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

        public async Task<IActionResult> HoldingIndex()
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

            try
            {
                var jobApplications = await _context
                    .JobApplication.Where(p => p.CompanyId == GlobalVariablesService.CompanyId)
                    .OrderByDescending(p => p.Id)
                    .Where(p => p.IsHolding == true)
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

                var uni = _context.University.Where(x => x.JobApplicationId == job.Id).ToList();

                var course = _context.Course.Where(x => x.JobApplicationId == job.Id).ToList();

                var viewModel = _mapper.Map<JobApplicationViewModel>(job);

                viewModel.Universities = uni.Select(x => new UniversityViewModel
                    {
                        Id = x.Id,
                        JobApplicationId = x.JobApplicationId,
                        UniversityGraduationYear = x.UniversityGraduationYear,
                        UniversityName = x.UniversityName,
                        Collage = x.Collage,
                    })
                    .ToList();

                viewModel.Courses = course
                    .Select(x => new CourseViewModel
                    {
                        Id = x.Id,
                        JobApplicationId = x.JobApplicationId,
                        CourseName = x.CourseName,
                        CourseAddress = x.CourseAddress,
                        From = x.From,
                        To = x.To,
                    })
                    .ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }

        public IActionResult ShortList(int id)
        {
            try
            {
                var job = _context.JobApplication.FirstOrDefault(x => x.Id == id);
                var uni = _context.University.Where(x => x.JobApplicationId == job.Id).ToList();
                var course = _context.Course.Where(x => x.JobApplicationId == job.Id).ToList();

                var viewModel = _mapper.Map<JobApplicationViewModel>(job);

                viewModel.Universities = uni.Select(x => new UniversityViewModel
                    {
                        Id = x.Id,
                        JobApplicationId = x.JobApplicationId,
                        UniversityGraduationYear = x.UniversityGraduationYear,
                        UniversityName = x.UniversityName,
                        Collage = x.Collage,
                    })
                    .ToList();

                viewModel.Courses = course
                    .Select(x => new CourseViewModel
                    {
                        Id = x.Id,
                        JobApplicationId = x.JobApplicationId,
                        CourseName = x.CourseName,
                        CourseAddress = x.CourseAddress,
                        From = x.From,
                        To = x.To,
                    })
                    .ToList();

                return View("ShortListView", viewModel);
            }
            catch (Exception ex)
            {
                return View("Error", new ErrorViewModel { Exception = ex.Message });
            }
        }

        public IActionResult CreateShortlist(int id)
        {
            var job = _context.JobApplication.FirstOrDefault(x => x.Id == id);

            if (job == null)
                return NotFound();

            job.IsShortListed = true;
            job.IsExcluded = false;
            job.IsHolding = false;
            _context.Update(job);
            _context.SaveChanges();

            // Redirect back to grid with a filter
            return RedirectToAction("ShortListedIndex");
        }

        public IActionResult CreateExclude(int id)
        {
            var job = _context.JobApplication.FirstOrDefault(x => x.Id == id);

            if (job == null)
                return NotFound();

            job.IsExcluded = true;
            job.IsShortListed = false;
            job.IsHolding = false;
            _context.Update(job);
            _context.SaveChanges();

            // Redirect back to grid with a filter
            return RedirectToAction("ExcludedIndex");
        }

        public IActionResult CreateHolding(int id)
        {
            var job = _context.JobApplication.FirstOrDefault(x => x.Id == id);

            if (job == null)
                return NotFound();

            job.IsHolding = true;
            job.IsShortListed = false;
            job.IsExcluded = false;
            _context.Update(job);
            _context.SaveChanges();

            // Redirect back to grid with a filter
            return RedirectToAction("HoldingIndex");
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
