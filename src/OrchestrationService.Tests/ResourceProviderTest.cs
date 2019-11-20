using maskx.OrchestrationService.Worker;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OrchestrationService.Tests
{
    [Collection("WorkerHost Collection")]
    public class ResourceProviderTest
    {
        [Fact]
        public void CreateOne()
        {
            JobProvider.Jobs.Add(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new maskx.OrchestrationService.Worker.Orchestration()
                {
                    Creator = "DefaultObjectCreator",
                    Uri = "OrchestrationService.Tests.Orchestration.PrepareVMTemplateAuthorizeOrchestration"
                }
            });
            while (true)
            {
                Task.Delay(1000).Wait();
            }
        }
    }
}