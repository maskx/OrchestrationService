using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DurableTask.Core;
using System.Threading.Tasks;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace OrchestrationService.Tests
{
    [Trait("c", "TraceActivityTest")]
    public class TraceActivityTest
    {
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;
        public OrchestrationWorkerClient OrchestrationWorkerClient { get; private set; }

        public TraceActivityTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
            List<(string Name, string Version, Type Type)> orchestrationTypes = new List<(string Name, string Version, Type Type)>();
            orchestrationTypes.Add((typeof(TestOrchestration).FullName, "", typeof(TestOrchestration)));
            workerHost = TestHelpers.CreateHostBuilder(options, orchestrationTypes).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync();
        }

        [Fact(DisplayName = "Trace")]
        public void Trace()
        {
            OrchestrationTrackingArgs trackingArgs = null;
            var _ = new TraceActivityEventListener((args) =>
            {
                trackingArgs = args;
            });
            var instance = OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).FullName
                },
                Input = ""
            }).Result;
            while (true)
            {
                var result = OrchestrationWorkerClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    Assert.Equal("1", result.Output);
                    break;
                }
            }
            Assert.NotNull(trackingArgs);
            Assert.Equal(trackingArgs.ExecutionId, instance.ExecutionId);
            Assert.Equal(trackingArgs.InstanceId, instance.InstanceId);
            Assert.Equal(EventLevel.Informational, trackingArgs.EventLevel);
            Assert.Equal("TestOrchestration-Begin", trackingArgs.EventType);
            Assert.Equal("123", trackingArgs.Message);
            Assert.Empty(trackingArgs.Info);
        }

        public class TestOrchestration : TaskOrchestration<int, string>
        {
            public override async Task<int> RunTask(OrchestrationContext context, string input)
            {
                await context.ScheduleTask<TaskResult>(typeof(TraceActivity), new TraceActivityInput()
                {
                    EventLevel = TraceEventType.Information,
                    EventType = "TestOrchestration-Begin",
                    Message = "123",
                });
                return 1;
            }
        }
    }
}