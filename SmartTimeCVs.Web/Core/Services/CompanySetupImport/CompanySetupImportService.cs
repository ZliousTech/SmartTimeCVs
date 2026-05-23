using System.Globalization;
using System.Net.Http;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Common.Base;
using Microsoft.EntityFrameworkCore;
using SmartTimeCVs.Web.Core.Enums;
using SmartTimeCVs.Web.Core.Models;
using SmartTimeCVs.Web.Data;

namespace SmartTimeCVs.Web.Core.Services.CompanySetupImport
{
    public class CompanySetupImportService : ICompanySetupImportService
    {
        private static readonly Regex PasswordRegex = new(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            RegexOptions.Compiled);

        private static readonly Regex UserNameRegex = new(@"^\S+$", RegexOptions.Compiled);

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public CompanySetupImportService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public byte[] BuildTemplateWorkbook()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Employees");
            for (var i = 0; i < CompanySetupImportColumns.TemplateHeaders.Length; i++)
            {
                sheet.Cell(1, i + 1).Value = CompanySetupImportColumns.TemplateHeaders[i];
                sheet.Cell(1, i + 1).Style.Font.Bold = true;
            }
            sheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<CompanySetupImportParseResult> ParseAndPreviewAsync(
            Stream excelStream,
            string fileName,
            string companyId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(companyId))
                return Invalid("Company context is missing.");

            XLWorkbook workbook;
            try
            {
                workbook = new XLWorkbook(excelStream);
            }
            catch (Exception)
            {
                return Invalid("Could not read the Excel file. Please upload a valid .xlsx file.");
            }

            using (workbook)
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    return Invalid("The Excel file has no worksheets.");

                var headerMap = ReadHeaderMap(worksheet);
                if (headerMap == null)
                    return Invalid("Invalid or missing column headers. Download the template and use row 1 for column names.");

                foreach (var required in CompanySetupImportColumns.TemplateHeaders)
                {
                    if (!headerMap.ContainsKey(NormalizeHeader(required)))
                        return Invalid($"Required column '{required}' is missing.");
                }

                var categories = SysBase.GetJobCategories(companyId);
                var existingApps = await _context.JobApplication
                    .Where(j => j.CompanyId == companyId && j.IsFromCompanySetup && !j.IsDeleted)
                    .Select(j => new
                    {
                        j.FullName,
                        j.Email,
                        j.NationalID,
                        j.SystemUserName
                    })
                    .ToListAsync(cancellationToken);

                var existingNationalIds = existingApps
                    .Select(x => x.NationalID.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingEmails = existingApps
                    .Select(x => x.Email.Trim().ToLowerInvariant())
                    .ToHashSet();
                var existingFullNames = existingApps
                    .Select(x => x.FullName.Trim().ToLowerInvariant())
                    .ToHashSet();
                var existingUserNamesGlobal = await _context.JobApplication
                    .Where(j => j.SystemUserName != null && j.SystemUserName != "")
                    .Select(j => j.SystemUserName!)
                    .ToListAsync(cancellationToken);
                var existingUserNames = existingUserNamesGlobal
                    .Select(u => u.Trim().ToLowerInvariant())
                    .ToHashSet();

                var batch = new CompanySetupImportBatch
                {
                    CompanyId = companyId,
                    FileName = fileName
                };

                var seenNationalIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var seenEmails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var seenUserNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var seenMobiles = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                for (var rowNum = 2; rowNum <= lastRow; rowNum++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var nationalIdRaw = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.NationalIdHeader);
                    var emailRaw = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.EmailHeader);
                    var userNameRaw = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.UserNameHeader);

                    if (string.IsNullOrWhiteSpace(nationalIdRaw)
                        && string.IsNullOrWhiteSpace(emailRaw)
                        && string.IsNullOrWhiteSpace(userNameRaw))
                        continue;

