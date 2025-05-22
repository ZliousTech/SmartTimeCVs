using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class MakeJobAppicationOptionalInWorkExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperience_JobApplication_JobApplicationId",
                table: "WorkExperience");

            migrationBuilder.AlterColumn<int>(
                name: "JobApplicationId",
                table: "WorkExperience",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperience_JobApplication_JobApplicationId",
                table: "WorkExperience",
                column: "JobApplicationId",
                principalTable: "JobApplication",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperience_JobApplication_JobApplicationId",
                table: "WorkExperience");

            migrationBuilder.AlterColumn<int>(
                name: "JobApplicationId",
                table: "WorkExperience",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperience_JobApplication_JobApplicationId",
                table: "WorkExperience",
                column: "JobApplicationId",
                principalTable: "JobApplication",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
