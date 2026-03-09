using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededContractTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ContractTypes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ContractTypes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ContractTypes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ContractTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ContractTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ContractTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ContractTypes",
                keyColumn: "Id",
                keyValue: 7);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ContractTypes",
                columns: new[] { "Id", "ClausesEn", "ClausesNative", "CreatedOn", "DescriptionEn", "DescriptionNative", "IsDeleted", "LastUpdatedOn", "NameEn", "NameNative" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, false, null, "Fixed-Term Contract", "عقد عمل محدد المدة" },
                    { 2, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, false, null, "Open-Ended Contract", "عقد عمل غير محدد المدة" },
                    { 3, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, false, null, "Full-Time Contract", "عقد عمل بدوام كامل" },
                    { 4, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, false, null, "Part-Time Contract", "عقد عمل بدوام جزئي" },
                    { 5, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, false, null, "Temporary Contract", "عقد عمل مؤقت" },
                    { 6, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, false, null, "Freelance / Independent Contractor", "عقد عمل مستقل / عمل حر" },
                    { 7, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, false, null, "Internship / Training Contract", "عقد تدريب" }
                });
        }
    }
}
