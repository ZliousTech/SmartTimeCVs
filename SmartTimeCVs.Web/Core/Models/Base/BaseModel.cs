namespace SmartTimeCVs.Web.Core.Models.Base
{
    public class BaseModel
    {
        public bool IsDeleted { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
    }
}
