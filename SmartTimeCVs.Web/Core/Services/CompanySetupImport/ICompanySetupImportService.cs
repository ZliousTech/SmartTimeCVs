namespace SmartTimeCVs.Web.Core.Services.CompanySetupImport
{
    public interface ICompanySetupImportService
    {
        byte[] BuildTemplateWorkbook();

        Task<CompanySetupImportParseResult> ParseAndPreviewAsync(
            Stream excelStream,
            string fileName,
            string companyId,
            CancellationToken cancellationToken = default);

        Task<CompanySetupImportConfirmResult> ConfirmBatchAsync(
            CompanySetupImportBatch batch,
            string companyId,
            CancellationToken cancellationToken = default);
    }
}
