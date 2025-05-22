namespace SmartTimeCVs.Web.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // JobApplication
            CreateMap<JobApplication, JobApplicationViewModel>()
            .ForMember(dest => dest.WorkExperiences, opt => opt.MapFrom(src => src.WorkExperience))
            .ReverseMap()
            .ForMember(dest => dest.WorkExperience, opt => opt.MapFrom(src => src.WorkExperiences));

            // WorkExperiences
            CreateMap<WorkExperience, WorkExperienceViewModel>().ReverseMap();
        }
    }
}