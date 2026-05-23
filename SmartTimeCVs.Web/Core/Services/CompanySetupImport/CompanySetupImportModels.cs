namespace SmartTimeCVs.Web.Core.Services.CompanySetupImport
{
    public static class CompanySetupImportColumns
    {
        public const string FullNameHeader = "Full Name";
        public const string UserNameHeader = "User Name";
        public const string EmailHeader = "Email";
        public const string PasswordHeader = "Password";
        public const string DateOfBirthHeader = "Date of Birth";
        public const string GenderHeader = "Gender";
        public const string NationalityHeader = "Nationality";
        public const string PlaceOfBirthHeader = "Place Of Birth";
        public const string NationalIdHeader = "National ID";
        public const string MobileNumberHeader = "Mobile Number";
        public const string MaritalStatusHeader = "Marital Status";
        public const string ApplyingForHeader = "Applying For";
        public const string JobTitleHeader = "Job Title";

        public static readonly string[] TemplateHeaders =
        {
            FullNameHeader,
            UserNameHeader,
            EmailHeader,
            PasswordHeader,
            DateOfBirthHeader,
            GenderHeader,
            NationalityHeader,
            PlaceOfBirthHeader,
            NationalIdHeader,
            MobileNumberHeader,
            MaritalStatusHeader,
            ApplyingForHeader,
            JobTitleHeader
        };

        private static readonly Dictionary<string, string> HeaderAliases =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [FullNameHeader] = FullNameHeader,
                ["FullName"] = FullNameHeader,
                [UserNameHeader] = UserNameHeader,
                ["User Name"] = UserNameHeader,
                ["Username"] = UserNameHeader,
                [EmailHeader] = EmailHeader,
                ["Email Address"] = EmailHeader,
                [PasswordHeader] = PasswordHeader,
                [DateOfBirthHeader] = DateOfBirthHeader,
                ["DateOfBirth"] = DateOfBirthHeader,
                ["DOB"] = DateOfBirthHeader,
                [GenderHeader] = GenderHeader,
                [NationalityHeader] = NationalityHeader,
                [PlaceOfBirthHeader] = PlaceOfBirthHeader,
                ["Place of Birth"] = PlaceOfBirthHeader,
                [NationalIdHeader] = NationalIdHeader,
                ["NationalId"] = NationalIdHeader,
                ["National Number"] = NationalIdHeader,
                [MobileNumberHeader] = MobileNumberHeader,
                ["Mobile"] = MobileNumberHeader,
                ["Mobile Number"] = MobileNumberHeader,
                [MaritalStatusHeader] = MaritalStatusHeader,
                ["Marital Status"] = MaritalStatusHeader,
                [ApplyingForHeader] = ApplyingForHeader,
                ["Applying For"] = ApplyingForHeader,
                [JobTitleHeader] = JobTitleHeader,
                ["Job Title"] = JobTitleHeader
            };

        public static bool TryResolveHeader(string rawHeader, out string canonicalHeader)
        {
            canonicalHeader = "";
            if (string.IsNullOrWhiteSpace(rawHeader))
                return false;
            return HeaderAliases.TryGetValue(rawHeader.Trim(), out canonicalHeader!);
        }
    }

    public sealed class CompanySetupImportRowData
    {
        public string FullName { get; set; } = "";
        public string SystemUserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public DateTime DateOfBirth { get; set; }
        public int GenderId { get; set; }
        public string Nationality { get; set; } = "";
        public string PlaceOfBirth { get; set; } = "";
        public string NationalID { get; set; } = "";
        public string MobileNumber { get; set; } = "";
        public int MaritalStatusId { get; set; }
        public string ApplyingFor { get; set; } = "";
        public string JobTitle { get; set; } = "";
    }

    public sealed class CompanySetupImportBatch
    {
        public string BatchId { get; set; } = Guid.NewGuid().ToString("N");
        public string CompanyId { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string FileName { get; set; } = "";
        public List<CompanySetupImportPreviewRow> SuccessRows { get; set; } = new();
        public List<CompanySetupImportFailedRow> FailedRows { get; set; } = new();
    }

    public sealed class CompanySetupImportPreviewRow
    {
        public int RowNumber { get; set; }
        public string FullName { get; set; } = "";
        public string NationalID { get; set; } = "";
        public string Email { get; set; } = "";
        public string SystemUserName { get; set; } = "";
        public CompanySetupImportRowData Data { get; set; } = new();
    }

    public sealed class CompanySetupImportFailedRow
    {
        public int RowNumber { get; set; }
        public string NationalID { get; set; } = "";
        public string? EmployeeName { get; set; }
        public string Reason { get; set; } = "";
    }

    public sealed class CompanySetupImportParseResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public CompanySetupImportBatch? Batch { get; set; }
    }

    public sealed class CompanySetupImportConfirmResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int ImportedCount { get; set; }
    }
}
