using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeEvaluator.Data.Migrations
{
    /// <inheritdoc />
    public partial class FileNameIsNotRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CodeSubmissions_CodeFileName",
                table: "CodeSubmissions");

            migrationBuilder.DropColumn(
                name: "CodeFileName",
                table: "CodeSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeFileName",
                table: "CodeSubmissions",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                comment: "The name of the file containing the code to be executed. This file must be located in the specified code directory.");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSubmissions_CodeFileName",
                table: "CodeSubmissions",
                column: "CodeFileName",
                unique: true);
        }
    }
}
