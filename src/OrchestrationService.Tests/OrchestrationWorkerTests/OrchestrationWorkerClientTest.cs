using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System;
using Xunit;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    [Collection("WorkerHost Collection")]
    [Trait("c", "OrchestrationWorkerClientTest")]
    public class OrchestrationWorkerClientTest
    {
        private WorkerHostFixture fixture;

        public OrchestrationWorkerClientTest(WorkerHostFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "JumpStartOrchestrationAsync")]
        public void JumpStartOrchestrationAsync()
        {
            var instance = fixture.OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).Name,
                },
                Input = ""
            }).Result;
            while (true)
            {
                var result = fixture.OrchestrationWorkerClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    Assert.Equal("1", result.Output);
                    break;
                }
            }
        }
    }
}