using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDateFromToInCurrentWorkSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentWorkPeriod",
                table: "JobApplication");

            migrationBuilder.AlterColumn<string>(
                name: "OtherLanguage",
                table: "JobApplication",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentFrom",
                table: "JobApplication",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentTo",
                table: "JobApplication",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentFrom",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "CurrentTo",
                table: "JobApplication");

            migrationBuilder.AlterColumn<string>(
                name: "OtherLanguage",
                table: "JobApplication",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentWorkPeriod",
                table: "JobApplication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }
    }
}
