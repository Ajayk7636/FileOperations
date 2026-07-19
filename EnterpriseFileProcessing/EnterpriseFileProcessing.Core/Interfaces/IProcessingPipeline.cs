using System.Threading;
using System.Threading.Tasks;
using EnterpriseFileProcessing.Core.Models;

namespace EnterpriseFileProcessing.Core.Interfaces;

public interface IProcessingPipeline
{
    Task ProcessJobAsync(Job job, CancellationToken cancellationToken);
}
