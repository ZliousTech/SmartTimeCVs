namespace SmartTimeCVs.Web.Core.Services
{
    /// <summary>
    /// Interface for sending notifications (orchestrates email and SMS)
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Send interview notification to candidate
        /// </summary>
        /// <param name="schedule">The interview schedule details</param>
        /// <param name="notificationType">How to notify (Email, SMS, or Both)</param>
        /// <returns>True if notification sent successfully</returns>
        Task<bool> SendInterviewNotificationAsync(InterviewSchedule schedule, NotificationType notificationType);
    }
}
