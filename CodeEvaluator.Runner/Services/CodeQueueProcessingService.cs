using CodeEvaluator.Data.Contexts;
using CodeEvaluator.Data.Models;
using CodeEvaluator.Runner.Handlers;
using CodeEvaluator.Runner.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeEvaluator.Runner.Services;

public class CodeQueueProcessingService(
    ILogger<CodeQueueProcessingService> logger,
    CodeQueueHandler queueHandler,
    IDbContextFactory<CodeDataDbContext> dbContextFactory,
    ContainerHandler containerHandler) : IHostedLifecycleService
{
    /// <summary>
    /// The number of seconds between checking the queue for new items
    /// If there is an available container, and the queue is not empty,
    /// the new item is dequeued and the container is started
    /// </summary>
    private const int SecondsBetweenQueueChecks = 5;

    /// <summary>
    /// The max number of containers that can be run concurrently
    /// is the number of processors available to the application minus two reserved for the host
    /// </summary>
    private static readonly int MaxConcurrentContainers = Environment.ProcessorCount - 2;

    /// <summary>
    /// The token source used to stop the service when requested
    /// </summary>
    private readonly CancellationTokenSource _stopTokenSource = new();

    /// <summary>
    /// The timer used to check the queue for new items to process
    /// </summary>
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(SecondsBetweenQueueChecks));

    /// <summary>
    /// The running containers that are currently processing code submissions
    /// </summary>
    private readonly CodeExecutionTask[] _runningContainers =
        Enumerable.Repeat(CodeExecutionTask.Empty, MaxConcurrentContainers).ToArray();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start running the queue.
        Task.Run(ExecuteQueueLoopAsync, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _stopTokenSource.CancelAsync();
    }

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        // Create the default images for the supported languages
        await containerHandler.CreateDefaultImages(cancellationToken);
    }

    public Task StartedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ExecuteQueueLoopAsync()
    {
        while (!_stopTokenSource.IsCancellationRequested)
        {
            while (await _timer.WaitForNextTickAsync(_stopTokenSource.Token))
            {
                await ProcessQueueAsync();
            }
        }
    }

    private async Task ProcessQueueAsync()
    {
        await using CodeDataDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        // Get the number of free containers
        int freeContainers = _runningContainers.Count(container => container.IsCompleted);

        // Get the next code submissions from the queue
        CodeSubmission[] queuedSubmissions = await queueHandler.PopSubmissionsFromQueue(dbContext, freeContainers);

        int containerIndex = 0;
        int submissionIndex = 0;
        
        // Fill the completed containers with new code submissions
        while (containerIndex < freeContainers && submissionIndex < queuedSubmissions.Length)
        {
            // Skip the container if it is not completed
            if (!_runningContainers[containerIndex].IsCompleted)
            {
                containerIndex++;
                continue;
            }

            // Get the next submission
            CodeSubmission nextSubmission = queuedSubmissions[submissionIndex];

            // Mark the submission as running
            nextSubmission.Status = CodeSubmission.CodeSubmissionStatus.Running;
            nextSubmission.StartedAt = DateTime.UtcNow;

            // Start the container with the code submission
            Task executionTask = containerHandler.StartExecutionAsync(nextSubmission, _stopTokenSource.Token);

            // Add the task to the running containers
            _runningContainers[containerIndex] = new CodeExecutionTask(executionTask, nextSubmission);

            // Log the start of the code execution
            logger.LogInformation("Started code execution for submission {SubmissionId} in container {ContainerIndex}",
                nextSubmission.Id, containerIndex);

            containerIndex++;
            submissionIndex++;
        }

        // Save the state of the code submissions
        await dbContext.SaveChangesAsync();
    }
}