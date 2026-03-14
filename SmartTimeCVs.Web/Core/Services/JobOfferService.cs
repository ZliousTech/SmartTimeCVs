using Microsoft.EntityFrameworkCore;
using SmartTimeCVs.Web.Core.Enums;
using SmartTimeCVs.Web.Core.Models;
using SmartTimeCVs.Web.Core.ViewModels;
using SmartTimeCVs.Web.Data;

namespace SmartTimeCVs.Web.Core.Services
{
    public class JobOfferService : IJobOfferService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobOfferService> _logger;
        private readonly INotificationService _notificationService;

        public JobOfferService(
            ApplicationDbContext context,
            ILogger<JobOfferService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<JobOfferViewModel?> GetOfferViewModelAsync(int jobApplicationId)
        {
            try
            {
                var application = await _context.JobApplication
                    .Include(j => j.JobOffer)
                    .FirstOrDefaultAsync(j => j.Id == jobApplicationId);

                if (application == null) return null;

                var model = new JobOfferViewModel
                {
                    JobApplicationId = application.Id,
                    CandidateName = application.FullName,
                    CandidateAddress = application.Address,
                    JobTitle = application.JobTitle,
                    StartDate = DateTime.Now.AddDays(14), // Default start date
                    ProbationPeriod = "3 Months",
                    WorkingHours = "9:00 AM - 5:00 PM"
                };

                if (application.JobOffer != null)
                {
                    model.Id = application.JobOffer.Id;
                    model.OfferedSalary = application.JobOffer.OfferedSalary;
                    model.Currency = application.JobOffer.Currency ?? "JOD";
                    model.Allowances = application.JobOffer.Allowances;
                    model.StartDate = application.JobOffer.StartDate;
                    model.ProbationPeriod = application.JobOffer.ProbationPeriod;
                    model.WorkingHours = application.JobOffer.WorkingHours;
                    model.Benefits = application.JobOffer.Benefits;
                    model.ManagerName = application.JobOffer.ManagerName;
                    model.Department = application.JobOffer.Department;
                    model.SenderName = application.JobOffer.SenderName;
                    model.SenderEmail = application.JobOffer.SenderEmail;
                    model.Notes = application.JobOffer.Notes;
                    model.Status = application.JobOffer.Status;
                    model.StatusString = application.JobOffer.Status.ToString();
                    model.CreatedOn = application.JobOffer.CreatedOn;
                    model.SentOn = application.JobOffer.SentOn;
                }

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job offer view model for application {AppId}", jobApplicationId);
                return null;
            }
        }

        public async Task<JobOffer?> GetOfferByApplicationIdAsync(int jobApplicationId)
        {
            return await _context.JobOffer
                .FirstOrDefaultAsync(o => o.JobApplicationId == jobApplicationId);
        }

        public async Task<JobOffer> SaveOfferAsync(JobOfferViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                JobOffer offer;

                if (model.Id.HasValue && model.Id.Value > 0)
                {
                    // Update existing
                    var offerEntity = await _context.JobOffer.FindAsync(model.Id.Value);
                    if (offerEntity == null) throw new Exception("Offer not found");
                    offer = offerEntity;

                    offer.OfferedSalary = model.OfferedSalary;
                    offer.Currency = model.Currency;
                    offer.Allowances = model.Allowances;
                    offer.StartDate = model.StartDate;
                    offer.ProbationPeriod = model.ProbationPeriod;
                    offer.WorkingHours = model.WorkingHours;
                    offer.Benefits = model.Benefits;
                    offer.ManagerName = model.ManagerName;
                    offer.Department = model.Department;
                    offer.SenderName = model.SenderName;
                    offer.SenderEmail = model.SenderEmail;
                    offer.Notes = model.Notes;
                    offer.LastUpdatedOn = DateTime.Now;

                    // If it was rejected or expired, maybe reset to Draft? 
                    // For now, keep logic simple.
                }
                else
                {
                    // Create new
                    offer = new JobOffer
                    {
                        JobApplicationId = model.JobApplicationId,
                        OfferedSalary = model.OfferedSalary,
                        Currency = model.Currency,
                        Allowances = model.Allowances,
                        StartDate = model.StartDate,
                        ProbationPeriod = model.ProbationPeriod,
                        WorkingHours = model.WorkingHours,
                        Benefits = model.Benefits,
                        ManagerName = model.ManagerName,
                        Department = model.Department,
                        SenderName = model.SenderName,
                        SenderEmail = model.SenderEmail,
                        Notes = model.Notes,
                        Status = JobOfferStatus.Draft,
                        CreatedOn = DateTime.Now
                    };
                    _context.JobOffer.Add(offer);
                }

                await _context.SaveChangesAsync();

                // Update JobApplication status if it's the first time
                var application = await _context.JobApplication.FindAsync(model.JobApplicationId);
                if (application != null && (application.CandidateStatus != CandidateStatus.Offered && application.CandidateStatus != CandidateStatus.Hired))
                {
                    // We don't change status just by creating a draft, usually.
                    // But if requirements say so, we can.
                    // Let's wait until "Sent" to change status to Offered.
                }

                await transaction.CommitAsync();
                return offer;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error saving job offer for application {AppId}", model.JobApplicationId);
                throw;
            }
        }

