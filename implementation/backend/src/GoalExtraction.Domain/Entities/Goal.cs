namespace GoalExtraction.Domain.Entities;

using System.ComponentModel.DataAnnotations;
using GoalExtraction.Domain.Common;
using GoalExtraction.Domain.Enums;

public class Goal : BaseEntity
{
    [Required]
    public Guid EmployeeId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public Employee? Employee { get; set; }

    [Required]
    [MaxLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public GoalCategory Category { get; set; } = GoalCategory.PERFORMANCE;

    [MaxLength(500)]
    public string Metric { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Timeframe { get; set; } = string.Empty;

    public GoalPriority Priority { get; set; } = GoalPriority.MEDIUM;

    public GoalStatus Status { get; set; } = GoalStatus.ACTIVE;

    public GoalProvenance Provenance { get; set; } = GoalProvenance.AI_ORIGINAL;

    [MaxLength(64)]
    public string SourceTranscriptHash { get; set; } = string.Empty;

    public Guid? ReviewerId { get; set; }
}
