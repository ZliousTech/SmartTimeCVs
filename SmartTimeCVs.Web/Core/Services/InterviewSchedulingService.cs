namespace SmartTimeCVs.Web.Core.Services
{
    /// <summary>
    /// Service for managing interview scheduling operations
    /// </summary>
    public class InterviewSchedulingService : IInterviewSchedulingService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<InterviewSchedulingService> _logger;

        public InterviewSchedulingService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<InterviewSchedulingService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<InterviewSchedule> ScheduleInterviewAsync(ScheduleInterviewViewModel model)
        {
            // Create the schedule entity
            var schedule = new InterviewSchedule
            {
                JobApplicationId = model.JobApplicationId,
                InterviewDate = model.InterviewDate!.Value,
                InterviewTime = model.InterviewTime!.Value,
                InterviewLocation = model.InterviewLocation,
                TestDate = model.TestDate,
                TestTime = model.TestTime,
                TestLocation = model.TestLocation,
                NotificationType = model.NotificationType,
                Notes = model.Notes,
                Status = ScheduleStatus.Scheduled,
                CreatedOn = DateTime.Now,
                CompanyId = GlobalVariablesService.CompanyId
            };

            // Add to database
            _context.InterviewSchedule.Add(schedule);

            // Update job application status
            var jobApplication = await _context.JobApplication
                .FirstOrDefaultAsync(j => j.Id == model.JobApplicationId);

            if (jobApplication != null)
            {
                jobApplication.CandidateStatus = CandidateStatus.InterviewScheduled;
                jobApplication.LastUpdatedOn = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Send notification
            try
            {
                await _notificationService.SendInterviewNotificationAsync(schedule, model.NotificationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification for schedule {ScheduleId}", schedule.Id);
                // Don't throw - schedule is saved, notification can be retried
            }

            _logger.LogInformation(
                "Interview scheduled for JobApplication {JobApplicationId} on {InterviewDate}",
                model.JobApplicationId,
                model.InterviewDate
            );

            return schedule;
        }

        public async Task<List<InterviewScheduleListViewModel>> GetAllSchedulesAsync(string? companyId)
        {
            var schedules = await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .Where(s => s.CompanyId == companyId && !s.IsDeleted)
                .OrderByDescending(s => s.InterviewDate)
                .ThenBy(s => s.InterviewTime)
                .AsNoTracking()
                .ToListAsync();

            return schedules.Select(s => new InterviewScheduleListViewModel
            {
                Id = s.Id,
                JobApplicationId = s.JobApplicationId,
                CandidateName = s.JobApplication?.FullName,
                CandidateEmail = s.JobApplication?.Email,
                CandidatePhone = s.JobApplication?.MobileNumber,
                CandidateImageUrl = s.JobApplication?.ImageUrl,
                JobTitle = s.JobApplication?.JobTitle ?? s.JobApplication?.ApplyingFor,
                InterviewDate = s.InterviewDate,
                InterviewTime = s.InterviewTime,
                InterviewLocation = s.InterviewLocation,
                TestDate = s.TestDate,
                TestTime = s.TestTime,
                TestLocation = s.TestLocation,
                Status = s.Status,
                NotificationType = s.NotificationType,
                IsNotificationSent = s.IsNotificationSent,
                NotificationSentAt = s.NotificationSentAt,
                Notes = s.Notes,

                CreatedOn = s.CreatedOn,
                ApplicationDate = s.JobApplication?.CreatedOn ?? DateTime.MinValue
            }).ToList();
        }

        public async Task<ScheduleInterviewViewModel?> GetEditScheduleViewModelAsync(int scheduleId)
        {
            var schedule = await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null) return null;

            return new ScheduleInterviewViewModel
            {
                Id = schedule.Id,
                JobApplicationId = schedule.JobApplicationId,
                CandidateName = schedule.JobApplication?.FullName,
                CandidateEmail = schedule.JobApplication?.Email,
                CandidatePhone = schedule.JobApplication?.MobileNumber,
                JobTitle = schedule.JobApplication?.JobTitle ?? schedule.JobApplication?.ApplyingFor,
                InterviewDate = schedule.InterviewDate,
                InterviewTime = schedule.InterviewTime,
                InterviewLocation = schedule.InterviewLocation,
                TestDate = schedule.TestDate,
                TestTime = schedule.TestTime,
                TestLocation = schedule.TestLocation, // Include TestLocation if missing in mapping
                NotificationType = schedule.NotificationType,
                Notes = schedule.Notes
            };
        }

        public async Task UpdateScheduleAsync(ScheduleInterviewViewModel model)
        {
            if (!model.Id.HasValue) throw new ArgumentException("Schedule ID is required for update.");

            var schedule = await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .FirstOrDefaultAsync(s => s.Id == model.Id.Value);

            if (schedule == null) throw new KeyNotFoundException("Schedule not found.");

            // Update fields
            // Update fields
            schedule.InterviewDate = model.InterviewDate!.Value;
            schedule.InterviewTime = model.InterviewTime!.Value;
            schedule.InterviewLocation = model.InterviewLocation;
            schedule.TestDate = model.TestDate;
            schedule.TestTime = model.TestTime;
            schedule.TestLocation = model.TestLocation;
            schedule.NotificationType = model.NotificationType;
            schedule.Notes = model.Notes;
            schedule.Status = ScheduleStatus.Rescheduled;
            
            // Assuming LastUpdatedOn exists on InterviewSchedule or we rely on tracking
            // schedule.LastUpdatedOn = DateTime.Now; 

            await _context.SaveChangesAsync();

            // Send notification
            try
            {
                await _notificationService.SendInterviewNotificationAsync(schedule, model.NotificationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send update notification for schedule {ScheduleId}", schedule.Id);
            }
        }

        public async Task<InterviewSchedule?> GetScheduleByIdAsync(int id)
        {
            return await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<ScheduleInterviewViewModel?> GetScheduleViewModelAsync(int jobApplicationId)
        {
            var jobApplication = await _context.JobApplication
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == jobApplicationId);

            if (jobApplication == null)
                return null;

            return new ScheduleInterviewViewModel
            {
                JobApplicationId = jobApplicationId,
                CandidateName = jobApplication.FullName,
                CandidateEmail = jobApplication.Email,
                CandidatePhone = jobApplication.MobileNumber,
                JobTitle = jobApplication.JobTitle ?? jobApplication.ApplyingFor,
                // InterviewDate and InterviewTime are now nullable and left empty by default
                NotificationType = NotificationType.Email
            };
        }
    }
}
