using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public interface ICommunicationProcessor<T> where T : CommunicationJob, new()
    {
        string Name { get; set; }
        int MaxBatchCount { get; set; }
        Task<T[]> ProcessAsync(params T[] jobs);
    }
}