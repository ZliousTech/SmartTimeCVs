namespace SmartTimeCVs.Web.Core.ViewModels
{
    /// <summary>
    /// ViewModel for displaying interview schedules in a list/table
    /// </summary>
    public class InterviewScheduleListViewModel
    {
        public int Id { get; set; }
        public int JobApplicationId { get; set; }

        // Candidate Info
        public string? CandidateName { get; set; }
        public string? CandidateEmail { get; set; }
        public string? CandidatePhone { get; set; }
        public string? CandidateImageUrl { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string? JobTitle { get; set; }

        // Interview Details
        public DateTime InterviewDate { get; set; }
        public TimeSpan InterviewTime { get; set; }
        public string? InterviewLocation { get; set; }

        // Test Details
        public DateTime? TestDate { get; set; }
        public TimeSpan? TestTime { get; set; }
        public string? TestLocation { get; set; }

        // Status & Notification
        public ScheduleStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public NotificationType NotificationType { get; set; }
        public bool IsNotificationSent { get; set; }
        public DateTime? NotificationSentAt { get; set; }

        // Notes
        public string? Notes { get; set; }

        // Timestamps
        public DateTime? CreatedOn { get; set; }

        // Helper properties for display
        public string InterviewDateTime => $"{InterviewDate:yyyy-MM-dd} {InterviewTime:hh\\:mm}";
        public string? TestDateTime => TestDate.HasValue && TestTime.HasValue 
            ? $"{TestDate:yyyy-MM-dd} {TestTime:hh\\:mm}" 
            : null;
    }
}
