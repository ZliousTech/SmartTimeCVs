public class WorkExperienceViewModel : BaseModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "Employer Name")]
    public string EmployerName { get; set; } = null!;

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    [MaxLength(1000, ErrorMessage = Errors.MaxLength), Display(Name = "Job Description")]
    public string JobDescription { get; set; } = null!;

    [MaxLength(500, ErrorMessage = Errors.MaxLength), Display(Name = "Reason for Leaving")]
    public string ReasonForLeaving { get; set; } = null!;

    [Display(Name = "Attachment")]
    public IFormFile? AttachmentFile { get; set; }
    public string? AttachmentUrl { get; set; }

    #region Table Relations.

    public int? JobApplicationId { get; set; }

    public JobApplicationViewModel? JobApplication { get; set; }

    #endregion Table Relations.
}

