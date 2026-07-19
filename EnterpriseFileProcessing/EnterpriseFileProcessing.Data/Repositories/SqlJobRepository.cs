using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Data.Repositories;

// For the purpose of this solution, we'll use an In-Memory mock implementation of the repository
// In a real scenario, this would use ADO.NET / Dapper to interact with SQL Server
public class SqlJobRepository : IJobRepository
{
    private static readonly ConcurrentDictionary<Guid, Job> _jobs = new ConcurrentDictionary<Guid, Job>();

    public SqlJobRepository()
    {
        // Seed some data if empty
        if (_jobs.IsEmpty)
        {
            var jobId = Guid.NewGuid();
            _jobs.TryAdd(jobId, new Job
            {
                JobId = jobId,
                Name = "Test Copy Job",
                OperationType = OperationType.Copy,
                State = JobState.Waiting,
                CreatedAt = DateTime.UtcNow,
                RequestJson = "{\"SourcePath\":\"C:\\\\source\\\\\", \"DestinationPath\":\"C:\\\\dest\\\\\"}"
            });
        }
    }

    public Job GetJobById(Guid jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public IEnumerable<Job> GetJobsByState(JobState state, int maxItems = 10)
    {
        return _jobs.Values
            .Where(j => j.State == state)
            .OrderBy(j => j.CreatedAt)
            .Take(maxItems)
            .ToList();
    }

    public void UpdateJob(Job job)
    {
        _jobs.AddOrUpdate(job.JobId, job, (id, existing) => job);
        Console.WriteLine($"[DB] Job {job.JobId} updated to state {job.State}");
    }

    public void InsertJob(Job job)
    {
        _jobs.TryAdd(job.JobId, job);
    }

    public void AddJobLog(Guid jobId, string message, string status, string exception = "")
    {
        Console.WriteLine($"[DB LOG] JobId: {jobId} | Status: {status} | Msg: {message}");
    }
}
