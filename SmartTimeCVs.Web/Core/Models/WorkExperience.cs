namespace SmartTimeCVs.Web.Core.Models
{
    public class WorkExperience : BaseModel
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(250)]
        public string EmployerName { get; set; } = null!;

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        [MaxLength(1000)]
        public string JobDescription { get; set; } = null!;

        [MaxLength(500)]
        public string ReasonForLeaving { get; set; } = null!;

        #region Table Relations.

        public int? JobApplicationId { get; set; }

        public JobApplication? JobApplication { get; set; }

        #endregion Table Relations.
    }
}
