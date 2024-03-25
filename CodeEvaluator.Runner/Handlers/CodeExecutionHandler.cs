using CodeEvaluator.Data;
using CodeEvaluator.Data.Models;

namespace CodeEvaluator.Runner.Handlers;

/// <summary>
/// This class is responsible for handling the execution of code submissions.
/// This includes setting up the container folder before execution, and cleaning up the folder after execution,
/// including extracting the stdout and stderr from the container. Other files may be extracted as well.
/// </summary>
/// <param name="logger">A logger instance for logging messages.</param>
public class CodeExecutionHandler(ILogger<CodeExecutionHandler> logger)
{
    private static readonly string CodeFilesPath = Path.Join(Paths.ApplicationDataPath, "submissions");
    private static readonly string ContainerMountPath = Path.Join(Paths.ApplicationDataPath, "mnt");

    /// <summary>
    /// Writes the code submission to a file in the code files path.
    /// </summary>
    /// <param name="codeStream">The submitted code data stream.</param>
    /// <param name="codeLanguage"></param>
    /// <returns>The code submission object with the code submission data.</returns>
    public async Task<CodeSubmission> SetupContainerFolder(Stream codeStream, CodeLanguage codeLanguage)
    {
        // Create the code files path if it does not exist
        if (!Directory.Exists(CodeFilesPath))
        {
            Directory.CreateDirectory(CodeFilesPath);
        }

        // Save the code to a file
        Guid codeGuid;
        string codeFileName;

        // Ensure the file name is unique, if not, generate a new one
        do
        {
            codeGuid = Guid.NewGuid();
            codeFileName = codeGuid.ToString();
        } while (File.Exists(Path.Join(CodeFilesPath, codeFileName)));

        // Save the code to the file
        string codeFilePath = Path.Join(CodeFilesPath, codeFileName);
        await using (FileStream fileStream = new(codeFilePath, FileMode.Create, FileAccess.Write))
        {
            await codeStream.CopyToAsync(fileStream);
        }

        // Construct the code submission, return it
        return new CodeSubmission
        {
            Id = codeGuid,
            Language = codeLanguage,
            QueuedAt = DateTime.UtcNow,
            Status = CodeSubmission.CodeSubmissionStatus.Queued
        };
    }
}