using CodeEvaluator.Data;
using CodeEvaluator.Data.Contexts;
using CodeEvaluator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeEvaluator.Runner.Handlers;

public class CodeQueueHandler
{
    /// <summary>
    /// Writes the code submission to a file in the code files path.
    /// </summary>
    /// <param name="dbContext">The database context for the code data.</param>
    /// <param name="codeStream">The submitted code data stream.</param>
    /// <param name="codeLanguage"></param>
    /// <returns>The code submission object with the code submission data.</returns>
    public async Task<CodeSubmission> SaveCodeToDisk(CodeDataDbContext dbContext, Stream codeStream, CodeLanguage codeLanguage)
    {
        // Create the code files path if it does not exist
        if (!Directory.Exists(Paths.SubmissionsPath))
        {
            Directory.CreateDirectory(Paths.SubmissionsPath);
        }

        // Save the code to a file
        Guid codeGuid;
        string codeFileName;

        // Ensure the file name is unique, if not, generate a new one
        do
        {
            codeGuid = Guid.NewGuid();
            codeFileName = codeGuid.ToString();
        } while (File.Exists(Path.Join(Paths.SubmissionsPath, codeFileName)));

        // Save the code to the file
        string codeFilePath = Path.Join(Paths.SubmissionsPath, codeFileName);
        await using (FileStream fileStream = new(codeFilePath, FileMode.Create, FileAccess.Write))
        {
            await codeStream.CopyToAsync(fileStream);
        }

        // Construct the code submission, return it
        CodeSubmission codeSubmission = new()
        {
            Id = codeGuid,
            Language = codeLanguage,
            QueuedAt = DateTime.UtcNow,
            Status = CodeSubmission.CodeSubmissionStatus.Queued
        };
        
        dbContext.CodeSubmissions.Add(codeSubmission);
        await dbContext.SaveChangesAsync();
        
        return codeSubmission;
    }
    
    /// <summary>
    /// Gets the next code submission from the queue, marks it as running, and returns it.
    /// </summary>
    public async Task<CodeSubmission[]> PopSubmissionsFromQueue(CodeDataDbContext context, int count)
    {
        return await context.CodeSubmissions
            .Where(submission => submission.Status == CodeSubmission.CodeSubmissionStatus.Queued)
            .OrderBy(submission => submission.QueuedAt)
            .Take(count)
            .ToArrayAsync();
    }

    /// <summary>
    /// This method is called before the code queue processing service starts running.
    /// It is responsible for marking any submissions that were started but not completed as queued.
    /// This can happen if the service was stopped before the running the submission was completed.
    /// </summary>
    public async Task DetectStuckSubmissionsBeforeRun()
    {
        throw new NotImplementedException();
    }
}