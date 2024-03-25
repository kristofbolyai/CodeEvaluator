using CodeEvaluator.Data.Models;

namespace CodeEvaluator.Runner.Models;

public record CodeExecutionTask(Task ExecutionTask, CodeSubmission CodeSubmission)
{
    public static CodeExecutionTask Empty => new(Task.CompletedTask, CodeSubmission.Empty);

    public bool IsCompleted => ExecutionTask.IsCompleted;
}