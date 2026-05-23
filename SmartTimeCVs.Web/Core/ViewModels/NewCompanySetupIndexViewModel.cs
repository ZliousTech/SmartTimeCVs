using SmartTimeCVs.Web.Core.Services.CompanySetupImport;

namespace SmartTimeCVs.Web.Core.ViewModels
{
    public class NewCompanySetupIndexViewModel
    {
        public IEnumerable<JobApplicationViewModel> Employees { get; set; } = [];
        public CompanySetupImportBatch? ImportBatch { get; set; }
        public string[] ColumnNames { get; set; } = CompanySetupImportColumns.TemplateHeaders;
    }
}
