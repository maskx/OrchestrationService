using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Xunit;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    [Trait("C", "JobProvider")]
    public class JobProviderTest
    {
        private readonly IHost workerHost = null;
        private readonly OrchestrationWorker orchestrationWorker;

        public JobProviderTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions
            {
                HubName = "NoRule"
            };
            List<(string Name, string Version, Type Type)> orchestrationTypes = new List<(string Name, string Version, Type Type)>
            {
                ("TestOrchestration", "", typeof(TestOrchestration))
            };
            workerHost = TestHelpers.CreateHostBuilder(
                (cxt, services) =>
                {
                    services.AddSingleton<IJobProvider>(new JobProvider());
                },
                communicationWorkerOptions: options,
                orchestrationWorkerOptions: new maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions() { GetBuildInOrchestrators = (sp) => orchestrationTypes }).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync();
        }

        [Fact(DisplayName = "JumpStart")]
        public void JumpStart()
        {
            var instance = new OrchestrationInstance() { InstanceId = Guid.NewGuid().ToString("N") };

            orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = instance.InstanceId,
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).Name
                },
                Input = ""
            }).Wait();
            var client = new TaskHubClient(workerHost.Services.GetService<IOrchestrationServiceClient>());
            while (true)
            {
                var result = client.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    Assert.Equal("1", result.Output);
                    break;
                }
            }
        }

        [Fact(DisplayName = "JobProvider")]
        public void FetchFromJobProvider()
        {
            var instance = new OrchestrationInstance() { InstanceId = Guid.NewGuid().ToString("N") };
            JobProvider.Jobs.Add(new Job()
            {
                InstanceId = instance.InstanceId,
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).Name
                },
                Input = ""
            });
            var client = new TaskHubClient(workerHost.Services.GetService<IOrchestrationServiceClient>());
            while (true)
            {
                var result = client.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
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