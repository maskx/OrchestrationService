using maskx.OrchestrationService.Worker;
using System.Threading.Tasks;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class MockCommunicationProcessor : ICommunicationProcessor
    {
        public string Name { get; set; } = "MockCommunicationProcessor";

        public async Task<CommunicationJob> ProcessAsync(CommunicationJob job)
        {
            job.ResponseCode = 200;
            job.ResponseContent = "MockCommunicationProcessor";
            return job;
        }
    }
}