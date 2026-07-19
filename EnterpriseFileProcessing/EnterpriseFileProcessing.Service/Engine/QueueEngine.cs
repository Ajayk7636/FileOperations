using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Service.Engine
{


public class QueueEngine : IQueueEngine
{
    private readonly IJobRepository _jobRepository;
    private readonly IProcessingPipeline _processingPipeline;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _activeJobs;
    private bool _isRunning = false;
    private const int MaxConcurrentJobs = 3;

    public QueueEngine(IJobRepository jobRepository, IProcessingPipeline processingPipeline)
    {
        _jobRepository = jobRepository;
        _processingPipeline = processingPipeline;
            _activeJobs = new ConcurrentDictionary<Guid, CancellationTokenSource>();
    }

    public void Start(CancellationToken cancellationToken)
    {
        _isRunning = true;

        // Recover interrupted jobs on startup
        var runningJobs = _jobRepository.GetJobsByState(JobState.Running, 100).ToList();
        foreach (var job in runningJobs)
        {
            Console.WriteLine($"[QueueEngine] Recovering interrupted job {job.JobId}");
            job.State = JobState.Waiting; // Reset to Waiting to retry
            _jobRepository.UpdateJob(job);
            _jobRepository.AddJobLog(job.JobId, "Job recovered from interrupted state on service startup", "Info");
        }

        Console.WriteLine("[QueueEngine] Started listening for jobs...");

        // Polling loop
        Task.Run(async () =>
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                // 1. Check for User-Initiated Cancellations
                var cancelledJobs = _jobRepository.GetJobsByState(JobState.Cancelled, 100).ToList();
                foreach (var cancelledJob in cancelledJobs)
                {
                    if (_activeJobs.TryGetValue(cancelledJob.JobId, out var jobTokenSource))
                    {
                        if (!jobTokenSource.IsCancellationRequested)
                        {
                            Console.WriteLine($"[QueueEngine] User cancellation detected for Job: {cancelledJob.JobId}");
                            jobTokenSource.Cancel();
                        }
                    }
                }

                // 2. Dispatch waiting jobs
                var waitingJobs = _jobRepository.GetJobsByState(JobState.Waiting).ToList();
                var runningCount = _activeJobs.Count;

                foreach (var job in waitingJobs)
                {
                    if (runningCount >= MaxConcurrentJobs)
                        break;

                    runningCount++;

                    Console.WriteLine($"[QueueEngine] Dispatching Job: {job.JobId}");

                    // Create linked token so global shutdown ALSO cancels the job
                    var jobCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _activeJobs.TryAdd(job.JobId, jobCts);

                    // Fire and forget so we can process multiple
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _processingPipeline.ProcessJobAsync(job, jobCts.Token);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[QueueEngine] Error dispatching job: {ex.Message}");
                        }
                        finally
                        {
                            _activeJobs.TryRemove(job.JobId, out _);
                            jobCts.Dispose();
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
}
