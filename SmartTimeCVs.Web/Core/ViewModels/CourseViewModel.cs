namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class CourseViewModel : BaseModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "Course Name")]
        public string CourseName { get; set; } = null!;

        [Required]
        [MaxLength(500, ErrorMessage = Errors.MaxLength), Display(Name = "Course Address")]
        public string CourseAddress { get; set; } = null!;

        [Required]
        [Display(Name = "Date From")]
        public DateTime? From { get; set; }

        [Required]
        [Display(Name = "Date To")]
        public DateTime? To { get; set; }

        #region Table Relations.

        public int? JobApplicationId { get; set; }

        public JobApplication? JobApplication { get; set; }

        #endregion Table Relations.
    }
}
