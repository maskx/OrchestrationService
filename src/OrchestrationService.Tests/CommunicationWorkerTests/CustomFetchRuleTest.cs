using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Xunit;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    [Trait("C", "CommunicationWorker")]
    public class CustomFetchRuleTest : IDisposable
    {
        private readonly DataConverter dataConverter = new JsonDataConverter();
        private readonly IHost workerHost = null;
        private readonly OrchestrationWorker orchestrationWorker;
        readonly CommunicationWorker<CustomCommunicationJob> communicationWorker = null;
        readonly IOrchestrationService SQLServerOrchestrationService = null;
        public CustomFetchRuleTest()
        {
            List<(string Name, string Version, Type Type)> orchestrationTypes = new();
            orchestrationTypes.Add((typeof(TestOrchestration).FullName, "", typeof(TestOrchestration)));
            orchestrationTypes.Add((typeof(TestOrchestration<CustomCommunicationJob>).ToString(), "", typeof(TestOrchestration<CustomCommunicationJob>)));
            workerHost = TestHelpers.CreateHostBuilder<CustomCommunicationJob>(
                hubName: "CustomRule",
                orchestrationWorkerOptions: new maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions()
                {
                    GetBuildInOrchestrators = (sp) => orchestrationTypes
                }
               ).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            communicationWorker = workerHost.Services.GetService<CommunicationWorker<CustomCommunicationJob>>();
            SQLServerOrchestrationService = workerHost.Services.GetService<IOrchestrationService>();
        }

        public void Dispose()
        {
            if (communicationWorker != null)
                communicationWorker.DeleteCommunicationAsync().Wait();
            if (SQLServerOrchestrationService != null)
                SQLServerOrchestrationService.DeleteAsync(true).Wait();
            GC.SuppressFinalize(this);
        }

        [Fact(DisplayName = "CustomFetchRuleTest")]
        public void SendRequst()
        {
            var instance = new OrchestrationInstance() { InstanceId = Guid.NewGuid().ToString("N") };

            orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = instance.InstanceId,
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration<CustomCommunicationJob>).ToString()
                },
                Input = dataConverter.Serialize(new CustomCommunicationJob()
                {
                    Processor = "MockCommunicationProcessor",
                    RequestOperation = "Create",
                    RequestTo = "SPF",
                    SubscriptionId = Guid.NewGuid().ToString(),
                    ManagementUnit = "abc"
                })
            }).Wait();

            var hubClient = new TaskHubClient(workerHost.Services.GetService<IOrchestrationServiceClient>());
            while (true)
            {
                var result = hubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;

                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = dataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, response.Code);
                    var r = response.Content as CommunicationResult;
                    Assert.Equal("MockCommunicationProcessor", r.ResponseContent);
                    break;
                }
            }
        }
    }
}