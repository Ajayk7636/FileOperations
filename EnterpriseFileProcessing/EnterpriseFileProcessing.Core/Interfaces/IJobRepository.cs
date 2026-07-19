using System;
using System.Collections.Generic;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Core.Interfaces
{


public interface IJobRepository
{
    Job GetJobById(Guid jobId);
    IEnumerable<Job> GetJobsByState(JobState state, int maxItems = 10);
    void UpdateJob(Job job);
    void InsertJob(Job job);
    void AddJobLog(Guid jobId, string message, string status, string exception = "");
}
}
