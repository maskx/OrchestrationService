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
    public class NoFetchRuleTest : IDisposable
    {
        private readonly DataConverter dataConverter = new JsonDataConverter();
        private readonly IHost workerHost = null;
        private readonly OrchestrationWorker orchestrationWorker;
        readonly CommunicationWorker<CustomCommunicationJob> communicationWorker = null;
        readonly IOrchestrationService SQLServerOrchestrationService = null;
        public NoFetchRuleTest()
        {
            List<(string Name, string Version, Type Type)> orchestrationTypes = new();
            orchestrationTypes.Add((typeof(TestOrchestration).FullName, "", typeof(TestOrchestration)));
            workerHost = TestHelpers.CreateHostBuilder(
                hubName: "NoFetchRuleTest",
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
        }

        public void Dispose()
        {
            if (communicationWorker != null)
                communicationWorker.DeleteCommunicationAsync().Wait();
            if (SQLServerOrchestrationService != null)
                SQLServerOrchestrationService.DeleteAsync(true).Wait();
            GC.SuppressFinalize(this);
        }

        [Fact(DisplayName = "NoFetchRuleTest")]
        public void SendRequst()
        {
            var instance = orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).FullName
                },
                Input = dataConverter.Serialize(new CustomCommunicationJob()
                {
                    Processor = "MockCommunicationProcessor"
                })
            }).Result;
            orchestrationWorker.RegistOrchestrationCompletedAction((args) =>
            {
                if (args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId)
                {
                    var b = args;
                }
            });
            var client = new TaskHubClient(workerHost.Services.GetService<IOrchestrationServiceClient>());
            while (true)
            {
                var result = client.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
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