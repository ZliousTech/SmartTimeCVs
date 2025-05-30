namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class AttachmentFileViewModel : BaseModel
    {
        public int Id { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile? AttachmentFile { get; set; }
        public string? AttachmentUrl { get; set; }

        #region Table Relations.

        public int? JobApplicationId { get; set; }
        public JobApplicationViewModel? JobApplication { get; set; }

        #endregion Table Relations.
    }
}
