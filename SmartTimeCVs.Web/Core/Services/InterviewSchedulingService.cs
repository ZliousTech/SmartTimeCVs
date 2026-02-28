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
                jobApplication.IsShortListed = false;
                jobApplication.IsExcluded = false;
                jobApplication.IsHolding = false;
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
            var today = DateTime.Today;
            var upperLimit = today.AddDays(1); // Tomorrow 00:00:00

            // Filter for future interviews: Date >= Tomorrow OR (Date == Today AND Time > Now)
            // But strict requirement is just "Future".
            // Previous code used: s.InterviewDate.Date > now.Date
            // This implies strictly "Start from Tomorrow onwards".
            // Because if InterviewDate is today, .Date > Today.Date is False.
            // So it only showed interviews starting TOMORROW.
            // Let's replicate this logic safely.
            // InterviewDate > Today (23:59:59)
            // Actually: InterviewDate >= Tomorrow (00:00:00)

            // Let's stick to the previous logic interpretation: "Future dates" (excluding today).
            
            var schedules = await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .Where(s => s.CompanyId == companyId && !s.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            var filtered = schedules
                .Where(s => !s.InterviewResult.HasValue && s.InterviewDate.Date > today)
                .OrderByDescending(s => s.InterviewDate)
                .ThenBy(s => s.InterviewTime)
                .ToList();

            return filtered.Select(s => MapToListViewModel(s)).ToList();
        }

        public async Task<List<InterviewScheduleListViewModel>> GetTestSchedulesAsync(string? companyId)
        {
            var today = DateTime.Today;

            var schedules = await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .Where(s => s.CompanyId == companyId && !s.IsDeleted && s.TestDate.HasValue)
                .AsNoTracking()
                .ToListAsync();

            // Logic: TestDate > Today (strictly future)
            var filtered = schedules
                .Where(s => !s.TestResult.HasValue && s.TestDate.HasValue && s.TestDate.Value.Date > today)
                .OrderByDescending(s => s.TestDate)
                .ThenBy(s => s.TestTime)
                .ToList();

            return filtered.Select(s => MapToListViewModel(s)).ToList();
        }

        public async Task<ResultsViewModel> GetInterviewResultsAsync(string? companyId)
        {
            var today = DateTime.Today;

            // Fetch all potentially relevant schedules first
            // Criteria: 
            // - Same company
            // - Not deleted
            // - Interview Date <= Today (future ones are in schedules page)
            // - No result yet (evaluated ones are in final results)
            
            // To avoid EF translation issues with .Date, we filter in memory if volume is low, 
            // OR use strict comparison if time component is zeroed on saved dates.
            // Assuming InterviewDate includes time component? The model has InterviewTime separately. 
            // If InterviewDate stored as date+00:00:00, then comparison is safe.
            // If InterviewDate has time, comparison might be tricky.
            // Let's assume broad filtering first then split in memory.
            
            var upperLimit = today.AddDays(1); // Tomorrow 00:00:00

            var schedules = await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .Where(s => s.CompanyId == companyId && !s.IsDeleted && 
                            !s.InterviewResult.HasValue && 
                            s.InterviewDate < upperLimit)
                .AsNoTracking()
                .ToListAsync();

            var viewModel = new ResultsViewModel();

            // 1. Today's Requests: Date >= Today AND Date < Tomorrow
            // This covers full datetime range of today
            viewModel.TodayRequests = schedules
                .Where(s => s.InterviewDate.Date == today)
                .OrderBy(s => s.InterviewTime)
                .Select(s => MapToListViewModel(s))
                .ToList();

            // 2. Past Requests: Date < Today
            viewModel.PastRequests = schedules
                .Where(s => s.InterviewDate.Date < today)
                .OrderByDescending(s => s.InterviewDate)
                .ThenBy(s => s.InterviewTime)
                .Select(s => MapToListViewModel(s))
                .ToList();

            return viewModel;
        }

        public async Task<ResultsViewModel> GetTestResultsAsync(string? companyId)
        {
            var today = DateTime.Today;
            var upperLimit = today.AddDays(1);

            // Fetch all potentially relevant schedules first
            // Criteria:
            // - Same company
            // - Not deleted
            // - Has Test Date
            // - TestDate < Tomorrow
            // - No result yet
            
            var schedules = await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .Where(s => s.CompanyId == companyId && !s.IsDeleted && 
                            s.TestDate.HasValue && 
                            !s.TestResult.HasValue && 
                            s.TestDate < upperLimit)
                .AsNoTracking()
                .ToListAsync();

            var viewModel = new ResultsViewModel();

            // 1. Today's Requests
            viewModel.TodayRequests = schedules
                .Where(s => s.TestDate!.Value.Date == today)
                .OrderBy(s => s.TestTime)
                .Select(s => MapToListViewModel(s))
                .ToList();

            // 2. Past Requests
            viewModel.PastRequests = schedules
                .Where(s => s.TestDate!.Value.Date < today)
                .OrderByDescending(s => s.TestDate)
                .ThenBy(s => s.TestTime)
                .Select(s => MapToListViewModel(s))
                .ToList();

            return viewModel;
        }

        public async Task<List<InterviewScheduleListViewModel>> GetFinalResultsAsync(string? companyId)
        {
            var schedules = await _context.InterviewSchedule
                .Include(s => s.JobApplication)
                .ThenInclude(j => j.JobOffer)
                .Where(s => s.CompanyId == companyId && !s.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            // Logic:
            // - Must have an Interview Result (meaning the process is "done" or at least the interview is done)
            // - OR has a Test Result
            // User said:
            // "contains a table with job applications and their results after being evaluated in Interview Results and Test Results pages"
            // "if they have interview only, result will be interview result"
            // "if they have interview and test, result will be interview and test"
            
            // So we basically want to show anything that has at least one result?
            // Or maybe anything that was scheduled? 
            // The user said "after they have been evaluated", implying we only show those with results.

            var filtered = schedules
                .Where(s => s.InterviewResult.HasValue || s.TestResult.HasValue)
                .OrderByDescending(s => s.InterviewDate)
                .ThenBy(s => s.InterviewTime)
                .ToList();

            return filtered.Select(s => MapToListViewModel(s)).ToList();
        }

        /// <summary>
        /// Shared mapping helper to avoid code duplication
        /// </summary>
        private static InterviewScheduleListViewModel MapToListViewModel(InterviewSchedule s)
        {
            return new InterviewScheduleListViewModel
            {
                Id = s.Id,
                JobApplicationId = s.JobApplicationId,
                CandidateName = s.JobApplication?.FullName,
                CandidateEmail = s.JobApplication?.Email,
                CandidatePhone = s.JobApplication?.MobileNumber,
                CandidateImageUrl = s.JobApplication?.ImageUrl,
                CVFilePath = s.JobApplication?.AttachmentUrl,
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
                InterviewResult = s.InterviewResult,
                InterviewResultNote = s.InterviewResultNote,
                TestResult = s.TestResult,
                TestResultNote = s.TestResultNote,
                HasOffer = s.JobApplication?.JobOffer != null,
                OfferStatus = s.JobApplication?.JobOffer?.Status,
                OfferStatusString = s.JobApplication?.JobOffer?.Status.ToString(),
                CreatedOn = s.CreatedOn,
                ApplicationDate = s.JobApplication?.CreatedOn ?? DateTime.MinValue
            };
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
