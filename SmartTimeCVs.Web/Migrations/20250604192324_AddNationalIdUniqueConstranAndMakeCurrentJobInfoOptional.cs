using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddNationalIdUniqueConstranAndMakeCurrentJobInfoOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttachmentFile");

            migrationBuilder.AlterColumn<string>(
                name: "ReasonForLeavingCurrent",
                table: "JobApplication",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentJobDescription",
                table: "JobApplication",
                type: "nvarchar(2500)",
                maxLength: 2500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2500)",
                oldMaxLength: 2500);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentEmployerName",
                table: "JobApplication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentEmployerAddress",
                table: "JobApplication",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "AttachmentFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobApplicationId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    LastUpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentFile_JobApplication_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplication",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplication_NationalID",
                table: "JobApplication",
                column: "NationalID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentFile_JobApplicationId",
                table: "AttachmentFile",
                column: "JobApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttachmentFile");

            migrationBuilder.DropIndex(
                name: "IX_JobApplication_NationalID",
                table: "JobApplication");

            migrationBuilder.AlterColumn<string>(
                name: "ReasonForLeavingCurrent",
                table: "JobApplication",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentJobDescription",
                table: "JobApplication",
                type: "nvarchar(2500)",
                maxLength: 2500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2500)",
                oldMaxLength: 2500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentEmployerName",
                table: "JobApplication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentEmployerAddress",
                table: "JobApplication",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AttachmentFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobApplicationId = table.Column<int>(type: "int", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "GETDATE()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentFile_JobApplication_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplication",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentFile_JobApplicationId",
                table: "AttachmentFile",
                column: "JobApplicationId");
        }
    }
}
