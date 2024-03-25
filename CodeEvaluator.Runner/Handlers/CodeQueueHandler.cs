using CodeEvaluator.Data.Contexts;
using CodeEvaluator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeEvaluator.Runner.Handlers;

public class CodeQueueHandler
{
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