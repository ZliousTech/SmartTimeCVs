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
        public async Task<IActionResult> Create()
        {
            ViewBag.ContractTypes = await _context.ContractTypes.ToListAsync();
            return View();
        }

        // POST: Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CompanyName,CompanyAddress,CommercialNumber,RepresentativeName,RepresentativeTitle,EmployeeName,EmployeeNationalId,EmployeeAddress,JobTitle,ContractDuration,StartDate,EndDate,ProbationPeriod,MonthlySalary,SalaryPaymentDay,JobApplicationId,ContractTypeId")] Contract contract)
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
        public async Task<IActionResult> Print(int? id, string lang = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewBag.ContractLang = lang;

            var contract = await _context.Contracts
                .Include(c => c.JobApplication)
                .Include(c => c.ContractType)
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
                .Where(j => j.JobOffer != null 
                         && j.JobOffer.Status == Core.Enums.JobOfferStatus.Accepted
                         && !_context.Contracts.Any(c => c.JobApplicationId == j.Id))
                .Select(j => new 
                { 
                    id = j.Id, 
                    fullName = j.FullName,
                    nationalId = j.NationalID,
                    address = j.Address,
                    jobTitle = !string.IsNullOrEmpty(j.JobTitle) ? j.JobTitle : (j.JobOffer != null && !string.IsNullOrEmpty(j.JobOffer.Department) ? j.JobOffer.Department : j.ApplyingFor),
                    expectedSalary = j.JobOffer != null ? j.JobOffer.OfferedSalary : j.ExpectedSalary,
                    probationPeriod = j.JobOffer != null ? j.JobOffer.ProbationPeriod : "ثلاثة أشهر"
                })
                .ToListAsync();
            return Json(applications);
        }

        [HttpGet]
        public async Task<IActionResult> GetContractTypeDetails(int id)
        {
            var contractType = await _context.ContractTypes.FindAsync(id);
            if (contractType == null)
            {
                return NotFound();
            }

            return Json(new 
            {
                companyName = contractType.FirstPartyName,
                companyAddress = contractType.FirstPartyAddress,
                representativeName = contractType.AuthorizedSignatory,
                commercialNumber = contractType.CommercialNumber
            });
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

            var subject = (_localizer["Work Contract"].Value ?? "Work Contract") + " - " + contract.CompanyName;
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var actionUrl = $"{baseUrl}/CandidatePortal/ContractLogin?contractId={contract.Id}";

            var body = $@"
<html dir='ltr'>
<body>
    <p>Dear {contract.EmployeeName},</p>
    <p>Please find attached your work contract with {contract.CompanyName}.</p>
    <p>To proceed, please print the contract, sign it, and upload the signed copy along with your National ID.</p>
    <br/>
    <p>Please click the button below to upload your documents:</p>
    <p><a href='{actionUrl}' style='display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;'>Upload Contract Requirements</a></p>
    <br/>
    <p>If the button is not clickable or hidden, you can copy and paste this link into your browser:</p>
    <p><b>{actionUrl}</b></p>
    <br/>
    <p>Best regards,<br/>{contract.CompanyName}</p>
</body>
</html>";

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
        [HttpGet]
        public async Task<IActionResult> DeleteSpecificContract()
        {
            var nationalId = "2000628930";
            var contract = await _context.Contracts
                .Include(c => c.JobApplication)
                .FirstOrDefaultAsync(c => c.EmployeeNationalId == nationalId);

            if (contract == null) return Content("Contract not found.");

            var attachments = await _context.ContractAttachments.Where(a => a.ContractId == contract.Id).ToListAsync();
            foreach (var attr in attachments)
            {
                if (!string.IsNullOrEmpty(attr.FileUrl))
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/attachments", attr.FileUrl);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                _context.ContractAttachments.Remove(attr);
            }

            if (!string.IsNullOrEmpty(contract.SignedContractUrl))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/attachments", contract.SignedContractUrl);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            if (!string.IsNullOrEmpty(contract.NationalIdUrl))
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/attachments", contract.NationalIdUrl);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();

            return Content("Successfully deleted the contract and its associated files.");
        }
    }
}
