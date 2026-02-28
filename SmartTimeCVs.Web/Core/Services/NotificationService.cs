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
        private readonly IConfiguration _configuration;

        public NotificationService(
            IEmailService emailService,
            ISmsService smsService,
            ApplicationDbContext context,
            ILogger<NotificationService> logger,
            IConfiguration configuration)
        {
            _emailService = emailService;
            _smsService = smsService;
            _context = context;
            _logger = logger;
            _configuration = configuration;
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
        public async Task<bool> SendJobOfferNotificationAsync(JobOffer offer, NotificationType notificationType)
        {
            if (offer.JobApplication == null)
            {
                // Ensure JobApplication is loaded
                offer.JobApplication = await _context.JobApplication.FindAsync(offer.JobApplicationId);
            }

            if (offer.JobApplication == null)
            {
                _logger.LogError("Job application not found for offer {OfferId}", offer.Id);
                return false;
            }

            var emailSuccess = true;
            var smsSuccess = true;

            var subject = "Job Offer - SmartTime CVs";
            var emailBody = BuildJobOfferEmailBody(offer);
            var smsMessage = BuildJobOfferSmsMessage(offer);

            if (notificationType == NotificationType.Email || notificationType == NotificationType.Both)
            {
                if (!string.IsNullOrEmpty(offer.JobApplication.Email))
                {
                    emailSuccess = await _emailService.SendEmailAsync(
                        offer.JobApplication.Email,
                        subject,
                        emailBody,
                        offer.SenderEmail,
                        offer.SenderName
                    );
                }
            }

            if (notificationType == NotificationType.SMS || notificationType == NotificationType.Both)
            {
                if (!string.IsNullOrEmpty(offer.JobApplication.MobileNumber))
                {
                    smsSuccess = await _smsService.SendSmsAsync(
                        offer.JobApplication.MobileNumber,
                        smsMessage
                    );
                }
            }

            return (notificationType == NotificationType.Both) 
                ? (emailSuccess && smsSuccess) 
                : (emailSuccess || smsSuccess);
        }

        private string BuildJobOfferEmailBody(JobOffer offer)
        {
            var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5090";
            var actionUrl = $"{baseUrl}/Biography";
            var companyName = string.IsNullOrEmpty(offer.SenderName) ? "SmartTime CVs" : offer.SenderName;
            var expiryDate = offer.CreatedOn?.AddDays(7).ToString("dd MMM yyyy") ?? DateTime.Now.AddDays(7).ToString("dd MMM yyyy");
            var managerName = string.IsNullOrEmpty(offer.ManagerName) ? "Management" : offer.ManagerName;
            var department = string.IsNullOrEmpty(offer.Department) ? "" : " / " + offer.Department;
            var allowances = string.IsNullOrEmpty(offer.Allowances) ? "N/A" : offer.Allowances;
            var probation = string.IsNullOrEmpty(offer.ProbationPeriod) ? "N/A" : offer.ProbationPeriod;
            var hours = string.IsNullOrEmpty(offer.WorkingHours) ? "N/A" : offer.WorkingHours;
            var benefits = string.IsNullOrEmpty(offer.Benefits) ? "N/A" : offer.Benefits;
            var address = string.IsNullOrEmpty(offer.JobApplication?.Address) ? "N/A" : offer.JobApplication.Address;

             return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f4f7f6; margin: 0; padding: 0; }}
                    .container {{ max-width: 800px; margin: 40px auto; background: #fff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); padding: 40px; }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; text-decoration: underline; }}
                    .content {{ color: #333; font-size: 15px; }}
                    .content p {{ margin-bottom: 15px; }}
                    .content ul {{ margin-bottom: 15px; margin-top: 10px; }}
                    .btn-primary {{ background-color: #0d6efd; color: #ffffff !important; padding: 14px 32px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block; font-size: 16px; margin-top: 20px; box-shadow: 0 2px 4px rgba(13, 110, 253, 0.4); transition: transform 0.2s; }}
                    .signature-block {{ margin-top: 40px; }}
                    .signature-line {{ width: 250px; border-bottom: 1px solid #333; margin-bottom: 5px; display: inline-block; }}
                    .action-section {{ text-align: center; margin-top: 50px; padding-top: 30px; border-top: 1px solid #eee; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>JOB OFFER LETTER</h1>
                    </div>
                    <div class='content'>
                        <p><strong>Date:</strong> {DateTime.Now.ToString("dd MMM yyyy")}</p>
                        <br/>
                        <p><strong>Candidate Name:</strong> {offer.JobApplication?.FullName}<br/>
                        <strong>Address:</strong> {address}</p>
                        
                        <p>Dear {offer.JobApplication?.FullName},</p>
                        
                        <p>We are pleased to offer you the position of <strong>{offer.JobApplication?.JobTitle}</strong> at <strong>{companyName}</strong>, reporting to <strong>{managerName}</strong>{department}.</p>
                        
                        <p>Your employment will commence on <strong>{offer.StartDate.ToString("dd MMM yyyy")}</strong>. The compensation package for this position is as follows:</p>
                        
                        <ul>
                            <li><strong>Basic Salary:</strong> {offer.OfferedSalary.ToString("N2")} {offer.Currency ?? "JOD"} per month</li>
                            <li><strong>Allowances (if any):</strong> {allowances}</li>
                            <li><strong>Probation Period:</strong> {probation}</li>
                            <li><strong>Working Hours:</strong> {hours}</li>
                            <li><strong>Other Benefits:</strong> {benefits}</li>
                        </ul>
                        
                        <p>This offer is contingent upon successful completion of all pre-employment requirements and signing the official employment contract.</p>
                        
                        <p>Please confirm your acceptance of this offer by signing below and returning a copy of this letter by <strong>{expiryDate}</strong>.</p>
                        
                        <p>We look forward to welcoming you to our team.</p>
                        
                        <p>Sincerely,</p>
                        
                        <div class='signature-block'>
                            <div class='signature-line'></div><br/>
                            <strong>{offer.SenderName ?? offer.ManagerName ?? "HR Department"}</strong><br/>
                            {companyName}
                        </div>
                        
                        <div class='action-section'>
                            <p>To securely view and electronically respond to this offer, please click the button below:</p>
                            <a href='{actionUrl}' class='btn-primary'>View & Respond to Offer</a>
                        </div>
                    </div>
                </div>
            </body>
            </html>";
        }

        private static string BuildJobOfferSmsMessage(JobOffer offer)
        {
            return $"Congratulations {offer.JobApplication.FullName}! We are pleased to offer you the position. Please check your email for details. - SmartTime CVs";
        }
    }
}
