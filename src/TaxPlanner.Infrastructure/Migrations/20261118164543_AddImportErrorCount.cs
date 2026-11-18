using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxPlanner.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportErrorCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ErrorCount",
                table: "Imports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorCount",
                table: "Imports");
        }
    }
}
