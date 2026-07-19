using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;
using EnterpriseFileProcessing.Service.Handlers;

namespace EnterpriseFileProcessing.Service.Engine;

public class ProcessingPipeline : IProcessingPipeline
{
    private readonly IJobRepository _jobRepository;

    public ProcessingPipeline(IJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task ProcessJobAsync(Job job, CancellationToken cancellationToken)
    {
        try
        {
            job.State = JobState.Running;
            job.StartedAt = DateTime.UtcNow;
            _jobRepository.UpdateJob(job);
            _jobRepository.AddJobLog(job.JobId, "Job started", "Info");

            // 1. Factory - Get Handler
            var handler = OperationFactory.GetHandler(job.OperationType);

            // 2. Pre-Verification (Global) - simplified
            _jobRepository.AddJobLog(job.JobId, "Running pre-verification...", "Info");

            // 3. Operation specific validation
            bool isValid = await handler.ValidateAsync(job, cancellationToken);
            if (!isValid)
            {
                throw new Exception("Operation validation failed.");
            }

            // 4. Execution
            _jobRepository.AddJobLog(job.JobId, "Executing job...", "Info");
            await handler.ExecuteAsync(job, cancellationToken);

            // 5. Post Verification
            _jobRepository.AddJobLog(job.JobId, "Running post-verification...", "Info");
            await handler.PostVerifyAsync(job, cancellationToken);

            job.State = JobState.Completed;
            job.CompletedAt = DateTime.UtcNow;
            _jobRepository.UpdateJob(job);
            _jobRepository.AddJobLog(job.JobId, "Job completed successfully", "Success");
        }
        catch (OperationCanceledException)
        {
            job.State = JobState.Cancelled;
            _jobRepository.UpdateJob(job);
            _jobRepository.AddJobLog(job.JobId, "Job was cancelled", "Warning");
        }
        catch (Exception ex)
        {
            job.State = JobState.Failed;
            job.ErrorMessage = ex.Message;
            _jobRepository.UpdateJob(job);
            _jobRepository.AddJobLog(job.JobId, $"Job failed: {ex.Message}", "Error", ex.StackTrace ?? string.Empty);
        }
    }
}
