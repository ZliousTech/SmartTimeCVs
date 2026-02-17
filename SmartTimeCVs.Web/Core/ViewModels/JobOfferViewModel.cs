using SmartTimeCVs.Web.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class JobOfferViewModel
    {
        public int? Id { get; set; }

        [Required]
        public int JobApplicationId { get; set; }

        public string? CandidateName { get; set; }
        public string? JobTitle { get; set; }

        [Required(ErrorMessage = "Offered Salary is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Please enter a valid salary")]
        public decimal OfferedSalary { get; set; }

        public string? Allowances { get; set; }

        [Required(ErrorMessage = "Start Date is required")]
        public DateTime StartDate { get; set; }

        public string? ProbationPeriod { get; set; }
        public string? WorkingHours { get; set; }
        public string? Benefits { get; set; }
        public string? ManagerName { get; set; }
        public string? Department { get; set; }
        public string? Notes { get; set; }

        public JobOfferStatus Status { get; set; }
        public string? StatusString { get; set; }

        public DateTime? CreatedOn { get; set; }
        public DateTime? SentOn { get; set; }

        public NotificationType NotificationType { get; set; } = NotificationType.Email;
    }
}
