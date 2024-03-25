using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeEvaluator.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodeFileName = table.Column<string>(type: "TEXT", nullable: false, comment: "The name of the file containing the code to be executed. This file must be located in the specified code directory."),
                    QueuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "The date and time when the code execution was started."),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: true, comment: "The date and time when the code execution was completed. This field is both set on successful and failed code execution."),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSubmissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSubmissions_CodeFileName",
                table: "CodeSubmissions",
                column: "CodeFileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeSubmissions");
        }
    }
}
