using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace CodeEvaluator.Data.Models;

public class CodeSubmission
{
    public static CodeSubmission Empty => new()
    {
        Id = Guid.Empty,
        Language = CodeLanguage.CSharp,
        QueuedAt = DateTime.MinValue,
        Status = CodeSubmissionStatus.Queued
    };

    public Guid Id { get; init; }

    [Comment("The language of the code to be executed.")]
    [Required]
    public required CodeLanguage Language { get; init; }

    [Required]
    public required DateTime QueuedAt { get; init; } = DateTime.UtcNow;

    [Comment("The date and time when the code execution was started.")]
    public DateTime? StartedAt { get; set; }

    [Comment(
        "The date and time when the code execution was completed. This field is both set on successful and failed code execution.")]
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