using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewAndTestResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InterviewResult",
                table: "InterviewSchedule",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewResultNote",
                table: "InterviewSchedule",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestResult",
                table: "InterviewSchedule",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestResultNote",
                table: "InterviewSchedule",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewResult",
                table: "InterviewSchedule");

            migrationBuilder.DropColumn(
                name: "InterviewResultNote",
                table: "InterviewSchedule");

            migrationBuilder.DropColumn(
                name: "TestResult",
                table: "InterviewSchedule");

            migrationBuilder.DropColumn(
                name: "TestResultNote",
                table: "InterviewSchedule");
        }
    }
}