        public async Task<bool> SendOfferAsync(int jobOfferId, NotificationType notificationType, string? baseUrl = null)
        {
            try
            {
                var offer = await _context.JobOffer
                    .Include(o => o.JobApplication)
                    .FirstOrDefaultAsync(o => o.Id == jobOfferId);

                if (offer == null) return false;

                // 1. Update Offer Status
                offer.Status = JobOfferStatus.Sent;
                offer.SentOn = DateTime.Now;
                offer.LastUpdatedOn = DateTime.Now;

                // 2. Update Candidate Status
                if (offer.JobApplication != null)
                {
                    offer.JobApplication.CandidateStatus = CandidateStatus.Offered;
                }

                await _context.SaveChangesAsync();

                // 3. Send Notification in the background (fire-and-forget)
                // This prevents the email sending from blocking the response
                // and avoids session/context loss during long operations
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendJobOfferNotificationAsync(offer, notificationType, baseUrl);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Background email sending failed for offer {OfferId}. The offer was saved successfully but the notification may not have been delivered.", jobOfferId);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending job offer {OfferId}", jobOfferId);
                return false;
            }
        }

        public async Task<bool> RespondToOfferAsync(int jobOfferId, bool accepted)
        {
            try
            {
                var offer = await _context.JobOffer
                    .Include(o => o.JobApplication)
                    .FirstOrDefaultAsync(o => o.Id == jobOfferId);

                if (offer == null) return false;

                offer.Status = accepted ? JobOfferStatus.Accepted : JobOfferStatus.Rejected;
                offer.RespondedOn = DateTime.Now;
                offer.LastUpdatedOn = DateTime.Now;

                if (offer.JobApplication != null)
                {
                    offer.JobApplication.CandidateStatus = accepted ? CandidateStatus.Hired : CandidateStatus.Rejected;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to job offer {OfferId}", jobOfferId);
                return false;
            }
        }

        public async Task<bool> ValidateCandidateMobileAsync(int offerId, string mobileNumber)
        {
            var offer = await _context.JobOffer
                .Include(o => o.JobApplication)
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null || offer.JobApplication == null) 
                return false;

            if (string.IsNullOrWhiteSpace(offer.JobApplication.MobileNumber) || string.IsNullOrWhiteSpace(mobileNumber))
                return false;

            // Simple validation: ignoring spaces, dashes, parentheses
            var storedMobile = new string(offer.JobApplication.MobileNumber.Where(char.IsDigit).ToArray());
            var inputMobile = new string(mobileNumber.Where(char.IsDigit).ToArray());

            return storedMobile == inputMobile && storedMobile.Length > 0;
        }
    }
}
