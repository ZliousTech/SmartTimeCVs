namespace SmartTimeCVs.Web.Core.Services
{
    /// <summary>
    /// Notification service that orchestrates email and SMS notifications
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IEmailService emailService,
            ISmsService smsService,
            ApplicationDbContext context,
            ILogger<NotificationService> logger)
        {
            _emailService = emailService;
            _smsService = smsService;
            _context = context;
            _logger = logger;
        }

        public async Task<bool> SendInterviewNotificationAsync(InterviewSchedule schedule, NotificationType notificationType)
        {
            var jobApplication = await _context.JobApplication
                .FirstOrDefaultAsync(j => j.Id == schedule.JobApplicationId);

            if (jobApplication == null)
            {
                _logger.LogError("Job application not found for schedule {ScheduleId}", schedule.Id);
                return false;
            }

            var emailSuccess = true;
            var smsSuccess = true;

            // Build notification messages
            var subject = "Interview Scheduled - SmartTime CVs";
            var emailBody = BuildEmailBody(jobApplication, schedule);
            var smsMessage = BuildSmsMessage(jobApplication, schedule);

            // Send based on notification type
            if (notificationType == NotificationType.Email || notificationType == NotificationType.Both)
            {
                if (!string.IsNullOrEmpty(jobApplication.Email))
                {
                    emailSuccess = await _emailService.SendEmailAsync(
                        jobApplication.Email,
                        subject,
                        emailBody
                    );
                }
            }

            if (notificationType == NotificationType.SMS || notificationType == NotificationType.Both)
            {
                if (!string.IsNullOrEmpty(jobApplication.MobileNumber))
                {
                    smsSuccess = await _smsService.SendSmsAsync(
                        jobApplication.MobileNumber,
                        smsMessage
                    );
                }
            }

            // Update schedule notification status
            var success = (notificationType == NotificationType.Both) 
                ? (emailSuccess && smsSuccess) 
                : (emailSuccess || smsSuccess);

            if (success)
            {
                schedule.IsNotificationSent = true;
                schedule.NotificationSentAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return success;
        }

        private static string BuildEmailBody(JobApplication application, InterviewSchedule schedule)
        {
            var testSection = schedule.TestDate.HasValue
                ? $@"
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Test Date:</strong></td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'>{schedule.TestDate:dddd, MMMM dd, yyyy}</td>
                </tr>
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Test Time:</strong></td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'>{schedule.TestTime:hh\\:mm}</td>
                </tr>
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Test Location:</strong></td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'>{schedule.TestLocation ?? "TBD"}</td>
                </tr>"
                : "";

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #198754, #20c997); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #fff; padding: 30px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 10px 10px; }}
                    .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Interview Invitation</h1>
                    </div>
                    <div class='content'>
                        <p>Dear <strong>{application.FullName}</strong>,</p>
                        <p>We are pleased to inform you that you have been selected for an interview for the position of <strong>{application.JobTitle ?? application.ApplyingFor}</strong>.</p>
                        
                        <h3 style='color: #198754;'>Interview Details</h3>
                        <table>
                            <tr>
                                <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Date:</strong></td>
                                <td style='padding: 10px; border-bottom: 1px solid #eee;'>{schedule.InterviewDate:dddd, MMMM dd, yyyy}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Time:</strong></td>
                                <td style='padding: 10px; border-bottom: 1px solid #eee;'>{schedule.InterviewTime:hh\\:mm}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border-bottom: 1px solid #eee;'><strong>Location:</strong></td>
                                <td style='padding: 10px; border-bottom: 1px solid #eee;'>{schedule.InterviewLocation ?? "To be confirmed"}</td>
                            </tr>
                            {testSection}
                        </table>

                        {(string.IsNullOrEmpty(schedule.Notes) ? "" : $"<p><strong>Additional Notes:</strong> {schedule.Notes}</p>")}

                        <p style='margin-top: 20px;'>Please confirm your attendance by replying to this email.</p>
                        <p>We look forward to meeting you!</p>
                        
                        <p>Best regards,<br/>HR Team</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated message from SmartTime CVs</p>
                    </div>
                </div>
            </body>
            </html>";
        }

        private static string BuildSmsMessage(JobApplication application, InterviewSchedule schedule)
        {
            var testInfo = schedule.TestDate.HasValue
                ? $" Test: {schedule.TestDate:MM/dd} at {schedule.TestTime:hh\\:mm}."
                : "";

            return $"Hi {application.FullName}, you're invited for an interview on {schedule.InterviewDate:MM/dd/yyyy} at {schedule.InterviewTime:hh\\:mm}.{testInfo} Please confirm your attendance. - SmartTime CVs";
        }
    }
}
