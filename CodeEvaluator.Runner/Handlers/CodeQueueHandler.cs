using CodeEvaluator.Data.Contexts;
using CodeEvaluator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeRunner.Handlers;

public class CodeQueueHandler(CodeDataDbContext context)
{
    /**
     * Gets the next code submission from the queue, marks it as running, and returns it.
     */
    public async Task<CodeSubmission> PopCodeFromQueue()
    {
        CodeSubmission nextCodeInQueue = await context.CodeSubmissions.OrderBy(submission => submission.QueuedAt).FirstAsync();
        
        nextCodeInQueue.Status = CodeSubmission.CodeSubmissionStatus.Running;
        nextCodeInQueue.StartedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        
        return nextCodeInQueue;
    }
}