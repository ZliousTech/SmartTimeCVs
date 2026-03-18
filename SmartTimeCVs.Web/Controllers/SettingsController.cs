using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using SmartTimeCVs.Web.Core.Models;
using SmartTimeCVs.Web.Data;

namespace SmartTimeCVs.Web.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public SettingsController(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public IActionResult ManageLookups()
        {
            return View();
        }

        public async Task<IActionResult> ContractTypes()
        {
            var types = await _context.ContractTypes.ToListAsync();
            return View(types);
        }

        public async Task<IActionResult> AddContractType(int? id)
        {
            ViewBag.ContractCategories = await _context.ContractCategories.ToListAsync();
            ViewBag.DocumentRequirements = await _context.DocumentRequirementLookups.ToListAsync();
            
            if (id.HasValue)
            {
                var contractType = await _context.ContractTypes
                    .Include(c => c.DocumentRequirements)
                    .FirstOrDefaultAsync(c => c.Id == id.Value);
                    
                if (contractType == null)
                {
                    return NotFound();
                }
                
                ViewBag.SelectedDocumentRequirements = contractType.DocumentRequirements.Select(d => d.Id).ToList();
                return View("ContractTypeForm", contractType);
            }
            return View("ContractTypeForm", new ContractType());
        }

        public async Task<IActionResult> ContractCategories()
        {
            var categories = await _context.ContractCategories.ToListAsync();
            return View(categories);
        }

        public async Task<IActionResult> AddContractCategory(int? id)
        {
            if (id.HasValue)
            {
                var category = await _context.ContractCategories.FindAsync(id.Value);
                if (category == null)
                {
                    return NotFound();
                }
                return View("ContractCategoryForm", category);
            }
            return View("ContractCategoryForm", new ContractCategory());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveContractCategory(ContractCategory model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id == 0)
                {
                    model.CreatedOn = DateTime.Now;
                    _context.ContractCategories.Add(model);
                }
                else
                {
                    var existing = await _context.ContractCategories.FindAsync(model.Id);
                    if (existing != null)
                    {
                        existing.NameEn = model.NameEn;
                        existing.NameNative = model.NameNative;
                        existing.LastUpdatedOn = DateTime.Now;
                        _context.ContractCategories.Update(existing);
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ContractCategories));
            }
            return View("ContractCategoryForm", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContractCategory(int id)
        {
            var entity = await _context.ContractCategories.FindAsync(id);
            if (entity != null)
            {
                _context.ContractCategories.Remove(entity);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Not found" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveContractType(ContractType model, List<int> SelectedDocumentRequirements)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.Id == 0)
                    {
                        model.CreatedOn = DateTime.Now;
                        if (SelectedDocumentRequirements != null && SelectedDocumentRequirements.Any())
                        {
                            model.DocumentRequirements = await _context.DocumentRequirementLookups
                                .Where(r => SelectedDocumentRequirements.Contains(r.Id)).ToListAsync();
                        }
                        _context.ContractTypes.Add(model);
                    }
                    else
                    {
                        var existing = await _context.ContractTypes
                            .Include(c => c.DocumentRequirements)
                            .FirstOrDefaultAsync(c => c.Id == model.Id);
                            
                        if (existing != null)
                        {
                            existing.NameEn = model.NameEn;
                            existing.NameNative = model.NameNative;
                            existing.DescriptionEn = model.DescriptionEn;
                            existing.DescriptionNative = model.DescriptionNative;
                            existing.ContractFor = model.ContractFor;
                            existing.ClausesEn = model.ClausesEn;
                            existing.ClausesNative = model.ClausesNative;
                            existing.FirstPartyName = model.FirstPartyName;
                            existing.FirstPartyAddress = model.FirstPartyAddress;
                            existing.AuthorizedSignatory = model.AuthorizedSignatory;
                            existing.CommercialNumber = model.CommercialNumber;
                            existing.LastUpdatedOn = DateTime.Now;
                            
                            existing.DocumentRequirements.Clear();
                            if (SelectedDocumentRequirements != null && SelectedDocumentRequirements.Any())
                            {
                                var reqs = await _context.DocumentRequirementLookups
                                    .Where(r => SelectedDocumentRequirements.Contains(r.Id)).ToListAsync();
                                foreach (var req in reqs)
                                {
                                    existing.DocumentRequirements.Add(req);
                                }
                            }
                            
                            _context.ContractTypes.Update(existing);
                        }
                    }
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(ContractTypes));
                }

                // Log ModelState errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Content("ModelState Errors: " + string.Join(" | ", errors));
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message + " | Inner: " + ex.InnerException?.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContractType(int id)
        {
            var entity = await _context.ContractTypes.FindAsync(id);
            if (entity != null)
            {
                _context.ContractTypes.Remove(entity);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Not found" });
        }

        public async Task<IActionResult> DocumentRequirements()
        {
            var requirements = await _context.DocumentRequirementLookups.ToListAsync();
            return View(requirements);
        }

        public async Task<IActionResult> AddDocumentRequirement(int? id)
        {
            if (id.HasValue)
            {
                var requirement = await _context.DocumentRequirementLookups.FindAsync(id.Value);
                if (requirement == null)
                {
                    return NotFound();
                }
                return View("DocumentRequirementForm", requirement);
            }
            return View("DocumentRequirementForm", new DocumentRequirementLookup());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDocumentRequirement(DocumentRequirementLookup model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id == 0)
                {
                    model.CreatedOn = DateTime.Now;
                    _context.DocumentRequirementLookups.Add(model);
                }
                else
                {
                    var existing = await _context.DocumentRequirementLookups.FindAsync(model.Id);
                    if (existing != null)
                    {
                        existing.NameEn = model.NameEn;
                        existing.NameNative = model.NameNative;
                        existing.IsRequired = model.IsRequired;
                        existing.LastUpdatedOn = DateTime.Now;
                        _context.DocumentRequirementLookups.Update(existing);
                    }
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(DocumentRequirements));
            }
            return View("DocumentRequirementForm", model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDocumentRequirement(int id)
        {
            var entity = await _context.DocumentRequirementLookups.FindAsync(id);
            if (entity != null)
            {
                _context.DocumentRequirementLookups.Remove(entity);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Not found" });
        }
    }
}
