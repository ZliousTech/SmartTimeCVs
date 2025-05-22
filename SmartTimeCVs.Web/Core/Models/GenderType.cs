namespace SmartTimeCVs.Web.Core.Models
{
    public class GenderType
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(10)]
        public string GenderTypeName { get; set; } = null!;

        #region Related Tables.

        public virtual ICollection<JobApplication>? JobApplications { get; set; }

        #endregion Related Tables.
    }
}
