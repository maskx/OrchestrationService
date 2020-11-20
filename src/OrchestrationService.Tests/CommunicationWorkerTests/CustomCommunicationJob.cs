using maskx.OrchestrationService.Worker;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    [Table("Communication")]
    public class CustomCommunicationJob : CommunicationJob
    {
        public string SubscriptionId { get; set; }
        public string ManagementUnit { get; set; }
    }
}
