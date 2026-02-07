namespace SmartTimeCVs.Web.Core.Services
{
    /// <summary>
    /// Interface for interview scheduling operations
    /// </summary>
    public interface IInterviewSchedulingService
    {
        /// <summary>
        /// Schedule an interview for a candidate
        /// </summary>
        /// <param name="model">Schedule details from the form</param>
        /// <returns>Created interview schedule</returns>
        Task<InterviewSchedule> ScheduleInterviewAsync(ScheduleInterviewViewModel model);

        /// <summary>
        /// Get all interview schedules for a company
        /// </summary>
        /// <param name="companyId">Company identifier</param>
        /// <returns>List of scheduled interviews</returns>
        Task<List<InterviewScheduleListViewModel>> GetAllSchedulesAsync(string? companyId);

        /// <summary>
        /// Get a specific schedule by ID
        /// </summary>
        /// <param name="id">Schedule ID</param>
        /// <returns>Interview schedule or null</returns>
        Task<InterviewSchedule?> GetScheduleByIdAsync(int id);

        /// <summary>
        /// Get candidate details for scheduling modal
        /// </summary>
        /// <param name="jobApplicationId">Job application ID</param>
        /// <returns>ViewModel with candidate info pre-filled</returns>
        Task<ScheduleInterviewViewModel?> GetScheduleViewModelAsync(int jobApplicationId);

        /// <summary>
        /// Get schedule details for editing
        /// </summary>
        /// <param name="scheduleId">Schedule ID</param>
        /// <returns>ViewModel with schedule info pre-filled</returns>
        Task<ScheduleInterviewViewModel?> GetEditScheduleViewModelAsync(int scheduleId);

        /// <summary>
        /// Update an existing interview schedule
        /// </summary>
        /// <param name="model">Expanded schedule details</param>
        Task UpdateScheduleAsync(ScheduleInterviewViewModel model);
    }
}
