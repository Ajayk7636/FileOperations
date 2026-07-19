using System.Threading;
using System.Threading.Tasks;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Core.Interfaces
{


public interface IOperationHandler
{
    Task<bool> ValidateAsync(Job job, CancellationToken cancellationToken);
    Task ExecuteAsync(Job job, CancellationToken cancellationToken);
    Task PauseAsync(Job job);
    Task ResumeAsync(Job job);
    Task CancelAsync(Job job);
    Task<bool> PostVerifyAsync(Job job, CancellationToken cancellationToken);
    Task RollbackAsync(Job job);
}
}
