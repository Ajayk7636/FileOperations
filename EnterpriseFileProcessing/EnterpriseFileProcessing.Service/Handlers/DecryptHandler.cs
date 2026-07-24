using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Service.Handlers
{
    public class DecryptHandler : IOperationHandler
    {
        public Task<bool> ValidateAsync(Job job, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public async Task ExecuteAsync(Job job, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[DecryptHandler] Executing Decryption for Job {job.JobId}");

            string sourcePath = "C:\\source\\file.enc";
            string destPath = "C:\\dest\\file.dec";
            bool skipExecution = false;

            try
            {
                var config = JObject.Parse(job.RequestJson);
                if (config["SourcePath"] != null) sourcePath = config["SourcePath"].ToString();
                if (config["DestinationPath"] != null) destPath = config["DestinationPath"].ToString();
                if (config["SkipExecution"] != null) skipExecution = config.Value<bool>("SkipExecution");
            }
            catch { }

            // Assume it can be a file or directory for now. Check file first, then directory.
            long srcSize = 0;
            int srcCount = 0;

            MetricsHelper.GetFileMetrics(sourcePath, out srcSize);
            if (srcSize > 0)
            {
                srcCount = 1;
            }
            else
            {
                MetricsHelper.GetDirectoryMetrics(sourcePath, out srcSize, out srcCount);
            }

            Console.WriteLine($"[DecryptHandler Job {job.JobId}] Source Data: Count = {srcCount}, Size = {MetricsHelper.FormatSize(srcSize)}");

            long destSize = 0;
            int destCount = 0;
            MetricsHelper.GetFileMetrics(destPath, out destSize);
            if (destSize > 0)
            {
                destCount = 1;
            }
            else
            {
                MetricsHelper.GetDirectoryMetrics(destPath, out destSize, out destCount);
            }

            Console.WriteLine($"[DecryptHandler Job {job.JobId}] Existing Destination Data: Count = {destCount}, Size = {MetricsHelper.FormatSize(destSize)}");

            if (skipExecution)
            {
                Console.WriteLine($"[DecryptHandler Job {job.JobId}] SkipExecution flag is set. Skipping Decryption process.");
                return;
            }

            Console.WriteLine($"[DecryptHandler Job {job.JobId}] Decrypting...");
            // Simulated Decryption logic goes here
            await Task.Delay(2000, cancellationToken);

            Console.WriteLine($"[DecryptHandler Job {job.JobId}] Completed Decryption.");
        }

        public Task PauseAsync(Job job) => Task.CompletedTask;
        public Task ResumeAsync(Job job) => Task.CompletedTask;
        public Task CancelAsync(Job job) => Task.CompletedTask;
        public Task<bool> PostVerifyAsync(Job job, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task RollbackAsync(Job job)
        {
            Console.WriteLine($"[DecryptHandler] Rolling back files for Job {job.JobId}");
            return Task.CompletedTask;
        }
    }
}
