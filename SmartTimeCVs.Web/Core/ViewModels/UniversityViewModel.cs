namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class UniversityViewModel : BaseModel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "University / Institute Name")]
        public string UniversityName { get; set; } = null!;

        [Required]
        [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "Collage / Specialization")]
        public string Collage { get; set; } = null!;

        [Required]
        [Display(Name = "Graduation Year")]
        public int UniversityGraduationYear { get; set; }

        #region Table Relations.

        public int? JobApplicationId { get; set; }

        public JobApplicationViewModel? JobApplication { get; set; }

        #endregion Table Relations.
    }
}
