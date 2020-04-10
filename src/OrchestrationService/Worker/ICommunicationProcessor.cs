using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public interface ICommunicationProcessor
    {
        string Name { get; set; }
        int MaxBatchCount { get; set; }
        CommunicationWorker CommunicationWorker { get; set; }

        Task<CommunicationJob[]> ProcessAsync(params CommunicationJob[] jobs);
    }
}