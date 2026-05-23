using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace SmartTimeCVs.Web.Core.Services.CompanySetupImport
{
    public static class CompanySetupImportSession
    {
        private const string SessionKey = "CompanySetupImport:Batch";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static void SetBatch(ISession session, CompanySetupImportBatch batch) =>
            session.SetString(SessionKey, JsonSerializer.Serialize(batch, JsonOptions));

        public static CompanySetupImportBatch? GetBatch(ISession session)
        {
            var json = session.GetString(SessionKey);
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<CompanySetupImportBatch>(json, JsonOptions);
        }

        public static void Clear(ISession session) => session.Remove(SessionKey);
    }
}
