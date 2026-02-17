namespace SmartTimeCVs.Web.Core.ViewModels
{
    /// <summary>
    /// ViewModel for submitting a result (interview or test)
    /// </summary>
    public class SetResultViewModel
    {
        public int ScheduleId { get; set; }
        public int Result { get; set; }
        public string? Note { get; set; }
    }
}
