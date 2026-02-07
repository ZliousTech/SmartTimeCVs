namespace SmartTimeCVs.Web.Core.Services
{
    /// <summary>
    /// Interface for sending SMS messages
    /// </summary>
    public interface ISmsService
    {
        /// <summary>
        /// Send an SMS message asynchronously
        /// </summary>
        /// <param name="phoneNumber">Recipient phone number</param>
        /// <param name="message">SMS message content</param>
        /// <returns>True if SMS sent successfully</returns>
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }
}
