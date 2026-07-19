using System;
using System.Threading;
using System.Threading.Tasks;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Data.Repositories;
using EnterpriseFileProcessing.Service.Engine;

namespace EnterpriseFileProcessing.Service;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Enterprise File Processing Service...");

        // Setup Dependency Injection (Manual for simplicity in this console host,
        // in a real Windows Service we'd use Microsoft.Extensions.DependencyInjection or AutoFac)
        IJobRepository jobRepository = new SqlJobRepository();
        IProcessingPipeline pipeline = new ProcessingPipeline(jobRepository);
        IQueueEngine queueEngine = new QueueEngine(jobRepository, pipeline);

        using var cts = new CancellationTokenSource();

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
