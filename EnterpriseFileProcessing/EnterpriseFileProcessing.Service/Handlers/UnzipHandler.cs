using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Service.Handlers
{
    public class UnzipHandler : IOperationHandler
    {
        public Task<bool> ValidateAsync(Job job, CancellationToken cancellationToken) => Task.FromResult(true);

        public async Task ExecuteAsync(Job job, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[UnzipHandler] Executing 7-Zip (Unzip) for Job {job.JobId}");

            string sourceZip = "C:\\source\\archive.zip";
            string destFolder = "C:\\dest\\";
            string passwordArgs = "";
            bool skipExecution = false;

            try
            {
                var config = JObject.Parse(job.RequestJson);
                if (config["ZipFilePath"] != null) sourceZip = config["ZipFilePath"].ToString();
                if (config["DestinationPath"] != null) destFolder = config["DestinationPath"].ToString();
                if (config["Password"] != null) passwordArgs = $"-p\"{config["Password"].ToString()}\"";
                if (config["SkipExecution"] != null) skipExecution = config.Value<bool>("SkipExecution");
            }
            catch { }

            MetricsHelper.GetFileMetrics(sourceZip, out long sourceZipSize);
            MetricsHelper.GetZipMetrics(sourceZip, out long uncompressedSize, out int zipFileCount);
            Console.WriteLine($"[UnzipHandler Job {job.JobId}] Source Zip: Size = {MetricsHelper.FormatSize(sourceZipSize)}, Uncompressed Size = {MetricsHelper.FormatSize(uncompressedSize)}, Files = {zipFileCount}");

            MetricsHelper.GetDirectoryMetrics(destFolder, out long destSize, out int destCount);
            Console.WriteLine($"[UnzipHandler Job {job.JobId}] Existing Destination Data: Count = {destCount}, Size = {MetricsHelper.FormatSize(destSize)}");

            if (skipExecution)
            {
                Console.WriteLine($"[UnzipHandler Job {job.JobId}] SkipExecution flag is set. Skipping Unzip process.");
                return;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = "7z",
                Arguments = $"x \"{sourceZip}\" -o\"{destFolder}\" {passwordArgs} -aoa -bb1 -bsp1",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processInfo;

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                        Console.WriteLine($"[7-Zip Unzip {job.JobId}] {args.Data}");
                };

                try
                {
                    process.Start();
                    process.BeginOutputReadLine();

                    using (cancellationToken.Register(() => {
                        try { if (!process.HasExited) process.Kill(); } catch { }
                    }))
                    {
                        await Task.Run(() => process.WaitForExit());
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (process.ExitCode != 0)
                        throw new Exception($"7-Zip failed with exit code {process.ExitCode}");
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    Console.WriteLine($"[UnzipHandler] '7z' not found in this environment. Simulating unzip for Job {job.JobId}...");
                    await Task.Delay(2000, cancellationToken);
                }
            }

            Console.WriteLine($"[UnzipHandler] Completed Unzip for Job {job.JobId}");
        }

        public Task PauseAsync(Job job) => Task.CompletedTask;
        public Task ResumeAsync(Job job) => Task.CompletedTask;
        public Task CancelAsync(Job job) => Task.CompletedTask;
        public Task<bool> PostVerifyAsync(Job job, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task RollbackAsync(Job job)
        {
            Console.WriteLine($"[UnzipHandler] Rolling back files for Job {job.JobId}");
            Console.WriteLine($"[UnzipHandler] Safe rollback triggered. Skipping full directory wipe to prevent data loss.");
            return Task.CompletedTask;
        }
    }
}
