using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToJobApplicationAndRemoveIsAccepted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAccepted",
                table: "JobApplication");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "JobApplication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_Email",
                table: "JobApplication",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobApplication_Email",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "JobApplication");

            migrationBuilder.AddColumn<bool>(
                name: "IsAccepted",
                table: "JobApplication",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
