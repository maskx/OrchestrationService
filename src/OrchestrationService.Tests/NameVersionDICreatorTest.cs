using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace OrchestrationService.Tests
{
    [Trait("c", "NameVersionDICreatorTest")]
    public class NameVersionDICreatorTest :IDisposable
    {
        private readonly DataConverter dataConverter = new JsonDataConverter();
        private readonly IHost workerHost = null;
        private readonly OrchestrationWorkerClient orchestrationWorkerClient;
        readonly CommunicationWorker communicationWorker = null;
        readonly IOrchestrationService SQLServerOrchestrationService = null;
        public NameVersionDICreatorTest()
        {
            List<(string Name, string Version, Type Type)> orchestrationTypes = new();
            orchestrationTypes.Add(("TestOrchestration", "1", typeof(TestOrchestrationV1)));
            orchestrationTypes.Add(("TestOrchestration", "2", typeof(TestOrchestrationV2)));
            orchestrationTypes.Add((typeof(TestOrchestration).FullName, "", typeof(TestOrchestration)));
            List<(string Name, string Version, Type Type)> activityTypes = new();
            activityTypes.Add(("TestActivity", "1", typeof(TestActivityV1)));
            activityTypes.Add(("TestActivity", "2", typeof(TestActivityV2)));
            workerHost = TestHelpers.CreateHostBuilder(
                hubName : "NameVersionDICreatorTest",
                orchestrationWorkerOptions: new OrchestrationWorkerOptions()
                {
                    GetBuildInOrchestrators = (sp) => orchestrationTypes,
                    GetBuildInTaskActivities = (sp) => activityTypes
                }
               ).Build();
            workerHost.RunAsync();
            orchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            communicationWorker = workerHost.Services.GetService<CommunicationWorker>();
            SQLServerOrchestrationService = workerHost.Services.GetService<IOrchestrationService>();
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
            var client = new TaskHubClient(workerHost.Services.GetService<IOrchestrationServiceClient>());
            while (true)
            {
                var r1 = client.WaitForOrchestrationAsync(t1, TimeSpan.FromSeconds(30)).Result;
                if (r1 != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, r1.OrchestrationStatus);
                    Assert.Equal("1", r1.Output);
                    break;
                }
            }
            while (true)
            {
                var r2 = client.WaitForOrchestrationAsync(t2, TimeSpan.FromSeconds(30)).Result;
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
            var client = new TaskHubClient(workerHost.Services.GetService<IOrchestrationServiceClient>());
            while (true)
            {
                var r1 = client.WaitForOrchestrationAsync(t1, TimeSpan.FromSeconds(30)).Result;
                if (r1 != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, r1.OrchestrationStatus);
                    Assert.Equal("1", r1.Output);
                    break;
                }
            }
            while (true)
            {
                var r2 = client.WaitForOrchestrationAsync(t2, TimeSpan.FromSeconds(30)).Result;
                if (r2 != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, r2.OrchestrationStatus);
                    Assert.Equal("2", r2.Output);
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (communicationWorker != null)
                communicationWorker.DeleteCommunicationAsync().Wait();
            if (SQLServerOrchestrationService != null)
                SQLServerOrchestrationService.DeleteAsync(true).Wait();
            GC.SuppressFinalize(this);
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