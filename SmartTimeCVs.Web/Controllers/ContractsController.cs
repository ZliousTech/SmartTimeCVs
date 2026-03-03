using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SmartTimeCVs.Web.Core.Models;
using SmartTimeCVs.Web.Core.Services;
using SmartTimeCVs.Web.Data;

namespace SmartTimeCVs.Web.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ContractsController(ApplicationDbContext context, IEmailService emailService, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _emailService = emailService;
            _localizer = localizer;
        }

        // GET: Contracts
        public async Task<IActionResult> Index()
        {
            var contracts = await _context.Contracts
                .Include(c => c.JobApplication)
                .OrderByDescending(c => c.CreatedOn)
                .ToListAsync();
            return View(contracts);
        }

        // GET: Contracts/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SchoolName,RepresentativeName,RepresentativeTitle,EmployeeName,EmployeeNationalId,EmployeeAddress,JobTitle,ContractDuration,StartDate,EndDate,ProbationPeriod,MonthlySalary,SalaryPaymentDay,JobApplicationId")] Contract contract)
        {
            if (ModelState.IsValid)
            {
                contract.CreatedOn = DateTime.Now;
                _context.Add(contract);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }

        // GET: Contracts/Print/5
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.JobApplication)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }

        // API Endpoint to get Job Applications for autocomplete or dropdown
        [HttpGet]
        public async Task<IActionResult> GetJobApplications()
        {
            var applications = await _context.JobApplication
                .Include(j => j.JobOffer)
                .Where(j => j.JobOffer != null && j.JobOffer.Status == Core.Enums.JobOfferStatus.Accepted)
                .Select(j => new 
                { 
                    id = j.Id, 
                    fullName = j.FullName,
                    nationalId = j.NationalID,
                    address = j.Address,
                    jobTitle = j.JobOffer != null && !string.IsNullOrEmpty(j.JobOffer.Department) ? j.JobOffer.Department : j.JobTitle ?? j.ApplyingFor,
                    expectedSalary = j.JobOffer != null ? j.JobOffer.OfferedSalary : j.ExpectedSalary,
                    probationPeriod = j.JobOffer != null ? j.JobOffer.ProbationPeriod : "ثلاثة أشهر"
                })
                .ToListAsync();
            return Json(applications);
        }

        [HttpPost]
        public async Task<IActionResult> SendContractEmail(int contractId, IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
                return Json(new { success = false, message = _localizer["Invalid PDF file"].Value ?? "Invalid PDF file" });

            var contract = await _context.Contracts
                .Include(c => c.JobApplication)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null || contract.JobApplication == null || string.IsNullOrEmpty(contract.JobApplication.Email))
                return Json(new { success = false, message = _localizer["Contract or Employee Email not found"].Value ?? "Contract or Employee Email not found" });

            var subject = (_localizer["Work Contract"].Value ?? "Work Contract") + " - " + contract.SchoolName;
            var body = $@"
                <p>Dear {contract.EmployeeName},</p>
                <p>Please find attached your work contract with {contract.SchoolName}.</p>
                <p>Best regards,<br/>{contract.SchoolName}</p>
            ";

            var success = await _emailService.SendEmailAsync(
                to: contract.JobApplication.Email,
                subject: subject,
                body: body,
                attachment: pdfFile
            );

            if (success)
                return Json(new { success = true, message = _localizer["Email sent successfully!"].Value ?? "Email sent successfully!" });
            
            return Json(new { success = false, message = _localizer["Error sending email."].Value ?? "Error sending email." });
        }
    }
}
