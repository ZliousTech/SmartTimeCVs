using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIsFromCompanySetupToJobApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFromCompanySetup",
                table: "JobApplication",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFromCompanySetup",
                table: "JobApplication");
        }
    }
}
