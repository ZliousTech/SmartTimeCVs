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
        /// <param name="replyTo">Optional Reply-To email address</param>
        /// <param name="senderDisplayName">Optional Display Name for Sender</param>
        Task<bool> SendEmailAsync(string to, string subject, string body, string? replyTo = null, string? senderDisplayName = null);
    }
}
