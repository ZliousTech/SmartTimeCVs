using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTimeCVs.Web.Migrations
{
    /// <inheritdoc />
    public partial class FirstPartyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorizedSignatory",
                table: "ContractTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommercialNumber",
                table: "ContractTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstPartyAddress",
                table: "ContractTypes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstPartyName",
                table: "ContractTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorizedSignatory",
                table: "ContractTypes");

            migrationBuilder.DropColumn(
                name: "CommercialNumber",
                table: "ContractTypes");

            migrationBuilder.DropColumn(
                name: "FirstPartyAddress",
                table: "ContractTypes");

            migrationBuilder.DropColumn(
                name: "FirstPartyName",
                table: "ContractTypes");
        }
    }
}
