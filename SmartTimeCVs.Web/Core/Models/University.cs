namespace SmartTimeCVs.Web.Core.Models
{
    public class University : BaseModel
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(250)]
        public string UniversityName { get; set; } = null!;

        [MaxLength(250)]
        public string Collage { get; set; } = null!;

        public int UniversityGraduationYear { get; set; }

        #region Table Relations.

        public int? JobApplicationId { get; set; }

        public JobApplication? JobApplication { get; set; }

        #endregion Table Relations.
    }
}
