namespace SmartTimeCVs.Web.Core.Services
{
    /// <summary>
    /// Interface for sending emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send an email asynchronously
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML supported)</param>
        /// <returns>True if email sent successfully</returns>
        Task<bool> SendEmailAsync(string to, string subject, string body);
    }
}
