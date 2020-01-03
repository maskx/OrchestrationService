using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System;
using Xunit;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    [Collection("WorkerHost Collection")]
    public class ResourceProviderTest
    {
        [Fact]
        public void CreateOne()
        {
            var instance = new OrchestrationInstance() { InstanceId = Guid.NewGuid().ToString("N") };
            JobProvider.Jobs.Add(new Job()
            {
                InstanceId = instance.InstanceId,
                Orchestration = new OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = "OrchestrationService.Tests.Orchestration.PrepareVMTemplateAuthorizeOrchestration_"
                },
                Input = "Input:" + Guid.NewGuid().ToString()
            });
            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    break;
                }
            }
        }
    }
}