                    var fullName = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.FullNameHeader);
                    var userName = userNameRaw?.Trim() ?? "";
                    var email = emailRaw?.Trim() ?? "";
                    var password = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.PasswordHeader) ?? "";
                    var dobRaw = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.DateOfBirthHeader);
                    var genderRaw = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.GenderHeader);
                    var nationality = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.NationalityHeader) ?? "";
                    var placeOfBirth = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.PlaceOfBirthHeader) ?? "";
                    var nationalId = nationalIdRaw?.Trim() ?? "";
                    var mobile = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.MobileNumberHeader) ?? "";
                    var maritalRaw = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.MaritalStatusHeader);
                    var applyingForRaw = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.ApplyingForHeader) ?? "";
                    var jobTitleRaw = GetCellString(worksheet, rowNum, headerMap, CompanySetupImportColumns.JobTitleHeader) ?? "";

                    var errors = new List<string>();

                    if (string.IsNullOrWhiteSpace(fullName)) errors.Add("Full Name is required.");
                    if (string.IsNullOrWhiteSpace(userName)) errors.Add("User Name is required.");
                    else if (!UserNameRegex.IsMatch(userName)) errors.Add("User Name cannot contain spaces.");
                    if (string.IsNullOrWhiteSpace(email)) errors.Add("Email is required.");
                    else if (!IsValidEmail(email)) errors.Add("Email format is invalid.");
                    if (string.IsNullOrWhiteSpace(password)) errors.Add("Password is required.");
                    else if (!PasswordRegex.IsMatch(password))
                        errors.Add("Password must be at least 8 characters and include uppercase, lowercase, number, and special character.");
                    if (!TryParseDate(worksheet, rowNum, headerMap, dobRaw, out var dateOfBirth))
                        errors.Add("Date of Birth is required and must be a valid date.");
                    if (!TryParseGender(genderRaw, out var genderId))
                        errors.Add("Gender is required (Male/Female or 1/2).");
                    if (string.IsNullOrWhiteSpace(nationality)) errors.Add("Nationality is required.");
                    if (string.IsNullOrWhiteSpace(placeOfBirth)) errors.Add("Place Of Birth is required.");
                    if (string.IsNullOrWhiteSpace(nationalId)) errors.Add("National ID is required.");
                    else if (nationalId.Length > 18) errors.Add("National ID must be at most 18 characters.");
                    if (string.IsNullOrWhiteSpace(mobile)) errors.Add("Mobile Number is required.");
                    else if (mobile.Length > 15) errors.Add("Mobile Number must be at most 15 characters.");
                    if (!TryParseMaritalStatus(maritalRaw, out var maritalStatusId))
                        errors.Add("Marital Status is required (Single, Married, Divorced, Widowed, Separated or 1-5).");

                    string? applyingForGuid = null;
                    string? resolvedJobTitle = null;

                    if (string.IsNullOrWhiteSpace(applyingForRaw))
                        errors.Add("Applying For is required.");
                    else if (!TryResolveCategory(applyingForRaw, categories, out applyingForGuid, out var categoryError))
                        errors.Add(categoryError);

                    if (string.IsNullOrWhiteSpace(jobTitleRaw))
                        errors.Add("Job Title is required.");
                    else if (applyingForGuid != null
                             && !TryResolveJobTitle(jobTitleRaw, applyingForGuid, out resolvedJobTitle, out var titleError))
                        errors.Add(titleError);

                    if (errors.Count > 0)
                    {
                        batch.FailedRows.Add(new CompanySetupImportFailedRow
                        {
                            RowNumber = rowNum,
                            NationalID = nationalId,
                            EmployeeName = fullName,
                            Reason = string.Join(" ", errors)
                        });
                        continue;
                    }

                    var emailKey = email.ToLowerInvariant();
                    var userNameKey = userName.ToLowerInvariant();

                    if (seenNationalIds.TryGetValue(nationalId, out var dupNatRow))
                    {
                        FailDuplicate(batch, rowNum, nationalId, fullName,
                            $"Duplicate National ID in file (first on row {dupNatRow}).");
                        continue;
                    }
                    seenNationalIds[nationalId] = rowNum;

                    if (seenEmails.TryGetValue(emailKey, out var dupEmailRow))
                    {
                        FailDuplicate(batch, rowNum, nationalId, fullName,
                            $"Duplicate Email in file (first on row {dupEmailRow}).");
                        continue;
                    }
                    seenEmails[emailKey] = rowNum;

                    if (seenUserNames.TryGetValue(userNameKey, out var dupUserRow))
                    {
                        FailDuplicate(batch, rowNum, nationalId, fullName,
                            $"Duplicate User Name in file (first on row {dupUserRow}).");
                        continue;
                    }
                    seenUserNames[userNameKey] = rowNum;

                    if (seenMobiles.TryGetValue(mobile, out var dupMobileRow))
                    {
                        FailDuplicate(batch, rowNum, nationalId, fullName,
                            $"Duplicate Mobile Number in file (first on row {dupMobileRow}).");
                        continue;
                    }
                    seenMobiles[mobile] = rowNum;

                    if (existingNationalIds.Contains(nationalId))
                    {
                        FailDuplicate(batch, rowNum, nationalId, fullName,
                            "National ID is already registered for this company.");
                        continue;
                    }

                    if (existingEmails.Contains(emailKey))
                    {
                        FailDuplicate(batch, rowNum, nationalId, fullName,
                            "Email is already registered for this company.");
                        continue;
                    }

                    if (existingFullNames.Contains(fullName.Trim().ToLowerInvariant()))
                    {
                        FailDuplicate(batch, rowNum, nationalId, fullName,
                            "Full Name is already registered for this company.");
                        continue;
                    }

                    if (existingUserNames.Contains(userNameKey))
                    {
                        FailDuplicate(batch, rowNum, nationalId, fullName,
                            "User Name is already taken.");
                        continue;
                    }

                    try
                    {
                        var available = await CheckExternalUserNameAvailabilityAsync(userName, cancellationToken);
                        if (!available)
                        {
                            FailDuplicate(batch, rowNum, nationalId, fullName,
                                "User Name is already registered in the main company system.");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        batch.FailedRows.Add(new CompanySetupImportFailedRow
                        {
                            RowNumber = rowNum,
                            NationalID = nationalId,
                            EmployeeName = fullName,
                            Reason = ex.Message
                        });
                        continue;
                    }

                    var data = new CompanySetupImportRowData
                    {
                        FullName = fullName.Trim(),
                        SystemUserName = userName,
                        Email = email,
                        Password = password,
                        DateOfBirth = dateOfBirth,
                        GenderId = genderId,
                        Nationality = nationality.Trim(),
                        PlaceOfBirth = placeOfBirth.Trim(),
                        NationalID = nationalId,
                        MobileNumber = mobile.Trim(),
                        MaritalStatusId = maritalStatusId,
                        ApplyingFor = applyingForGuid!,
                        JobTitle = resolvedJobTitle!
                    };

                    batch.SuccessRows.Add(new CompanySetupImportPreviewRow
                    {
                        RowNumber = rowNum,
                        FullName = data.FullName,
                        NationalID = data.NationalID,
                        Email = data.Email,
                        SystemUserName = data.SystemUserName,
                        Data = data
                    });
                }

                return new CompanySetupImportParseResult { IsValid = true, Batch = batch };
            }
        }

        public async Task<CompanySetupImportConfirmResult> ConfirmBatchAsync(
            CompanySetupImportBatch batch,
            string companyId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(companyId))
            {
                return new CompanySetupImportConfirmResult
                {
                    Success = false,
                    Message = "Company context is missing."
                };
            }

            if (batch.SuccessRows.Count == 0)
            {
                return new CompanySetupImportConfirmResult
                {
                    Success = false,
                    Message = "No employees to import."
                };
            }

            var imported = 0;
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var row in batch.SuccessRows)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var d = row.Data;

                    var emailExists = await _context.JobApplication.AnyAsync(
                        j => j.CompanyId == companyId && j.Email == d.Email, cancellationToken);
                    if (emailExists)
                        continue;

                    var nationalIdExists = await _context.JobApplication.AnyAsync(
                        j => j.CompanyId == companyId && j.NationalID == d.NationalID, cancellationToken);
                    if (nationalIdExists)
                        continue;

                    var nameExists = await _context.JobApplication.AnyAsync(
                        j => j.CompanyId == companyId
                             && j.FullName.Trim().ToLower() == d.FullName.Trim().ToLower(), cancellationToken);
                    if (nameExists)
                        continue;

                    var userExists = await _context.JobApplication.AnyAsync(
                        j => j.SystemUserName == d.SystemUserName, cancellationToken);
                    if (userExists)
                        continue;

                    var mobileExists = await _context.JobApplication.AnyAsync(
                        j => j.CompanyId == companyId && j.MobileNumber == d.MobileNumber, cancellationToken);
                    if (mobileExists)
                        continue;

                    var available = await CheckExternalUserNameAvailabilityAsync(d.SystemUserName, cancellationToken);
                    if (!available)
                        continue;

                    var application = new JobApplication
                    {
                        FullName = d.FullName,
                        Email = d.Email,
                        SystemUserName = d.SystemUserName,
                        SystemPassword = d.Password,
                        CompanyId = companyId,
                        IsFromCompanySetup = true,
                        IsImported = false,
                        ImageUrl = "",
                        Address = "",
                        PlaceOfBirth = d.PlaceOfBirth,
                        NationalID = d.NationalID,
                        MobileNumber = d.MobileNumber,
                        Nationality = d.Nationality,
                        DateOfBirth = d.DateOfBirth,
                        GenderId = d.GenderId,
                        MaritalStatusId = d.MaritalStatusId,
                        ApplyingFor = d.ApplyingFor,
                        JobTitle = d.JobTitle,
                        HighSchoolName = "",
                        HighSchoolGraduationYear = 0,
                        EnglishLevelId = 1,
                        ComputerSkillsLevelId = 1,
                        ExpectedSalary = 1,
                        CreatedOn = DateTime.Now,
                        LastUpdatedOn = DateTime.Now,
                        CandidateStatus = null
                    };

                    _context.JobApplication.Add(application);
                    imported++;
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var isAr = CultureInfo.CurrentUICulture.Name.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
                return new CompanySetupImportConfirmResult
                {
                    Success = imported > 0,
                    ImportedCount = imported,
                    Message = isAr
                        ? $"تم استيراد {imported} موظفاً بنجاح."
                        : $"Successfully imported {imported} employee(s)."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new CompanySetupImportConfirmResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private static void FailDuplicate(
            CompanySetupImportBatch batch,
            int rowNum,
            string nationalId,
            string? fullName,
            string reason)
        {
            batch.FailedRows.Add(new CompanySetupImportFailedRow
            {
                RowNumber = rowNum,
                NationalID = nationalId,
                EmployeeName = fullName,
                Reason = reason
            });
        }

        private static CompanySetupImportParseResult Invalid(string message) =>
            new() { IsValid = false, ErrorMessage = message };

        private static Dictionary<string, int>? ReadHeaderMap(IXLWorksheet worksheet)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var col = 1;
            while (true)
            {
                var cell = worksheet.Cell(1, col);
                var text = cell.GetString()?.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    if (col == 1) return null;
                    break;
                }

                if (!CompanySetupImportColumns.TryResolveHeader(text, out var canonical))
                    return null;

                map[NormalizeHeader(canonical)] = col;
                col++;
            }

            return map.Count == 0 ? null : map;
        }

        private static string NormalizeHeader(string header) => header.Trim();

        private static string? GetCellString(
            IXLWorksheet worksheet,
            int row,
            IReadOnlyDictionary<string, int> headerMap,
            string canonicalHeader)
        {
            if (!headerMap.TryGetValue(NormalizeHeader(canonicalHeader), out var col))
                return null;
            var cell = worksheet.Cell(row, col);
            if (cell.IsEmpty())
                return null;
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return cell.GetString()?.Trim();
        }

        private static bool TryParseDate(
            IXLWorksheet worksheet,
            int row,
            IReadOnlyDictionary<string, int> headerMap,
            string? text,
            out DateTime date)
        {
            date = default;
            if (headerMap.TryGetValue(NormalizeHeader(CompanySetupImportColumns.DateOfBirthHeader), out var col))
            {
                var cell = worksheet.Cell(row, col);
                if (!cell.IsEmpty() && TryParseDateFromCell(cell, out date))
                    return true;
            }

            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var oaSerial)
                && oaSerial > 0 && oaSerial < 2958466)
            {
                try
                {
                    date = DateTime.FromOADate(oaSerial).Date;
                    return true;
                }
                catch (ArgumentException)
                {
                    // not an OLE date serial
                }
            }

            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
                || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                date = date.Date;
                return true;
            }

            return false;
        }

        /// <summary>Excel often stores dates as Number (OLE serial), not DateTime.</summary>
        private static bool TryParseDateFromCell(IXLCell cell, out DateTime date)
        {
            date = default;
            try
            {
                if (cell.DataType == XLDataType.DateTime)
                {
                    date = cell.GetDateTime().Date;
                    return true;
                }

                if (cell.DataType == XLDataType.Number)
                {
                    date = DateTime.FromOADate(cell.GetDouble()).Date;
                    return true;
                }

                var formatted = cell.GetFormattedString()?.Trim();
                if (!string.IsNullOrEmpty(formatted)
                    && (DateTime.TryParse(formatted, CultureInfo.CurrentCulture, DateTimeStyles.None, out date)
                        || DateTime.TryParse(formatted, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)))
                {
                    date = date.Date;
                    return true;
                }
            }
            catch (Exception)
            {
                // fall through to text parsing
            }

            return false;
        }

        private static bool TryParseGender(string? raw, out int genderId)
        {
            genderId = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var v = raw.Trim();
            if (int.TryParse(v, out var n) && Enum.IsDefined(typeof(GenderTypeEnum), n))
            {
                genderId = n;
                return true;
            }

            if (v.Equals("Male", StringComparison.OrdinalIgnoreCase)
                || v.Equals("M", StringComparison.OrdinalIgnoreCase))
            {
                genderId = (int)GenderTypeEnum.Male;
                return true;
            }

            if (v.Equals("Female", StringComparison.OrdinalIgnoreCase)
                || v.Equals("F", StringComparison.OrdinalIgnoreCase))
            {
                genderId = (int)GenderTypeEnum.Female;
                return true;
            }

            return false;
        }

        private static bool TryParseMaritalStatus(string? raw, out int maritalStatusId)
        {
            maritalStatusId = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var v = raw.Trim();
            if (int.TryParse(v, out var n) && Enum.IsDefined(typeof(MaritalStatusTypeEnum), n))
            {
                maritalStatusId = n;
                return true;
            }

            foreach (MaritalStatusTypeEnum status in Enum.GetValues(typeof(MaritalStatusTypeEnum)))
            {
                if (v.Equals(status.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    maritalStatusId = (int)status;
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveCategory(
            string raw,
            List<JobCategories> categories,
            out string? categoryGuid,
            out string error)
        {
            categoryGuid = null;
            error = "";
            var v = raw.Trim();

            var byGuid = categories.FirstOrDefault(c =>
                string.Equals(c.JobCategoryGuidID, v, StringComparison.OrdinalIgnoreCase));
            if (byGuid != null)
            {
                categoryGuid = byGuid.JobCategoryGuidID;
                return true;
            }

            var byName = categories.FirstOrDefault(c =>
                string.Equals(c.JobCategoryNameEn, v, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.JobCategoryNameNative, v, StringComparison.OrdinalIgnoreCase));
            if (byName != null)
            {
                categoryGuid = byName.JobCategoryGuidID;
                return true;
            }

            error = $"Applying For '{v}' was not found. Use a valid category name or GUID.";
            return false;
        }

        private static bool TryResolveJobTitle(
            string raw,
            string categoryGuid,
            out string? jobTitleText,
            out string error)
        {
            jobTitleText = null;
            error = "";
            var v = raw.Trim();
            var titles = SysBase.GetJobTitles(categoryGuid);

            var match = titles.FirstOrDefault(t =>
                string.Equals(t.JobTitleNameEn, v, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.JobTitleNameNative, v, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.JobTitleGuidID, v, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                error = $"Job Title '{v}' was not found for the selected category.";
                return false;
            }

            jobTitleText = match.JobTitleNameEn ?? match.JobTitleNameNative ?? v;
            return true;
        }

        private static bool IsValidEmail(string email) =>
            MailAddress.TryCreate(email, out _);

        private async Task<bool> CheckExternalUserNameAvailabilityAsync(
            string userName,
            CancellationToken cancellationToken)
        {
            var enableCheck = _configuration.GetValue<bool>("ExternalApiSettings:EnableUserCheck");
            if (!enableCheck)
                return true;

            var apiUrl = _configuration.GetValue<string>("ExternalApiSettings:CheckUserNameUrl");
            if (string.IsNullOrEmpty(apiUrl))
                return true;

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var content = new StringContent(
                JsonSerializer.Serialize(new { userName }),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(apiUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    "We are currently experiencing server issues. Please try again later.");

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var jsonDoc = JsonDocument.Parse(responseString);

            if (jsonDoc.RootElement.TryGetProperty("data", out var dataElement)
                && dataElement.TryGetProperty("isAvailable", out var isAvailableElement))
            {
                return isAvailableElement.GetBoolean();
            }

            throw new InvalidOperationException(
                "We are currently experiencing server issues. Please try again later.");
        }
    }
}
