using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateJobApplicationIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobApplication_Email",
                table: "JobApplication");

            migrationBuilder.DropIndex(
                name: "IX_JobApplication_FullName",
                table: "JobApplication");

            migrationBuilder.DropIndex(
                name: "IX_JobApplication_NationalID",
                table: "JobApplication");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyId",
                table: "JobApplication",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_Email_CompanyId",
                table: "JobApplication",
                columns: new[] { "Email", "CompanyId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_FullName_CompanyId",
                table: "JobApplication",
                columns: new[] { "FullName", "CompanyId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_NationalID_CompanyId",
                table: "JobApplication",
                columns: new[] { "NationalID", "CompanyId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobApplication_Email_CompanyId",
                table: "JobApplication");

            migrationBuilder.DropIndex(
                name: "IX_JobApplication_FullName_CompanyId",
                table: "JobApplication");

            migrationBuilder.DropIndex(
                name: "IX_JobApplication_NationalID_CompanyId",
                table: "JobApplication");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyId",
                table: "JobApplication",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_Email",
                table: "JobApplication",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_FullName",
                table: "JobApplication",
                column: "FullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_NationalID",
                table: "JobApplication",
                column: "NationalID",
                unique: true);
        }
    }
}
