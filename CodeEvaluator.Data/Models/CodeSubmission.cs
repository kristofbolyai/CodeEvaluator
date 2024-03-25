using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CodeEvaluator.Data.Models;

[Index(nameof(CodeFileName), IsUnique = true)]
public class CodeSubmission
{
    public int Id { get; init; }
    
    [Comment("The name of the file containing the code to be executed. This file must be located in the specified code directory.")]
    [Required]
    public required string CodeFileName { get; init; }
    
    [Required]
    public required DateTime QueuedAt { get; init; } = DateTime.UtcNow;
    
    [Comment("The date and time when the code execution was started.")]
    public DateTime? StartedAt { get; set; }
    
    [Comment("The date and time when the code execution was completed. This field is both set on successful and failed code execution.")]
    public DateTime? FinishedAt { get; set; }
    
    [Required]
    public required CodeSubmissionStatus Status { get; set; } = CodeSubmissionStatus.Queued;
    
    public enum CodeSubmissionStatus
    {
        Queued,
        Running,
        Completed,
        Failed
    }
}