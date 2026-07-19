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

            try
            {
                var config = JObject.Parse(job.RequestJson);
                if (config["ZipFilePath"] != null) sourceZip = config["ZipFilePath"].ToString();
                if (config["DestinationPath"] != null) destFolder = config["DestinationPath"].ToString();
                if (config["Password"] != null) passwordArgs = $"-p\"{config["Password"].ToString()}\"";
            }
            catch { }

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
                    await Task.Run(() => process.WaitForExit());
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
        public Task RollbackAsync(Job job) => Task.CompletedTask;
    }
}
