using Docker.DotNet;

namespace CodeRunner.Services;

public class CodeQueueProcessingService(ILogger<CodeQueueProcessingService> logger) : IHostedLifecycleService
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
    private Task[] _runningContainers = new Task[MaxConcurrentContainers];
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Task executeQueueLoopAsync = ExecuteQueueLoopAsync();
        
        await executeQueueLoopAsync;
        
        // Log the exception if the task was faulted
        if (executeQueueLoopAsync.IsFaulted)
        {
            logger.LogError(executeQueueLoopAsync.Exception, "An error occurred while processing the queue");
        }
        
        // Wait for all running containers to finish
        // TODO: Is this the correct way to wait for all running containers to finish?
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _stopTokenSource.CancelAsync();
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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
        // TODO
        logger.LogInformation("Processing queue at {Time}", DateTime.UtcNow);
    }
}