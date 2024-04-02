using CodeEvaluator.Data;
using CodeEvaluator.Data.Models;
using CodeEvaluator.Runner.Models;

namespace CodeEvaluator.Runner.Handlers;

/// <summary>
/// This class is responsible for handling the execution of code submissions.
/// This includes setting up the container folder before execution, and cleaning up the folder after execution,
/// including extracting the stdout and stderr from the container. Other files may be extracted as well.
/// </summary>
/// <param name="logger">A logger instance for logging messages.</param>
public class CodeExecutionHandler(ILogger<CodeExecutionHandler> logger)
{
    /// <summary>
    /// Sets up the container folder for the code submission.
    /// Steps:
    /// <list type="number">
    /// <item><description>Create a run directory for the submission's container.</description></item>
    /// <item><description>Copy the code file to the run directory.</description></item>
    /// <item><description>Copy the stdin and other input file to the run directory.</description></item>
    /// </list>
    /// </summary>
    /// <param name="codeSubmission">The code submission to set up the container folder for.</param>
    /// <returns>The path to the run directory for the submission's container.</returns>
    public string SetupContainerFolderForSubmission(CodeSubmission codeSubmission)
    {
        // Create a run directory for the submission's container
        string runDirectory = Path.Join(Paths.ContainerMountPath, codeSubmission.Id.ToString());
        Directory.CreateDirectory(runDirectory);
        
        // Get the old code file path
        string oldCodeFilePath = Path.Join(Paths.SubmissionsPath, codeSubmission.Id.ToString());
        
        // Get the new code file path
        string containerCodeFilePath = Path.Join(runDirectory, "code");
        
        // Copy the code file to the container folder
        File.Copy(oldCodeFilePath, containerCodeFilePath, true);
        
        logger.LogInformation("Copied the code file to the container folder '{Directory}' for the code submission {CodeSubmission}",runDirectory,  codeSubmission.Id);

        return runDirectory;
    }

    public async Task RunCodeSubmission(CodeSubmission codeSubmission, RunningContainerInstance containerInstance)
    {
        // byte[] commandBuffer = "echo 'Hello, World!' > output.txt"u8.ToArray();
        // await containerInstance.ShellStream.WriteAsync(commandBuffer, 0, commandBuffer.Length, CancellationToken.None);
        //
        // containerInstance.ShellStream.CloseWrite();
    }
}