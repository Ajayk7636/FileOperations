using System;

namespace EnterpriseFileProcessing.Core.Models
{


public class Job
{
    public Guid JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public OperationType OperationType { get; set; }
    public JobState State { get; set; }
    public int Priority { get; set; }
    public string RequestJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}
}
