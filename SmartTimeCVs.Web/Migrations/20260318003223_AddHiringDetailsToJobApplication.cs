using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddHiringDetailsToJobApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HiringDate",
                table: "JobApplication",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPassword",
                table: "JobApplication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemUserName",
                table: "JobApplication",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HiringDate",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "SystemPassword",
                table: "JobApplication");

            migrationBuilder.DropColumn(
                name: "SystemUserName",
                table: "JobApplication");
        }
    }
}
