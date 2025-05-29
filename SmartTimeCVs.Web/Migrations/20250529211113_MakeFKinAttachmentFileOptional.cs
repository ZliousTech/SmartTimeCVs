using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class MakeFKinAttachmentFileOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttachmentFile_JobApplication_JobApplicationId",
                table: "AttachmentFiles");

            migrationBuilder.AlterColumn<int>(
                name: "JobApplicationId",
                table: "AttachmentFiles",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_AttachmentFile_JobApplication_JobApplicationId",
                table: "AttachmentFiles",
                column: "JobApplicationId",
                principalTable: "JobApplication",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttachmentFile_JobApplication_JobApplicationId",
                table: "AttachmentFiles");

            migrationBuilder.AlterColumn<int>(
                name: "JobApplicationId",
                table: "AttachmentFiles",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttachmentFile_JobApplication_JobApplicationId",
                table: "AttachmentFiles",
                column: "JobApplicationId",
                principalTable: "JobApplication",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
