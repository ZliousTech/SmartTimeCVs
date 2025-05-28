namespace SmartTimeCVs.Web.Core.Models
{
    public class Course : BaseModel
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(250)]
        public string CourseName { get; set; } = null!;

        [MaxLength(500)]
        public string CourseAddress { get; set; } = null!;

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        #region Table Relations.

        public int? JobApplicationId { get; set; }

        public JobApplication? JobApplication { get; set; }

        #endregion Table Relations.
    }
}
