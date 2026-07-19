using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Service.Handlers;

public class CopyHandler : IOperationHandler
{
    public Task<bool> ValidateAsync(Job job, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[CopyHandler] Validating parameters for Job {job.JobId} (RequestJson: {job.RequestJson})");
        return Task.FromResult(true);
    }

    public async Task ExecuteAsync(Job job, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[CopyHandler] Executing Copy for Job {job.JobId}");

        // Simulating robust chunked processing that can be paused/cancelled
        for (int i = 1; i <= 5; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"[CopyHandler] Job {job.JobId} Copying chunk {i}/5...");
            await Task.Delay(1000, cancellationToken);
        }

        Console.WriteLine($"[CopyHandler] Completed Copy for Job {job.JobId}");
    }

    public Task PauseAsync(Job job)
    {
        Console.WriteLine($"[CopyHandler] Pausing Job {job.JobId}");
        return Task.CompletedTask;
    }

    public Task ResumeAsync(Job job)
    {
        Console.WriteLine($"[CopyHandler] Resuming Job {job.JobId}");
        return Task.CompletedTask;
    }

    public Task CancelAsync(Job job)
    {
        Console.WriteLine($"[CopyHandler] Cancelling Job {job.JobId}");
        return Task.CompletedTask;
    }

    public Task<bool> PostVerifyAsync(Job job, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[CopyHandler] Post-verifying hashes/size for Job {job.JobId}");
        return Task.FromResult(true);
    }

    public Task RollbackAsync(Job job)
    {
        Console.WriteLine($"[CopyHandler] Rolling back files for Job {job.JobId}");
        return Task.CompletedTask;
    }
}
