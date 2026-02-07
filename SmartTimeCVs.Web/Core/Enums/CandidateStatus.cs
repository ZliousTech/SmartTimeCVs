namespace SmartTimeCVs.Web.Core.Enums
{
    /// <summary>
    /// Overall status of candidate in the hiring process
    /// </summary>
    public enum CandidateStatus
    {
        Applied = 1,
        Shortlisted = 2,
        InterviewScheduled = 3,
        InterviewCompleted = 4,
        TestScheduled = 5,
        TestCompleted = 6,
        Offered = 7,
        Hired = 8,
        Rejected = 9,
        Withdrawn = 10
    }
}
