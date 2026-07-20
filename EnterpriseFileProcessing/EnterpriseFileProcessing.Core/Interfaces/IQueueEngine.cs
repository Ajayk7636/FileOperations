using System.Threading;
using System.Threading.Tasks;

namespace EnterpriseFileProcessing.Core.Interfaces
{


public interface IQueueEngine
{
    void Start(CancellationToken cancellationToken);
    void Stop();
}
}
