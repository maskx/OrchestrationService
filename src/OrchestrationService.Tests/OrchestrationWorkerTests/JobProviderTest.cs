using DurableTask.Core.Serializing;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using DurableTask.Core;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    [Trait("C", "JobProvider")]
    public class JobProviderTest
    {
        private DataConverter dataConverter = new JsonDataConverter();
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;

        public JobProviderTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
            List<Type> orchestrationTypes = new List<Type>();
            orchestrationTypes.Add(typeof(TestOrchestration));
            workerHost = TestHelpers.CreateHostBuilder(options, orchestrationTypes, (cxt, services) =>
            {
                services.AddSingleton<IJobProvider>(new JobProvider());
            }).Build();
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
                Orchestration = new maskx.OrchestrationService.OrchestrationCreator.Orchestration()
                {
                    Creator = "DICreator",
                    Uri = typeof(TestOrchestration).FullName + "_"
                },
                Input = ""
            }).Wait();
            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
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
                Orchestration = new maskx.OrchestrationService.OrchestrationCreator.Orchestration()
                {
                    Creator = "DICreator",
                    Uri = typeof(TestOrchestration).FullName + "_"
                },
                Input = ""
            });
            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
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