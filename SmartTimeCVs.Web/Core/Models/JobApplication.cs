namespace SmartTimeCVs.Web.Core.Models
{
	[Index(nameof(FullName), IsUnique = true)]
	public class JobApplication : BaseModel
	{
		#region Basic Information.

		[Key]
		public int Id { get; set; }

		public string ImageUrl { get; set; } = null!;

		[MaxLength(250)]
		public string FullName { get; set; } = null!;

		public DateTime? DateOfBirth { get; set; }

		[MaxLength(500)]
		public string PlaceOfBirth { get; set; } = null!;

		public int GenderId { get; set; }
		public GenderType? Gender { get; set; }

		[MaxLength(500)]
		public string Address { get; set; } = null!;

		[MaxLength(18)]
		public string NationalID { get; set; } = null!;

		[MaxLength(15)]
		public string MobileNumber { get; set; } = null!;

		[MaxLength(250)]
		public string Nationality { get; set; } = null!;

		public int MaritalStatusId { get; set; }
		public MaritalStatusType? MaritalStatus { get; set; }

		#endregion Basic Information.

		#region Education.

		[MaxLength(250)]
		public string HighSchoolName { get; set; } = null!;

		public int HighSchoolGraduationYear { get; set; }

		public ICollection<University> Univesity { get; set; } = new List<University>();

		#endregion Education.

		#region Language & Skills.

		public int EnglishLevelId { get; set; }
		public LevelType? EnglishLevel { get; set; }

		[MaxLength(50)]
		public string? OtherLanguage { get; set; }

		public int? OtherLanguageLevelId { get; set; }
		public LevelType? OtherLanguageLevel { get; set; }

		public int ComputerSkillsLevelId { get; set; }
		public LevelType? ComputerSkillsLevel { get; set; }

		public ICollection<Course> Course { get; set; } = new List<Course>();

		#endregion Language & Skills.

		#region Current Employment Information.

		[MaxLength(250)]
		public string CurrentEmployerName { get; set; } = null!;

		[MaxLength(500)]
		public string CurrentEmployerAddress { get; set; } = null!;

		[MaxLength(2500)]
		public string CurrentJobDescription { get; set; } = null!;

		public decimal? CurrentSalary { get; set; }

		public DateTime? CurrentFrom { get; set; }

		public DateTime? CurrentTo { get; set; }


		[MaxLength(500)]
		public string ReasonForLeavingCurrent { get; set; } = null!;

		public DateTime? ReadyToJoinFrom { get; set; }

		#endregion Current Employment Information.

		#region Previous Work Experience.

		public ICollection<WorkExperience> WorkExperience { get; set; } = new List<WorkExperience>();

		#endregion Previous Work Experience.

		#region Application Information.

		[MaxLength(100)]
		public string? ApplyingFor { get; set; }

		[MaxLength(500)]
		public string? AdminFeedback { get; set; }

		public bool IsAccepted { get; set; } = false;

		#endregion Application Information.

		#region Attachment.
		public string? AttachmentUrl { get; set; }

		#endregion Attachment.

		public string? CompanyId { get; set; }
	}
}
