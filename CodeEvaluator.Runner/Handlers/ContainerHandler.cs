using CodeEvaluator.Data.Models;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace CodeEvaluator.Runner.Handlers;

/// <summary>
/// This class is responsible for handling the creation and execution of containers for code submissions.
/// </summary>
/// <param name="logger">A logger instance for logging messages.</param>
/// <param name="client">A Docker client for interacting with the Docker API.</param>
/// <param name="codeExecutionHandler">A code execution handler for setting up the container folder.</param>
public class ContainerHandler(ILogger<ContainerHandler> logger, DockerClient client, CodeExecutionHandler codeExecutionHandler)
{
    private const int ContainerExecutionTimeoutSeconds = 60;

    private readonly Dictionary<CodeLanguage, string> _defaultImages = new();

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
    /// <param name="codeSubmission">The code submission to execute.</param>
    /// <param name="cancellationToken">The cancellation token for the execution process.</param>
    /// <returns></returns>
    public async Task StartExecutionAsync(CodeSubmission codeSubmission, CancellationToken cancellationToken)
    {
        // Create a new container
        CreateContainerResponse? createContainerResponse = await CreateNewContainerAsync(codeSubmission, cancellationToken);

        // If the container is null, return
        if (createContainerResponse is null)
        {
            logger.LogError("Failed to create a new container for the code submission {CodeSubmission}, warnings: {Warnings}",
                codeSubmission.Id, string.Join(", ", createContainerResponse?.Warnings ?? Enumerable.Empty<string>()));
            return;
        }

        logger.LogInformation("Created a new container for the code submission {CodeSubmission}, container ID: {ContainerId}",
            codeSubmission.Id, createContainerResponse.ID);

        // Start the container
        try
        {
            await StartContainerAsync(createContainerResponse.ID, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to start the container {ContainerId}", createContainerResponse.ID);
            return;
        }

        // Add a stop task to the container, to kill it after a timeout
        // Otherwise, it will shut down after the code execution is finished
        try
        {
            await StopContainerAsync(createContainerResponse.ID, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to stop the container {ContainerId}", createContainerResponse.ID);
            // Continue with the execution, try to remove the container
        }

        // Try to remove the container after it is stopped
        try
        {
            await RemoveContainerAsync(createContainerResponse.ID, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to remove the container {ContainerId}", createContainerResponse.ID);
        }
    }

    private async Task StartContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cancellationToken);

        logger.LogInformation("Started the container {ContainerId}", containerId);
    }

    private async Task StopContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        await client.Containers.StopContainerAsync(containerId,
            new ContainerStopParameters { WaitBeforeKillSeconds = ContainerExecutionTimeoutSeconds }, cancellationToken);

        logger.LogInformation("Stopped the container {ContainerId}", containerId);
    }

    private async Task RemoveContainerAsync(string containerId, CancellationToken cancellationToken)
    {
        await client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters(), cancellationToken);

        logger.LogInformation("Removed the container {ContainerId}", containerId);
    }

    private async Task<CreateContainerResponse?> CreateNewContainerAsync(CodeSubmission codeSubmission,
        CancellationToken cancellationToken)
    {
        if (_defaultImages.TryGetValue(codeSubmission.Language, out string? imageTag))
        {
            // Set up the container folder for the code submission
            string runDirectory = codeExecutionHandler.SetupContainerFolderForSubmission(codeSubmission);
            
            logger.LogInformation(
                "Using directory {Directory} for the code submission {CodeSubmission} with container image {ImageTag}",
                runDirectory, codeSubmission.Id, imageTag);

            // TODO: Add cmd to execute the code
            return await client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = imageTag,
                Cmd = ["sleep", "15"],
                WorkingDir = runDirectory
            }, cancellationToken);
        }

        logger.LogError("No image tag for the language {Language}, cannot execute code", codeSubmission.Language);
        return null;
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

    /// <summary>
    /// Pulls the image for the specified tag.
    /// This method is used for setting up the default images for the supported languages.
    /// </summary>
    /// <param name="tag">The tag of the image to pull.</param>
    /// <param name="cancellationToken">The cancellation token for the initialization process.</param>
    private async Task PullImageForTag(string tag, CancellationToken cancellationToken)
    {
        await client.Images.CreateImageAsync(new ImagesCreateParameters
        {
            FromImage = tag
        }, null, new Progress<JSONMessage>(), cancellationToken);
    }
}