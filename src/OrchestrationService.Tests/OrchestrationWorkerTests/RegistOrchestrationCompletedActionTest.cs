using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System;
using Xunit;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    [Collection("WorkerHost Collection")]
    [Trait("c", "RegistOrchestrationCompletedActionTest")]
    public class RegistOrchestrationCompletedActionTest
    {
        private WorkerHostFixture fixture;

        public RegistOrchestrationCompletedActionTest(WorkerHostFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "RegistOrchestrationCompletedAction")]
        public void RegistOrchestrationCompletedAction()
        {
            OrchestrationCompletedArgs completedArgs = null;
            fixture.OrchestrationWorker.RegistOrchestrationCompletedAction((args) =>
            {
                completedArgs = args;
            });
            var instance = fixture.OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = typeof(TestOrchestration).FullName + "_"
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
            Assert.NotNull(completedArgs);
            Assert.Equal(completedArgs.ExecutionId, instance.ExecutionId);
            Assert.Equal(completedArgs.InstanceId, instance.InstanceId);
            Assert.True(completedArgs.Status);
            Assert.False(completedArgs.IsSubOrchestration);
            Assert.Empty(completedArgs.Id);
            Assert.Empty(completedArgs.ParentExecutionId);
            Assert.Equal("1", completedArgs.Result);
        }
    }
}