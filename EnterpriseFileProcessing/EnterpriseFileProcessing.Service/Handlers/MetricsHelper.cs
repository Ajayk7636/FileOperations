using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EnterpriseFileProcessing.Service.Handlers
{
    public static class MetricsHelper
    {
        public static void GetDirectoryMetrics(string path, out long size, out int count)
        {
            size = 0;
            count = 0;
            if (!Directory.Exists(path)) return;

            try
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                count = files.Length;
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        size += info.Length;
                    }
                    catch (Exception)
                    {
                        // Ignore files we cannot access
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MetricsHelper] Error getting directory metrics for {path}: {ex.Message}");
            }
        }

        public static void GetFileMetrics(string path, out long size)
        {
            size = 0;
            if (!File.Exists(path)) return;
            try
            {
                var info = new FileInfo(path);
                size = info.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MetricsHelper] Error getting file metrics for {path}: {ex.Message}");
            }
        }

        public static void GetZipMetrics(string zipPath, out long uncompressedSize, out int fileCount)
        {
            uncompressedSize = 0;
            fileCount = 0;

            if (!File.Exists(zipPath)) return;

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "7z",
                    Arguments = $"l \"{zipPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = processInfo;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        // Example 7z l output:
                        // ------------------- ----- ------------ ------------  ------------------------
                        //                      2342          834        52528  12 files, 4 folders
                        // We can match the last line with numbers:

                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length > 0)
                        {
                            string lastLine = lines[lines.Length - 1];

                            // A simple regex to parse the final summary line from 7z output
                            // Format generally looks like: "      52528        1234  12 files" or similar
                            // Try to match the file count
                            var fileMatch = Regex.Match(lastLine, @"(\d+)\s+files?");
                            if (fileMatch.Success)
                            {
                                int.TryParse(fileMatch.Groups[1].Value, out fileCount);
                            }

                            // Match the total size (first big number on the last summary line after dashed line)
                            // More robustly, grab the line before "files, folders" or just rely on the columns.
                            // 7z output:
                            // Date      Time    Attr         Size   Compressed  Name
                            // ------------------- ----- ------------ ------------  ------------------------
                            // ...
                            // ------------------- ----- ------------ ------------  ------------------------
                            //                             <Uncomp>       <Comp>  X files

                            var sizeMatch = Regex.Match(lastLine, @"^\s*(\d+)\s+(\d+)?\s+(\d+)\s+files?");
                            if (sizeMatch.Success)
                            {
                                long.TryParse(sizeMatch.Groups[1].Value, out uncompressedSize);
                            }
                            else
                            {
                                // fallback simple approach for Uncompressed size which is the first large number
                                var numbers = Regex.Matches(lastLine, @"\b\d+\b");
                                if (numbers.Count >= 2 && lastLine.Contains("files"))
                                {
                                    long.TryParse(numbers[0].Value, out uncompressedSize);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MetricsHelper] Error getting zip metrics for {zipPath}: {ex.Message}");
            }
        }

        public static string FormatSize(long bytes)
        {
            double gb = bytes / (1024.0 * 1024.0 * 1024.0);
            return $"{bytes} Bytes ({gb:0.00} GB)";
        }
    }
}
