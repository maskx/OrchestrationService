using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
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
        readonly CommunicationWorkerClient<CustomCommunicationJob> _CommunicationWorkerClient = null;
        public CustomFetchRuleTest()
        {
            List<(string Name, string Version, Type Type)> orchestrationTypes = new();
            orchestrationTypes.Add((typeof(TestOrchestration).FullName, "", typeof(TestOrchestration)));
            workerHost = TestHelpers.CreateHostBuilder(
                hubName: "CustomRule",
                    orchestrationWorkerOptions: new OrchestrationWorkerOptions()
                    {
                        AutoCreate = true,
                        GetBuildInOrchestrators = (sp) => orchestrationTypes
                    }
                   ).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            communicationWorker = workerHost.Services.GetService<CommunicationWorker<CustomCommunicationJob>>();
            SQLServerOrchestrationService = workerHost.Services.GetService<IOrchestrationService>();
            _CommunicationWorkerClient = workerHost.Services.GetService<CommunicationWorkerClient<CustomCommunicationJob>>();
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
            var subId = Guid.NewGuid().ToString();
            var instance = new OrchestrationInstance() { InstanceId = Guid.NewGuid().ToString("N") };
            _CommunicationWorkerClient.CreateFetchRuleAsync(new FetchRule()
            {
                Name = "Rule1",
                Concurrency = 2,
                What = new List<Where>() { new Where() { Name = "SubscriptionId", Operator = "=", Value = $"'{subId}'" } },
                Scope = new List<string>() { "ManagementUnit" }
            }).Wait();
            _CommunicationWorkerClient.BuildFetchCommunicationJobSPAsync().Wait();
            orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = instance.InstanceId,
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).FullName
                },
                Input = dataConverter.Serialize(new CustomCommunicationJob()
                {
                    Processor = "MockCommunicationProcessor",
                    RequestOperation = "Create",
                    RequestTo = "SPF",
                    SubscriptionId = subId,
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