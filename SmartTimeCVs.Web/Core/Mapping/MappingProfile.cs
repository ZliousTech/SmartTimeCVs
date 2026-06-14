namespace SmartTimeCVs.Web.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // JobApplication
            CreateMap<JobApplication, JobApplicationViewModel>()
            .ForMember(dest => dest.WorkExperiences, opt => opt.MapFrom(src => src.WorkExperience))
            .ForMember(dest => dest.Universities, opt => opt.MapFrom(src => src.Univesity))
            .ForMember(dest => dest.Courses, opt => opt.MapFrom(src => src.Course))
            .ReverseMap()
            .ForMember(dest => dest.WorkExperience, opt => opt.MapFrom(src => src.WorkExperiences))
            .ForMember(dest => dest.Univesity, opt => opt.MapFrom(src => src.Universities))
            .ForMember(dest => dest.Course, opt => opt.MapFrom(src => src.Courses))
            // Owned by sync / senior pipeline: do not overwrite from form defaults (stays false until pull sets true).
            .ForMember(dest => dest.IsImported, opt => opt.Ignore())
            .ForMember(dest => dest.IsDraft, opt => opt.Ignore())
            .ForMember(dest => dest.LastCompletedStep, opt => opt.Ignore());

            // University
            CreateMap<University, UniversityViewModel>().ReverseMap();

            // Course
            CreateMap<Course, CourseViewModel>().ReverseMap();

            // WorkExperienc
            CreateMap<WorkExperience, WorkExperienceViewModel>().ReverseMap();

            // AttachmentFiles
            CreateMap<AttachmentFile, AttachmentFileViewModel>().ReverseMap();
        }
    }
}