namespace SmartTimeCVs.Web.Core.Dtos
{
    public class SmartTimeCompanyDTO
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public CompanyData? Data { get; set; }
    }

    public class CompanyData
    {
        public string? CompanyLogo { get; set; }
        public string? HomePageTextEn { get; set; }
        public string? HomePageTextNative { get; set; }
    }
}
