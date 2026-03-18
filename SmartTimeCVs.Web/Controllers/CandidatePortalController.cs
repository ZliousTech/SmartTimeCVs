using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using SmartTimeCVs.Web.Core.Dtos;
using SmartTimeCVs.Web.Core.Models;
using SmartTimeCVs.Web.Core.Services;
using SmartTimeCVs.Web.Core.ViewModels;
using SmartTimeCVs.Web.Data;

namespace SmartTimeCVs.Web.Controllers
{
    [AllowAnonymous]
    public class CandidatePortalController : Controller
    {
        private readonly IJobOfferService _jobOfferService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CandidatePortalController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CandidatePortalController(IJobOfferService jobOfferService, ApplicationDbContext context, ILogger<CandidatePortalController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _jobOfferService = jobOfferService;
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        private async Task LoadCompanyDataAsync(int appId)
        {
            try
            {
                var application = await _context.JobApplication.FindAsync(appId);
                if (application != null && !string.IsNullOrEmpty(application.CompanyId))
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetFromJsonAsync<SmartTimeCompanyDTO>
                                    ($"https://smarttimeapi.zlioustech.com/api/Company/GetCompanyLogoHomePageText/{application.CompanyId}");

                    var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;
                    ViewBag.CompanyName = currentCulture.Contains("en") ? response?.Data?.CompanyNameEn : response?.Data?.CompanyNameNative;
                    ViewBag.HomePageHtml = currentCulture.Contains("en") ? response?.Data?.HomePageTextEn : response?.Data?.HomePageTextNative;
                    ViewBag.CompanyLogo = response?.Data?.CompanyLogo;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load company data for app {AppId}", appId);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewOfferFromBiography(int appId)
        {
            if (appId <= 0) return NotFound();
            await LoadCompanyDataAsync(appId);

            var offerViewModel = await _jobOfferService.GetOfferViewModelAsync(appId);
            if (offerViewModel == null) return NotFound();

            // Get mobile from the application for SubmitResponse verification
            var app = await _context.JobApplication.FindAsync(appId);
            ViewBag.MobileNumber = app?.MobileNumber;

            return View("ViewOffer", offerViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Login(int appId)
        {
            if (appId <= 0) return NotFound();
            await LoadCompanyDataAsync(appId);
            return View(new CandidateLoginViewModel { AppId = appId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(CandidateLoginViewModel model)
        {
            await LoadCompanyDataAsync(model.AppId);
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var offer = await _jobOfferService.GetOfferByApplicationIdAsync(model.AppId);
                if (offer == null)
                {
                    ModelState.AddModelError("", "Job offer not found or has been revoked.");
                    return View(model);
                }

                bool isValid = await _jobOfferService.ValidateCandidateMobileAsync(offer.Id, model.MobileNumber);
                if (isValid)
                {
                    var offerViewModel = await _jobOfferService.GetOfferViewModelAsync(model.AppId);
                    if (offerViewModel == null) return NotFound();

                    ViewBag.MobileNumber = model.MobileNumber;
                    return View("ViewOffer", offerViewModel);
                }

                ModelState.AddModelError("", "The mobile number provided does not match our records.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during candidate login for appId {AppId}", model.AppId);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitResponse(int offerId, string mobileNumber, bool isAccepted)
        {
            if (offerId <= 0 || string.IsNullOrWhiteSpace(mobileNumber))
                return BadRequest("Invalid request data.");

            var offer = await _jobOfferService.GetOfferViewModelAsync(offerId); // we need appId for layout

            bool isValid = await _jobOfferService.ValidateCandidateMobileAsync(offerId, mobileNumber);
            if (!isValid)
                return Unauthorized("Mobile number verification failed.");

            // Load layout data if possible
            var actualOffer = await _context.JobOffer.FindAsync(offerId);
            if (actualOffer != null) await LoadCompanyDataAsync(actualOffer.JobApplicationId);

            var success = await _jobOfferService.RespondToOfferAsync(offerId, isAccepted);
            
            if (success)
            {
                ViewBag.IsAccepted = isAccepted;
                return View("OfferResponded");
            }
            
            return StatusCode(500, "An error occurred while saving your response. Please contact HR.");
        }

        [HttpGet]
        public async Task<IActionResult> ContractLogin(int contractId)
        {
            if (contractId <= 0) return NotFound();
            var contract = await _context.Contracts.FindAsync(contractId);
            if (contract != null && contract.JobApplicationId.HasValue)
            {
                await LoadCompanyDataAsync(contract.JobApplicationId.Value);
            }
            return View(new ContractLoginViewModel { ContractId = contractId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContractLogin(ContractLoginViewModel model)
        {
            var contract = await _context.Contracts.Include(c => c.JobApplication).FirstOrDefaultAsync(c => c.Id == model.ContractId);
            if (contract != null && contract.JobApplicationId.HasValue)
            {
                await LoadCompanyDataAsync(contract.JobApplicationId.Value);
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                if (contract == null || contract.JobApplication == null)
                {
                    ModelState.AddModelError("", "Contract not found.");
                    return View(model);
                }

                if (contract.JobApplication.MobileNumber == model.MobileNumber)
                {
                    // Successfully logged in via mobile
                    TempData["AuthenticatedContractId"] = model.ContractId;
                    return RedirectToAction("UploadContractRequirements", new { contractId = model.ContractId });
                }

                ModelState.AddModelError("", "The mobile number provided does not match our records.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during contract login for contractId {ContractId}", model.ContractId);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UploadContractRequirements(int contractId)
        {
            if (TempData["AuthenticatedContractId"] == null || (int)TempData["AuthenticatedContractId"] != contractId)
            {
                return RedirectToAction("ContractLogin", new { contractId });
            }
            // Keep authenticated for postback
            TempData.Keep("AuthenticatedContractId");

            var contract = await _context.Contracts
                .Include(c => c.JobApplication)
                .Include(c => c.ContractType)
                .ThenInclude(ct => ct.DocumentRequirements)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract != null && contract.JobApplicationId.HasValue)
            {
                await LoadCompanyDataAsync(contract.JobApplicationId.Value);
            }

            var model = new UploadContractRequirementsViewModel { ContractId = contractId };
            
            if (contract?.ContractType?.DocumentRequirements != null)
            {
                var isRtl = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("ar");
                model.DynamicRequirements = contract.ContractType.DocumentRequirements.Select(r => new DynamicRequirementViewModel
                {
                    RequirementId = r.Id,
                    RequirementName = isRtl ? r.NameNative : r.NameEn,
                    IsRequired = r.IsRequired
                }).ToList();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadContractRequirements(UploadContractRequirementsViewModel model)
        {
            if (TempData["AuthenticatedContractId"] == null || (int)TempData["AuthenticatedContractId"] != model.ContractId)
            {
                return RedirectToAction("ContractLogin", new { contractId = model.ContractId });
            }
            TempData.Keep("AuthenticatedContractId");

            var contract = await _context.Contracts
                .Include(c => c.JobApplication)
                .Include(c => c.ContractType)
                .ThenInclude(ct => ct.DocumentRequirements)
                .FirstOrDefaultAsync(c => c.Id == model.ContractId);

            if (contract != null && contract.JobApplicationId.HasValue)
            {
                await LoadCompanyDataAsync(contract.JobApplicationId.Value);
            }

            if (model.DynamicRequirements != null)
            {
                for (int i = 0; i < model.DynamicRequirements.Count; i++)
                {
                    var req = model.DynamicRequirements[i];
                    if (req.IsRequired && (req.File == null || req.File.Length == 0))
                    {
                        ModelState.AddModelError($"DynamicRequirements[{i}].File", $"Please upload the required document: {req.RequirementName}");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                if (model.DynamicRequirements == null || !model.DynamicRequirements.Any())
                {
                    if (contract?.ContractType?.DocumentRequirements != null)
                    {
                        var isRtl = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("ar");
                        model.DynamicRequirements = contract.ContractType.DocumentRequirements.Select(r => new DynamicRequirementViewModel
                        {
                            RequirementId = r.Id,
                            RequirementName = isRtl ? r.NameNative : r.NameEn,
                            IsRequired = r.IsRequired
                        }).ToList();
                    }
                }
                return View(model);
            }

            try
            {
                if (contract == null || contract.JobApplication == null)
                    return NotFound();

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/attachments");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Save Signed Contract
                if (model.SignedContract != null && model.SignedContract.Length > 0)
                {
                    var fileExtension1 = Path.GetExtension(model.SignedContract.FileName);
                    var fileName1 = $"{Guid.NewGuid()}{fileExtension1}";
                    var filePath1 = Path.Combine(uploadsFolder, fileName1);

                    using (var fileStream = new FileStream(filePath1, FileMode.Create))
                    {
                        await model.SignedContract.CopyToAsync(fileStream);
                    }

                    contract.SignedContractUrl = fileName1;
                }

                // Save National ID
                if (model.NationalIdFile != null && model.NationalIdFile.Length > 0)
                {
                    var fileExtension2 = Path.GetExtension(model.NationalIdFile.FileName);
                    var fileName2 = $"{Guid.NewGuid()}{fileExtension2}";
                    var filePath2 = Path.Combine(uploadsFolder, fileName2);

                    using (var fileStream = new FileStream(filePath2, FileMode.Create))
                    {
                        await model.NationalIdFile.CopyToAsync(fileStream);
                    }

                    contract.NationalIdUrl = fileName2;
                }

                // Save Dynamic Requirements
                if (model.DynamicRequirements != null)
                {
                    foreach (var req in model.DynamicRequirements)
                    {
                        if (req.File != null && req.File.Length > 0)
                        {
                            var ext = Path.GetExtension(req.File.FileName);
                            var fileName = $"{Guid.NewGuid()}{ext}";
                            var filePath = Path.Combine(uploadsFolder, fileName);
                            
                            using (var fs = new FileStream(filePath, FileMode.Create))
                            {
                                await req.File.CopyToAsync(fs);
                            }
                            
                            var attachment = new ContractAttachment
                            {
                                ContractId = contract.Id,
                                DocumentRequirementLookupId = req.RequirementId,
                                FileUrl = fileName,
                                CreatedOn = DateTime.Now
                            };
                            _context.ContractAttachments.Add(attachment);
                        }
                    }
                }

                contract.IsSigned = true;
                _context.Contracts.Update(contract);
                await _context.SaveChangesAsync();

                TempData.Remove("AuthenticatedContractId");
                return View("ContractRequirementsSubmitted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during uploading contract attachments for contractId {ContractId}", model.ContractId);
                ModelState.AddModelError("", "An error occurred while uploading your documents.");
                return View(model);
            }
        }
    }
}
