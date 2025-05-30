namespace SmartTimeCVs.Web.Core.Models
{
    public class AttachmentFile : BaseModel
    {
        [Key]
        public int Id { get; set; }

        public string? AttachmentUrl { get; set; }

        #region Table Relations.

        public int? JobApplicationId { get; set; }
        public JobApplication? JobApplication { get; set; }

        #endregion Table Relations.
    }
}
