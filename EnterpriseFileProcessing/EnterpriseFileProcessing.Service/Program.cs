using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;
using EnterpriseFileProcessing.Data.Repositories;
using EnterpriseFileProcessing.Service.Engine;

namespace EnterpriseFileProcessing.Service
{


class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Enterprise File Processing Service...");

        // Setup Dependency Injection (Manual for simplicity in this console host,
        // in a real Windows Service we'd use Microsoft.Extensions.DependencyInjection or AutoFac)
        var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
            ?? "Server=.;Database=EnterpriseFileProcessing;Trusted_Connection=True;";
        IJobRepository jobRepository = new SqlJobRepository(connectionString);
        IEmailNotificationEngine emailEngine = new EmailNotificationEngine();
        IProcessingPipeline pipeline = new ProcessingPipeline(jobRepository, emailEngine);
        IQueueEngine queueEngine = new QueueEngine(jobRepository, pipeline);

        if (args != null && args.Length > 0)
        {
            try
            {
                string jsonInput = string.Join(" ", args);
                Console.WriteLine($"[Program] Received input arguments, attempting to parse as JSON...");

                List<Job> jobsToQueue = new List<Job>();

                // Try to parse as a list first, if it fails, try parsing as a single object
                if (jsonInput.TrimStart().StartsWith("["))
                {
                    jobsToQueue = JsonConvert.DeserializeObject<List<Job>>(jsonInput);
                }
                else
                {
                    var singleJob = JsonConvert.DeserializeObject<Job>(jsonInput);
                    if (singleJob != null)
                    {
                        jobsToQueue.Add(singleJob);
                    }
                }

                if (jobsToQueue != null && jobsToQueue.Any())
                {
                    foreach (var job in jobsToQueue)
                    {
                        if (job.JobId == Guid.Empty)
                        {
                            job.JobId = Guid.NewGuid();
                        }
                        job.State = JobState.Waiting;
                        if (job.CreatedAt == default(DateTime))
                        {
                            job.CreatedAt = DateTime.UtcNow;
                        }

                        Console.WriteLine($"[Program] Inserting Job {job.JobId} ({job.OperationType}) into the queue.");
                        jobRepository.InsertJob(job);
                    }
                }
                else
                {
                    Console.WriteLine("[Program] Failed to parse input as Job(s).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] Error parsing JSON arguments: {ex.Message}");
            }
        }

        using (var cts = new CancellationTokenSource())
        {
            // Capture Ctrl+C for graceful shutdown
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Shutdown requested...");
                e.Cancel = true;
                cts.Cancel();
            };

            // Start Queue Engine
            queueEngine.Start(cts.Token);

            Console.WriteLine("Service is running. Press Ctrl+C to stop.");

            try
            {
                // Keep app running until cancellation is requested
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected on shutdown
            }
            finally
            {
                queueEngine.Stop();
                Console.WriteLine("Service stopped gracefully.");
            }
        }
    }
}
}
