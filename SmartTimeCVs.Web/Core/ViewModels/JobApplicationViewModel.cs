using SmartTimeCVs.Web.Core.Enums;

public class JobApplicationViewModel : BaseModel
{
    #region Basic Information.

    public int Id { get; set; }

    [Display(Name = "Profile Image")]
    public IFormFile? ImageFile { get; set; }
    public string? ImageUrl { get; set; }

    [Required]
    [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "Full Name")]
    [Remote("AllowName", "JobApplication", AdditionalFields = "Id", ErrorMessage = Errors.Duplicated)]
    public string FullName { get; set; } = null!;

    [Required]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(500, ErrorMessage = Errors.MaxLength), Display(Name = "Place Of Birth")]
    public string PlaceOfBirth { get; set; } = null!;

    [Display(Name = "Gender")]
    public int GenderId { get; set; } = (int)GenderTypeEnum.Male;
    public GenderType? Gender { get; set; }

    [MaxLength(500, ErrorMessage = Errors.MaxLength), Display(Name = "Address")]
    public string Address { get; set; } = null!;

    [MaxLength(18, ErrorMessage = Errors.MaxLength), Display(Name = "NationalID")]
    public string NationalID { get; set; } = null!;

    [MaxLength(15, ErrorMessage = Errors.MaxLength), Display(Name = "MobileNumber")]
    public string MobileNumber { get; set; } = null!;

    [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "Nationality")]
    public string Nationality { get; set; } = null!;

    [Display(Name = "MaritalStatus")]
    public int MaritalStatusId { get; set; } = (int)MaritalStatusTypeEnum.Single;
    public MaritalStatusType? MaritalStatus { get; set; }

    #endregion Basic Information.

    #region Education.

    [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "High School Name")]
    public string HighSchoolName { get; set; } = null!;

    [Display(Name = "High School Graduation Year")]
    public int HighSchoolGraduationYear { get; set; }

    public List<UniversityViewModel> Universities { get; set; } = new();

    #endregion Education.

    #region Language & Skills.

    [Display(Name = "English Level")]
    public int EnglishLevelId { get; set; }
    public LevelType? EnglishLevel { get; set; }

    [MaxLength(50, ErrorMessage = Errors.MaxLength), Display(Name = "Other Language")]
    public string OtherLanguage { get; set; } = null!;

    [Display(Name = "Other Language Level")]
    public int OtherLanguageLevelId { get; set; }
    public LevelType? OtherLanguageLevel { get; set; }

    [Display(Name = "Computer Skills Level")]
    public int ComputerSkillsLevelId { get; set; }
    public LevelType? ComputerSkillsLevel { get; set; }

    public List<CourseViewModel> Courses { get; set; } = new();

    #endregion Language & Skills.

    #region Current Employment Information.

    [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "Current Employer")]
    public string CurrentEmployerName { get; set; } = null!;

    [MaxLength(500, ErrorMessage = Errors.MaxLength), Display(Name = "Employer Address")]
    public string CurrentEmployerAddress { get; set; } = null!;

    [MaxLength(2500, ErrorMessage = Errors.MaxLength), Display(Name = "Job Description")]
    public string CurrentJobDescription { get; set; } = null!;

    [MaxLength(250, ErrorMessage = Errors.MaxLength), Display(Name = "Work Period")]
    public string CurrentWorkPeriod { get; set; } = null!;

    [Display(Name = "Current Salary")]
    public decimal? CurrentSalary { get; set; }

    [MaxLength(500, ErrorMessage = Errors.MaxLength), Display(Name = "Reason for Leaving")]
    public string ReasonForLeavingCurrent { get; set; } = null!;

    [Display(Name = "Ready To Join From")]
    public DateTime? ReadyToJoinFrom { get; set; }

    #endregion Current Employment Information.

    #region Previous Work Experience.

    public List<WorkExperienceViewModel> WorkExperiences { get; set; } = new();

    #endregion Previous Work Experience.

    #region Application Information.

    [MaxLength(100, ErrorMessage = Errors.MaxLength), Display(Name = "Applying For")]
    public string? ApplyingFor { get; set; }

    [MaxLength(500, ErrorMessage = Errors.MaxLength), Display(Name = "Admin Feedback")]
    public string? AdminFeedback { get; set; }

    public bool IsAccepted { get; set; } = false;

    #endregion Application Information.

    #region Attachment.

    [Display(Name = "Attachment")]
    public IFormFile? AttachmentFile { get; set; }
    public string? AttachmentUrl { get; set; }

    #endregion Attachment.

    public string? CompanyId { get; set; }
}
