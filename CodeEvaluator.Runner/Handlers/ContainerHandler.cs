using CodeEvaluator.Data.Models;
using CodeEvaluator.Runner.Models;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace CodeEvaluator.Runner.Handlers;

/// <summary>
/// This class is responsible for handling the creation and execution of containers for code submissions.
/// </summary>
/// <param name="logger">A logger instance for logging messages.</param>
/// <param name="client">A Docker client for interacting with the Docker API.</param>
public class ContainerHandler(ILogger<ContainerHandler> logger, DockerClient client)
{
    private const int ContainerExecutionTimeoutSeconds = 60;
    private const int ContainerExecutionCheckIntervalSeconds = 10;

    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(ContainerExecutionCheckIntervalSeconds));

    private readonly Dictionary<CodeLanguage, string> _defaultImages = new();

    private readonly HashSet<RunningContainerInstance> _runningContainers = [];

    /// <summary>
    /// Creates the default images for the supported languages.
    /// This method should be called when the application is first started.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the initialization process.</param>
    public async Task CreateDefaultImages(CancellationToken cancellationToken)
    {
        CodeLanguage[] codeLanguages = Enum.GetValues<CodeLanguage>();

        // Create the default image for each language
        foreach (CodeLanguage codeLanguage in codeLanguages)
        {
            // Fill the image tags for the language
            string? tag = FillImageTagsForLanguage(codeLanguage);

            // Skip if the tag is null
            if (tag is null) continue;

            // Pull the image for the tag
            await PullImageForTag(tag, cancellationToken);
        }
    }

    /// <summary>
    ///  Starts the execution of the code submission in a new container.
    /// </summary>
    /// <param name="codeLanguage">The code language of the code submission.</param>
    /// <param name="runDirectory">The run directory for the container, containing the code submission files.</param>
    /// <param name="cancellationToken">The cancellation token for the execution process.</param>
    /// <returns>The running container instance for the code submission, or null if the container could not be started.</returns>
    public async Task<RunningContainerInstance?> StartExecutionAsync(CodeLanguage codeLanguage, string runDirectory,
        CancellationToken cancellationToken)
    {
        // Create a new container
        CreateContainerResponse? createContainerResponse =
            await CreateNewContainerAsync(codeLanguage, runDirectory, cancellationToken);

        // If the container is null, return
        if (createContainerResponse is null)
        {
            logger.LogError("Failed to create a new container, warnings: {Warnings}",
                string.Join(", ", createContainerResponse?.Warnings ?? Enumerable.Empty<string>()));
            return null;
        }

        logger.LogInformation("Created a new container for the code submission with container ID: {ContainerId}",
            createContainerResponse.ID);

        // Start the container
        try
        {
            MultiplexedStream bashStream = await AttachContainerAsync(createContainerResponse.ID, cancellationToken);

            byte[] commandBuffer = "echo 'Hello, World!' > output.txt"u8.ToArray();
            await bashStream.WriteAsync(commandBuffer, 0, commandBuffer.Length, CancellationToken.None);

            bashStream.CloseWrite();

            // Add the container to the running containers
            RunningContainerInstance containerInstance = new(createContainerResponse.ID, bashStream, DateTime.UtcNow);
            _runningContainers.Add(containerInstance);
            
            await StartContainerAsync(createContainerResponse.ID, cancellationToken);

            return containerInstance;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to start the container {ContainerId}", createContainerResponse.ID);
            return null;
        }
    }

    public async Task ExecuteContainerMonitorLoopAsync(CancellationToken stopToken)
    {
        while (!stopToken.IsCancellationRequested)
        {
            while (await _timer.WaitForNextTickAsync(stopToken))
            {
                await MonitorContainersAsync(stopToken);
            }
        }
    }

    public bool IsContainerRunning(RunningContainerInstance containerInstance)
    {
        return _runningContainers.Contains(containerInstance);
    }

    private async Task<CreateContainerResponse?> CreateNewContainerAsync(CodeLanguage codeLanguage, string runDirectory,
        CancellationToken cancellationToken)
    {
        if (_defaultImages.TryGetValue(codeLanguage, out string? imageTag))
        {
            logger.LogInformation(
                "Using directory {Directory} for a container with image {ImageTag}", runDirectory, imageTag);

            return await client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = imageTag,
                Cmd = ["/bin/sh"],
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                OpenStdin = true,
                StdinOnce = true,
                WorkingDir = runDirectory
            }, cancellationToken);
        }

        logger.LogError("No image tag for the language {Language}, cannot execute code", codeLanguage);
        return null;
    }

    private async Task StartContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);

        logger.LogInformation("Started the container {ContainerId}", containerId);
    }

    private async Task<MultiplexedStream> AttachContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        return await client.Containers.AttachContainerAsync(containerId, false, new ContainerAttachParameters
        {
            Stream = true,
            Stdin = true,
            Stdout = true,
            Stderr = true
        }, cancellationToken);
    }

    private async Task StopContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        await client.Containers.StopContainerAsync(containerId, new ContainerStopParameters(), cancellationToken);

        logger.LogInformation("Stopped the container {ContainerId}", containerId);
    }

    private async Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        await client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters(), cancellationToken);

        logger.LogInformation("Removed the container {ContainerId}", containerId);
    }

    private string? FillImageTagsForLanguage(CodeLanguage codeLanguage)
    {
        switch (codeLanguage)
        {
            case CodeLanguage.CSharp:
                _defaultImages[codeLanguage] = "mcr.microsoft.com/dotnet/sdk:8.0";
                break;
            case CodeLanguage.Python:
                _defaultImages[codeLanguage] = "python:3.9.19";
                break;
            default:
                logger.LogCritical("No default image for the language {Language}", codeLanguage);
                return null;
        }

        return _defaultImages[codeLanguage];
    }

    private async Task PullImageForTag(string tag, CancellationToken cancellationToken)
    {
        logger.LogInformation("Started pulling the image for the tag {Tag}", tag);

        await client.Images.CreateImageAsync(new ImagesCreateParameters
        {
            FromImage = tag
        }, null, new Progress<JSONMessage>(), cancellationToken);

        logger.LogInformation("Finished pulling the image for the tag {Tag}", tag);
    }

    private async Task MonitorContainersAsync(CancellationToken cancellationToken)
    {
        List<RunningContainerInstance> timeoutContainerInstances = _runningContainers.Where(containerInstance =>
            (DateTime.UtcNow - containerInstance.StartTime).TotalSeconds > ContainerExecutionTimeoutSeconds).ToList();

        foreach (RunningContainerInstance containerInstance in timeoutContainerInstances)
        {
            // Add a stop task to the container, to kill it after a timeout
            // Otherwise, it will shut down after the code execution is finished
            try
            {
                await StopContainerAsync(containerInstance.ContainerId, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to stop the container {ContainerId}", containerInstance.ContainerId);
                // Continue with the execution, try to remove the container
            }

            // Try to remove the container after it is stopped
            try
            {
                await RemoveContainerAsync(containerInstance.ContainerId, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to remove the container {ContainerId}", containerInstance.ContainerId);
            }

            // Remove the container from the running containers
            _runningContainers.Remove(containerInstance);
        }
    }
}