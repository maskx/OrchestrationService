using DurableTask.Core.Serializing;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using DurableTask.Core;
using System.Threading.Tasks;
using Xunit;
using maskx.OrchestrationService;

namespace OrchestrationService.Tests
{
    [Trait("c", "NameVersionDICreatorTest")]
    public class NameVersionDICreatorTest : IDisposable
    {
        private DataConverter dataConverter = new JsonDataConverter();
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;
        private OrchestrationWorkerClient orchestrationWorkerClient;

        public NameVersionDICreatorTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
            List<(string Name, string Version, Type Type)> orchestrationTypes = new List<(string Name, string Version, Type Type)>();
            orchestrationTypes.Add(("TestOrchestration", "1", typeof(TestOrchestrationV1)));
            orchestrationTypes.Add(("TestOrchestration", "2", typeof(TestOrchestrationV2)));
            orchestrationTypes.Add((typeof(TestOrchestration).FullName, "", typeof(TestOrchestration)));
            List<(string Name, string Version, Type Type)> activityTypes = new List<(string Name, string Version, Type Type)>();
            activityTypes.Add(("TestActivity", "1", typeof(TestActivityV1)));
            activityTypes.Add(("TestActivity", "2", typeof(TestActivityV2)));
            workerHost = TestHelpers.CreateHostBuilder(options, orchestrationTypes, null, activityTypes).Build();
            workerHost.RunAsync();
            orchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync();
        }

        [Fact(DisplayName = "OrchestarionVersion")]
        public void OrchestarionVersion()
        {
            var t1 = orchestrationWorkerClient.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new maskx.OrchestrationService.OrchestrationSetting()
                {
                    Name = "TestOrchestration",
                    Version = "1"
                }
            }).Result;
            var t2 = orchestrationWorkerClient.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new maskx.OrchestrationService.OrchestrationSetting()
                {
                    Name = "TestOrchestration",
                    Version = "2"
                }
            }).Result;
            while (true)
            {
                var r1 = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(t1, TimeSpan.FromSeconds(30)).Result;
                if (r1 != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, r1.OrchestrationStatus);
                    Assert.Equal("1", r1.Output);
                    break;
                }
            }
            while (true)
            {
                var r2 = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(t2, TimeSpan.FromSeconds(30)).Result;
                if (r2 != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, r2.OrchestrationStatus);
                    Assert.Equal("2", r2.Output);
                    break;
                }
            }
        }

        [Fact(DisplayName = "ActivityVersion")]
        public void ActivityVersion()
        {
            var t1 = orchestrationWorkerClient.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new maskx.OrchestrationService.OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).FullName
                },
                Input = dataConverter.Serialize(1)
            }).Result;
            var t2 = orchestrationWorkerClient.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new maskx.OrchestrationService.OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).FullName
                },
                Input = dataConverter.Serialize(2)
            }).Result;
            while (true)
            {
                var r1 = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(t1, TimeSpan.FromSeconds(30)).Result;
                if (r1 != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, r1.OrchestrationStatus);
                    Assert.Equal("1", r1.Output);
                    break;
                }
            }
            while (true)
            {
                var r2 = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(t2, TimeSpan.FromSeconds(30)).Result;
                if (r2 != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, r2.OrchestrationStatus);
                    Assert.Equal("2", r2.Output);
                    break;
                }
            }
        }

        public class TestActivityV1 : TaskActivity<int, int>
        {
            protected override int Execute(TaskContext context, int input)
            {
                return 1;
            }
        }

        public class TestActivityV2 : TaskActivity<int, int>
        {
            protected override int Execute(TaskContext context, int input)
            {
                return 2;
            }
        }

        public class TestOrchestration : TaskOrchestration<int, int>
        {
            public override async Task<int> RunTask(OrchestrationContext context, int input)
            {
                int r = await context.ScheduleTask<int>("TestActivity", input.ToString());
                return r;
            }
        }

        public class TestOrchestrationV1 : TaskOrchestration<int, string>
        {
            public override Task<int> RunTask(OrchestrationContext context, string input)
            {
                return Task.FromResult(1);
            }
        }

        public class TestOrchestrationV2 : TaskOrchestration<int, string>
        {
            public override Task<int> RunTask(OrchestrationContext context, string input)
            {
                return Task.FromResult(2);
            }
        }
    }
}