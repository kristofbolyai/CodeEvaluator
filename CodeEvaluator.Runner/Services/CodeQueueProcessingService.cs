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
    ContainerHandler containerHandler,
    CodeExecutionHandler codeExecutionHandler) : IHostedLifecycleService
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
    private readonly Dictionary<CodeSubmission, RunningContainerInstance> _runningContainers = new();

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

        // Start the container check loop
        _ = Task.Run(() => containerHandler.ExecuteContainerMonitorLoopAsync(_stopTokenSource.Token), cancellationToken);
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
                try
                {
                    await ProcessQueueAsync();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An error occurred while processing the code queue");
                }
            }
        }
    }

    private async Task ProcessQueueAsync()
    {
        // Cleanup the running containers
        CleanupRunningContainersAsync();
        
        await using CodeDataDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        // Get the number of free containers
        int freeContainers = MaxConcurrentContainers - _runningContainers.Count;

        // Get the next code submissions from the queue
        CodeSubmission[] queuedSubmissions = await queueHandler.PopSubmissionsFromQueue(dbContext, freeContainers);

        int containerCount = 0;
        int submissionIndex = 0;
        
        // Fill the completed containers with new code submissions
        while (containerCount < freeContainers && submissionIndex < queuedSubmissions.Length)
        {
            // Get the next submission
            CodeSubmission nextSubmission = queuedSubmissions[submissionIndex];

            if (!await SetupExecutionForSubmission(nextSubmission))
            {
                // Setup failed, go to the next submission
                // Try running the next submission
                submissionIndex++;
                continue;
            }

            containerCount++;
            submissionIndex++;
        }

        // Save the state of the code submissions
        await dbContext.SaveChangesAsync();
    }

    private async Task<bool> SetupExecutionForSubmission(CodeSubmission nextSubmission)
    {
        // Set up the container folder for the code submission
        string runDirectoryForContainer;
        try
        {
            runDirectoryForContainer = codeExecutionHandler.SetupContainerFolderForSubmission(nextSubmission);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to set up the container folder for the code submission {CodeSubmission}",
                nextSubmission.Id);
            return false;
        }

        // Start the container with the code submission
        RunningContainerInstance? containerInstance =
            await containerHandler.StartExecutionAsync(nextSubmission.Language, runDirectoryForContainer, _stopTokenSource.Token);

        // If the container is null, continue to the next submission
        if (containerInstance is null)
        {
            // Try running the next submission
            return false;
        }

        // Add the container to the running containers
        _runningContainers.Add(nextSubmission, containerInstance);

        // Mark the submission as running
        nextSubmission.Status = CodeSubmission.CodeSubmissionStatus.Running;
        nextSubmission.StartedAt = DateTime.UtcNow;

        // Log the start of the code execution
        logger.LogInformation("Started code execution for submission {SubmissionId} in container {ContainerId}",
            nextSubmission.Id, containerInstance.ContainerId);

        // Send the container commands to run the code submission
        await codeExecutionHandler.RunCodeSubmission(nextSubmission, containerInstance);

        return true;
    }

    private void CleanupRunningContainersAsync()
    {
        foreach (KeyValuePair<CodeSubmission, RunningContainerInstance> runningContainer in _runningContainers)
        {
            if (containerHandler.IsContainerRunning(runningContainer.Value)) continue;

            // Remove the container from the running containers
            _runningContainers.Remove(runningContainer.Key);
        }
    }
}