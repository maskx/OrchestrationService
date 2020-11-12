using maskx.OrchestrationService.Worker;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    public class CustomCommunicationJob : CommunicationJob
    {
        public string SubscriptionId { get; set; }
        public string ManagementUnit { get; set; }
    }
}
