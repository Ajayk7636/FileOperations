using System;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Service.Engine
{
    public interface IEmailNotificationEngine
    {
        void SendStatusEmail(Job job, string templateName);
    }

    public class EmailNotificationEngine : IEmailNotificationEngine
    {
        public void SendStatusEmail(Job job, string templateName)
        {
            // In a real application, this would use SmtpClient or SendGrid API
            // and merge properties from the job into an HTML template.
            Console.WriteLine($"[EMAIL] To: admin@company.com | Subject: Job {job.JobId} update | Template: {templateName} | Status: {job.State}");
        }
    }
}
