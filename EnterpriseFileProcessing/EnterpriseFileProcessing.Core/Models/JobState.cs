namespace EnterpriseFileProcessing.Core.Models;

public enum JobState
{
    Waiting = 1,
    Ready = 2,
    Running = 3,
    Paused = 4,
    Cancelled = 5,
    Completed = 6,
    Failed = 7,
    Retry = 8,
    Scheduled = 9,
    ApprovalPending = 10
}
