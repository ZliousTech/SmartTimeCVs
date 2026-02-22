using SmartTimeCVs.Web.Core.Enums;
using SmartTimeCVs.Web.Core.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartTimeCVs.Web.Core.Models
{
    public class JobOffer : BaseModel
    {
        [Key]
        public int Id { get; set; }

        public int JobApplicationId { get; set; }
        [ForeignKey("JobApplicationId")]
        public JobApplication JobApplication { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OfferedSalary { get; set; }

        public string? Currency { get; set; }

        public string? Allowances { get; set; }

        public string? WorkingHours { get; set; }

        public DateTime StartDate { get; set; }

        public string? ProbationPeriod { get; set; }

        public string? Benefits { get; set; }

        public string? ManagerName { get; set; }

        public string? Department { get; set; }

        public string? SenderName { get; set; }

        public string? SenderEmail { get; set; }

        public string? Notes { get; set; }

        public JobOfferStatus Status { get; set; } = JobOfferStatus.Draft;

        public DateTime? SentOn { get; set; }

        public DateTime? RespondedOn { get; set; }
    }
}
