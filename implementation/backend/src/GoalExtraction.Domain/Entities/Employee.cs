namespace GoalExtraction.Domain.Entities;

using System.ComponentModel.DataAnnotations;
using GoalExtraction.Domain.Common;

public class Employee : BaseEntity
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}
