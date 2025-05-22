namespace SmartTimeCVs.Web.Core.Models
{
    public class MaritalStatusType
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(20)]
        public string? MaritalStatusTypeName { get; set; }

        #region Table Relations.

        public virtual ICollection<JobApplication>? JobApplications { get; set; }

        #endregion Table Relations.
    }
}
