namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class ErrorViewModel
    {
        public string? Exception { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(Exception);
    }
}
