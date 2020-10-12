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
    public class SimpleFetchRuleTest:IDisposable
    {
        private readonly DataConverter dataConverter = new JsonDataConverter();
        private readonly IHost workerHost = null;
        private readonly OrchestrationWorker orchestrationWorker;
        CommunicationWorker communicationWorker = null;
        IOrchestrationService SQLServerOrchestrationService = null;
        public SimpleFetchRuleTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions
            {
                
                //GetFetchRules = (sp) =>
                //{
                //    var r1 = new FetchRule()
                //    {
                //        What = new Dictionary<string, string>() { { "Processor", "MockCommunicationProcessor" } },
                //    };
                //    r1.Limitions.Add(new Limitation()
                //    {
                //        Concurrency = 1,
                //        Scope = new List<string>()
                //        {
                //        "RequestOperation"
                //        }
                //    });
                //    r1.Limitions.Add(new Limitation
                //    {
                //        Concurrency = 5,
                //        Scope = new List<string>()
                //        {
                //        "RequestTo"
                //        }
                //    });
                //    List<FetchRule> fetchRules = new List<FetchRule>
                //    {
                //        r1
                //    };
                //    return fetchRules;
                //}
            };
            List<(string Name, string Version, Type Type)> orchestrationTypes = new List<(string Name, string Version, Type Type)>
            {
                (typeof(TestOrchestration).FullName, "", typeof(TestOrchestration))
            };
            workerHost = TestHelpers.CreateHostBuilder(
                hubName : "NoRule",
                orchestrationWorkerOptions: new maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions() { GetBuildInOrchestrators = (sp) => orchestrationTypes }
               ).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            communicationWorker = workerHost.Services.GetService<CommunicationWorker>();
            SQLServerOrchestrationService = workerHost.Services.GetService<IOrchestrationService>();
        }

        public void Dispose()
        {
            if (communicationWorker != null)
                communicationWorker.DeleteCommunicationAsync().Wait();
            if (SQLServerOrchestrationService != null)
                SQLServerOrchestrationService.DeleteAsync(true).Wait();
        }

        [Fact(DisplayName = "SimpleFetchRuleTest")]
        public void SendRequst()
        {
            var instance = new OrchestrationInstance() { InstanceId = Guid.NewGuid().ToString("N") };

            orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = instance.InstanceId,
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).FullName
                },
                Input = dataConverter.Serialize(new AsyncRequestInput()
                {
                    Processor = "MockCommunicationProcessor",
                    RequestOperation = "Create",
                    RequestTo = "SPF"
                })
            }).Wait();
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