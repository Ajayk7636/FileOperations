using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using EnterpriseFileProcessing.Core.Interfaces;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Data.Repositories
{
    public class SqlJobRepository : IJobRepository
    {
        private readonly string _connectionString;

        public SqlJobRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Job GetJobById(Guid jobId)
        {
            Job job = null;
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("SELECT JobId, Name, OperationType, State, Priority, RequestJson, CreatedAt, StartedAt, CompletedAt, RetryCount, MaxRetries, ErrorMessage, CreatedBy FROM Jobs WHERE JobId = @JobId", connection))
            {
                command.Parameters.AddWithValue("@JobId", jobId);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        job = MapRowToJob(reader);
                    }
                }
            }
            return job;
        }

        public IEnumerable<Job> GetJobsByState(JobState state, int maxItems = 10)
        {
            var jobs = new List<Job>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("SELECT TOP (@MaxItems) JobId, Name, OperationType, State, Priority, RequestJson, CreatedAt, StartedAt, CompletedAt, RetryCount, MaxRetries, ErrorMessage, CreatedBy FROM Jobs WHERE State = @State ORDER BY Priority DESC, CreatedAt ASC", connection))
            {
                command.Parameters.AddWithValue("@State", (int)state);
                command.Parameters.AddWithValue("@MaxItems", maxItems);

                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jobs.Add(MapRowToJob(reader));
                        }
                    }
                }
                catch { /* Allow graceful failure if DB is offline during polling */ }
            }
            return jobs;
        }

        public void UpdateJob(Job job)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
                UPDATE Jobs
                SET State = @State, StartedAt = @StartedAt, CompletedAt = @CompletedAt,
                    RetryCount = @RetryCount, ErrorMessage = @ErrorMessage
                WHERE JobId = @JobId", connection))
            {
                command.Parameters.AddWithValue("@JobId", job.JobId);
                command.Parameters.AddWithValue("@State", (int)job.State);
                command.Parameters.AddWithValue("@StartedAt", (object)job.StartedAt ?? DBNull.Value);
                command.Parameters.AddWithValue("@CompletedAt", (object)job.CompletedAt ?? DBNull.Value);
                command.Parameters.AddWithValue("@RetryCount", job.RetryCount);
                command.Parameters.AddWithValue("@ErrorMessage", (object)job.ErrorMessage ?? DBNull.Value);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex) { Console.WriteLine($"DB Error updating job: {ex.Message}"); }
            }
        }

        public void InsertJob(Job job)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
                INSERT INTO Jobs (JobId, Name, OperationType, State, Priority, RequestJson, CreatedAt, MaxRetries, CreatedBy)
                VALUES (@JobId, @Name, @OperationType, @State, @Priority, @RequestJson, @CreatedAt, @MaxRetries, @CreatedBy)", connection))
            {
                command.Parameters.AddWithValue("@JobId", job.JobId);
                command.Parameters.AddWithValue("@Name", job.Name ?? string.Empty);
                command.Parameters.AddWithValue("@OperationType", (int)job.OperationType);
                command.Parameters.AddWithValue("@State", (int)job.State);
                command.Parameters.AddWithValue("@Priority", job.Priority);
                command.Parameters.AddWithValue("@RequestJson", job.RequestJson ?? string.Empty);
                command.Parameters.AddWithValue("@CreatedAt", job.CreatedAt);
                command.Parameters.AddWithValue("@MaxRetries", job.MaxRetries);
                command.Parameters.AddWithValue("@CreatedBy", job.CreatedBy ?? string.Empty);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex) { Console.WriteLine($"DB Error inserting job: {ex.Message}"); }
            }
        }

        public void AddJobLog(Guid jobId, string message, string status, string exception = "")
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
                INSERT INTO JobLogs (JobId, LogTime, Message, Status, Exception)
                VALUES (@JobId, @LogTime, @Message, @Status, @Exception)", connection))
            {
                command.Parameters.AddWithValue("@JobId", jobId);
                command.Parameters.AddWithValue("@LogTime", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Message", message);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Exception", exception ?? string.Empty);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex) { Console.WriteLine($"DB Error adding log: {ex.Message}"); }
            }
            Console.WriteLine($"[DB LOG] JobId: {jobId} | Status: {status} | Msg: {message}");
        }

        private Job MapRowToJob(IDataRecord reader)
        {
            return new Job
            {
                JobId = reader.GetGuid(reader.GetOrdinal("JobId")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                OperationType = (OperationType)reader.GetInt32(reader.GetOrdinal("OperationType")),
                State = (JobState)reader.GetInt32(reader.GetOrdinal("State")),
                Priority = reader.GetInt32(reader.GetOrdinal("Priority")),
                RequestJson = reader.GetString(reader.GetOrdinal("RequestJson")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                RetryCount = reader.GetInt32(reader.GetOrdinal("RetryCount")),
                MaxRetries = reader.GetInt32(reader.GetOrdinal("MaxRetries")),
                ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? string.Empty : reader.GetString(reader.GetOrdinal("ErrorMessage")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy"))
            };
        }
    }
}
