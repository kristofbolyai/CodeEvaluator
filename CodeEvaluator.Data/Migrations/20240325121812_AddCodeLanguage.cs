using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeEvaluator.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Language",
                table: "CodeSubmissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                comment: "The language of the code to be executed.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "CodeSubmissions");
        }
    }
}
