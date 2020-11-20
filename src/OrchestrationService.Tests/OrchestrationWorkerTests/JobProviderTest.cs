using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrchestrationService.Tests.CommunicationWorkerTests;
using System;
using System.Collections.Generic;
using Xunit;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    [Trait("C", "JobProvider")]
    public class JobProviderTest : IDisposable
    {
        private readonly IHost workerHost = null;
        private readonly OrchestrationWorker orchestrationWorker;
        private readonly IOrchestrationService orchestrationService;
        private readonly CommunicationWorker<CustomCommunicationJob> communicationWorker;
        public JobProviderTest()
        {
            workerHost = TestHelpers.CreateHostBuilder(
                (cxt, services) =>
                {
                    services.AddSingleton<IJobProvider>(new JobProvider());
                },
                hubName: "NoRule",
                orchestrationWorkerOptions: new OrchestrationWorkerOptions()
                {
                    GetBuildInOrchestrators = (sp) => new List<(string Name, string Version, Type Type)>()
                    {
                        ("TestOrchestration", "", typeof(TestOrchestration))
                    }
                }).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            orchestrationService = workerHost.Services.GetService<IOrchestrationService>();
            communicationWorker = workerHost.Services.GetService<CommunicationWorker<CustomCommunicationJob>>();
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

        public void Dispose()
        {
            if (orchestrationService != null)
                orchestrationService.DeleteAsync(true).Wait();
            if (communicationWorker != null)
                communicationWorker.DeleteCommunicationAsync().Wait();
            GC.SuppressFinalize(this);
        }
    }
}