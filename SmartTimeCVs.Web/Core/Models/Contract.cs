using System.ComponentModel.DataAnnotations;
using SmartTimeCVs.Web.Core.Models.Base;

namespace SmartTimeCVs.Web.Core.Models
{
    public class Contract : BaseModel
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(250)]
        public string CompanyName { get; set; } = "مدرسة Novara للغات";

        [MaxLength(250)]
        public string RepresentativeName { get; set; } = null!;

        [MaxLength(250)]
        public string RepresentativeTitle { get; set; } = null!;

        [MaxLength(500)]
        public string? CompanyAddress { get; set; }

        [MaxLength(100)]
        public string? CommercialNumber { get; set; }

        [MaxLength(250)]
        public string EmployeeName { get; set; } = null!;

        [MaxLength(20)]
        public string EmployeeNationalId { get; set; } = null!;

        [MaxLength(500)]
        public string EmployeeAddress { get; set; } = null!;

        [MaxLength(250)]
        public string JobTitle { get; set; } = null!;

        [MaxLength(100)]
        public string ContractDuration { get; set; } = null!;

        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }

        [MaxLength(100)]
        public string ProbationPeriod { get; set; } = null!;

        public decimal MonthlySalary { get; set; }

        public int SalaryPaymentDay { get; set; }

        // Optional relationship with JobApplication if we want to link it directly
        public int? JobApplicationId { get; set; }
        public JobApplication? JobApplication { get; set; }

        public int? ContractTypeId { get; set; }
        public ContractType? ContractType { get; set; }

        public bool IsSigned { get; set; } = false;

        public string? SignedContractUrl { get; set; }
        public string? NationalIdUrl { get; set; }
    }
}
