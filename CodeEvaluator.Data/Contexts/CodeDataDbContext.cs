using CodeEvaluator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeEvaluator.Data.Contexts;

public class CodeDataDbContext : DbContext
{
    private static readonly string SqliteDatabasePath = Path.Join(Paths.ApplicationDataPath, "CodeEvaluator");
    private static readonly string SqliteConnectionString = $"Data Source={SqliteDatabasePath}/code_data.db";

    public DbSet<CodeSubmission> CodeSubmissions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Ensure the database directory exists
        if (!File.Exists(SqliteDatabasePath))
        {
            Directory.CreateDirectory(SqliteDatabasePath);
        }

        optionsBuilder.UseSqlite(SqliteConnectionString);
    }
}