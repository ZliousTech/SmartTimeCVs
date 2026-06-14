using SmartTimeCVs.Web.Core.Models;

namespace SmartTimeCVs.Web.Core.Extensions
{
    /// <summary>
    /// Job applications created only via <c>New Company Setup</c> are stored in the same table
    /// for external API consumption but must be hidden from standard recruitment UI.
    /// </summary>
    public static class JobApplicationQueryableExtensions
    {
        public static IQueryable<JobApplication> ExcludeNewCompanySetup(this IQueryable<JobApplication> query) =>
            query.Where(j => !j.IsFromCompanySetup);

        public static IQueryable<JobApplication> ExcludeDrafts(this IQueryable<JobApplication> query) =>
            query.Where(j => !j.IsDraft);
    }
}
