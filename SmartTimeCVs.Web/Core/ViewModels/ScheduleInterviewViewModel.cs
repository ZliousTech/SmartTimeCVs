namespace SmartTimeCVs.Web.Core.ViewModels
{
    /// <summary>
    /// ViewModel for scheduling an interview/test from the modal popup
    /// </summary>
    public class ScheduleInterviewViewModel
    {
        /// <summary>
        /// Schedule ID (for editing)
        /// </summary>
        public int? Id { get; set; }

        public int JobApplicationId { get; set; }

        // Candidate info (for display purposes)
        public string? CandidateName { get; set; }
        public string? CandidateEmail { get; set; }
        public string? CandidatePhone { get; set; }
        public string? JobTitle { get; set; }

        // Interview Details

        [Required(ErrorMessage = "Interview date is required")]
        [Display(Name = "Interview Date")]
        [DataType(DataType.Date)]
        public DateTime? InterviewDate { get; set; }

        [Required(ErrorMessage = "Interview time is required")]
        [Display(Name = "Interview Time")]
        [DataType(DataType.Time)]
        public TimeSpan? InterviewTime { get; set; }

        [Required(ErrorMessage = "Interview location/link is required")]
        [MaxLength(500)]
        [Display(Name = "Interview Location / Link")]
        public string? InterviewLocation { get; set; }

        // Test Details (optional)
        [Display(Name = "Test Date")]
        [DataType(DataType.Date)]
        public DateTime? TestDate { get; set; }

        [Display(Name = "Test Time")]
        [DataType(DataType.Time)]
        public TimeSpan? TestTime { get; set; }

        [MaxLength(500)]
        [Display(Name = "Test Location")]
        public string? TestLocation { get; set; }

        // Notification
        [Required(ErrorMessage = "Please select notification type")]
        [Display(Name = "Notification Type")]
        public NotificationType NotificationType { get; set; } = NotificationType.Email;

        // Notes
        [MaxLength(1000)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}
