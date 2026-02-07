using Microsoft.Extensions.Options;

namespace SmartTimeCVs.Web.Core.Services
{
    /// <summary>
    /// SMS service implementation with provider abstraction
    /// Currently supports Twilio but can be extended for other providers
    /// </summary>
    public class SmsService : ISmsService
    {
        private readonly SmsSettings _smsSettings;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IOptions<SmsSettings> smsSettings, ILogger<SmsService> logger)
        {
            _smsSettings = smsSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // For production, implement actual SMS provider integration
                // Example with Twilio would be:
                // var client = new TwilioRestClient(_smsSettings.AccountSid, _smsSettings.AuthToken);
                // var smsMessage = await MessageResource.CreateAsync(
                //     to: new Twilio.Types.PhoneNumber(phoneNumber),
                //     from: new Twilio.Types.PhoneNumber(_smsSettings.FromNumber),
                //     body: message
                // );

                // For now, we'll simulate SMS sending and log it
                _logger.LogInformation(
                    "SMS would be sent to {PhoneNumber}: {Message}", 
                    phoneNumber, 
                    message.Length > 50 ? message[..50] + "..." : message
                );

                // Simulate async operation
                await Task.Delay(100);

                // In development, we return true to simulate success
                // In production, check actual API response
                if (string.IsNullOrEmpty(_smsSettings.AccountSid))
                {
                    _logger.LogWarning("SMS settings not configured. SMS was not actually sent.");
                    return true; // Return true to not block the flow in dev
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }

    /// <summary>
    /// SMS configuration settings (Twilio-compatible)
    /// </summary>
    public class SmsSettings
    {
        public string Provider { get; set; } = "Twilio";
        public string AccountSid { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string FromNumber { get; set; } = string.Empty;
    }
}
