namespace SmartTimeCVs.Web.Core.Models
{
    public class LevelType
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(10)]
        public string? LevelTypeName { get; set; }

        #region Table Relations.

        public virtual ICollection<JobApplication>? JobApplications { get; set; }

        #endregion Table Relations.
    }
}
