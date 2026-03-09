using SmartTimeCVs.Web.Core.Models;
using SmartTimeCVs.Web.Core.ViewModels;

namespace SmartTimeCVs.Web.Core.Services
{
    public interface IJobOfferService
    {
        Task<JobOfferViewModel?> GetOfferViewModelAsync(int jobApplicationId);
        Task<JobOffer?> GetOfferByApplicationIdAsync(int jobApplicationId);
        Task<JobOffer> SaveOfferAsync(JobOfferViewModel model);
        Task<bool> SendOfferAsync(int jobOfferId, NotificationType notificationType, string? baseUrl = null);
        Task<bool> RespondToOfferAsync(int jobOfferId, bool accepted);
        Task<bool> ValidateCandidateMobileAsync(int offerId, string mobileNumber);
    }
}
