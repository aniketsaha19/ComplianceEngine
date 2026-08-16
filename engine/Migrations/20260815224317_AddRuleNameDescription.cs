using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleNameDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Rules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Rules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Rules");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Rules");
        }
    }
}
