using SmartTimeCVs.Web.Core.ViewModels;

namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class ResultsViewModel
    {
        public List<InterviewScheduleListViewModel> TodayRequests { get; set; } = new();
        public List<InterviewScheduleListViewModel> PastRequests { get; set; } = new();
    }
}
