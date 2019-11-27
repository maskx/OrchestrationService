using System.Threading.Tasks;

namespace maskx.OrchestrationService.Worker
{
    public interface ICommunicationProcessor
    {
        string Name { get; set; }

        Task<CommunicationJob> ProcessAsync(CommunicationJob job);
    }
}