using SmartTimeCVs.Web.Core.Enums;
using SmartTimeCVs.Web.Core.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace SmartTimeCVs.Web.Core.Models
{
    /// <summary>
    /// Represents an interview and/or test schedule for a job applicant
    /// </summary>
    public class InterviewSchedule : BaseModel
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the job application
        /// </summary>
        public int JobApplicationId { get; set; }
        public JobApplication JobApplication { get; set; } = null!;

        #region Interview Details

        /// <summary>
        /// Date of the interview
        /// </summary>
        [Required]
        public DateTime InterviewDate { get; set; }

        /// <summary>
        /// Time of the interview
        /// </summary>
        [Required]
        public TimeSpan InterviewTime { get; set; }

        /// <summary>
        /// Location or meeting link for the interview
        /// </summary>
        [MaxLength(500)]
        public string? InterviewLocation { get; set; }

        #endregion Interview Details

        #region Test Details

        /// <summary>
        /// Date of the test (optional)
        /// </summary>
        public DateTime? TestDate { get; set; }

        /// <summary>
        /// Time of the test (optional)
        /// </summary>
        public TimeSpan? TestTime { get; set; }

        /// <summary>
        /// Location for the test
        /// </summary>
        [MaxLength(500)]
        public string? TestLocation { get; set; }

        #endregion Test Details

        #region Notification

        /// <summary>
        /// How the candidate should be notified
        /// </summary>
        public NotificationType NotificationType { get; set; }

        /// <summary>
        /// Whether the notification has been sent
        /// </summary>
        public bool IsNotificationSent { get; set; }

        /// <summary>
        /// When the notification was sent
        /// </summary>
        public DateTime? NotificationSentAt { get; set; }

        #endregion Notification

        #region Status

        /// <summary>
        /// Current status of this schedule
        /// </summary>
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;

        /// <summary>
        /// Additional notes about the schedule
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }

        #endregion Status

        /// <summary>
        /// Company identifier
        /// </summary>
        public string? CompanyId { get; set; }
    }
}
