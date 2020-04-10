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
    public class SimpleFetchRuleTest : IDisposable
    {
        private DataConverter dataConverter = new JsonDataConverter();
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;

        public SimpleFetchRuleTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
            options.GetFetchRules = (sp) =>
            {
                var r1 = new FetchRule()
                {
                    What = new Dictionary<string, string>() { { "Processor", "MockCommunicationProcessor" } },
                };
                r1.Limitions.Add(new Limitation()
                {
                    Concurrency = 1,
                    Scope = new List<string>()
                    {
                        "RequestOperation"
                    }
                });
                r1.Limitions.Add(new Limitation
                {
                    Concurrency = 5,
                    Scope = new List<string>()
                    {
                        "RequestTo"
                    }
                });
                List<FetchRule> fetchRules = new List<FetchRule>();
                fetchRules.Add(r1);
                return fetchRules;
            };
            List<(string Name, string Version, Type Type)> orchestrationTypes = new List<(string Name, string Version, Type Type)>();
            orchestrationTypes.Add((typeof(TestOrchestration).FullName, "", typeof(TestOrchestration)));
            workerHost = TestHelpers.CreateHostBuilder(options, orchestrationTypes).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync();
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

            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;

                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = dataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, response.Code);
                    var r = dataConverter.Deserialize<CommunicationResult>(response.Content);
                    Assert.Equal("MockCommunicationProcessor", r.ResponseContent);
                    break;
                }
            }
        }
    }
}