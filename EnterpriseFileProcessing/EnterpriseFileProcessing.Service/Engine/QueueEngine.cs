using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Service.Engine;

public class QueueEngine : IQueueEngine
{
    private readonly IJobRepository _jobRepository;
    private readonly IProcessingPipeline _processingPipeline;
    private bool _isRunning = false;
    private const int MaxConcurrentJobs = 3;

    public QueueEngine(IJobRepository jobRepository, IProcessingPipeline processingPipeline)
    {
        _jobRepository = jobRepository;
        _processingPipeline = processingPipeline;
    }

    public void Start(CancellationToken cancellationToken)
    {
        _isRunning = true;
        Console.WriteLine("[QueueEngine] Started listening for jobs...");

        // Polling loop
        Task.Run(async () =>
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                var waitingJobs = _jobRepository.GetJobsByState(JobState.Waiting).ToList();

                // Get running jobs to respect MaxConcurrentJobs limits
                var runningCount = _jobRepository.GetJobsByState(JobState.Running).Count();

                foreach (var job in waitingJobs)
                {
                    if (runningCount >= MaxConcurrentJobs)
                        break;

                    runningCount++;

                    Console.WriteLine($"[QueueEngine] Dispatching Job: {job.JobId}");

                    // Fire and forget so we can process multiple
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _processingPipeline.ProcessJobAsync(job, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[QueueEngine] Error dispatching job: {ex.Message}");
                        }
                    }, cancellationToken);
                }

                await Task.Delay(2000, cancellationToken); // Poll every 2 seconds
            }
        }, cancellationToken);
    }

    public void Stop()
    {
        _isRunning = false;
        Console.WriteLine("[QueueEngine] Stopped.");
    }
}
