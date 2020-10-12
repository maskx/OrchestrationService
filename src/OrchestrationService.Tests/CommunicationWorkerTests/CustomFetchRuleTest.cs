﻿using DurableTask.Core;
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
    public class CustomFetchRuleTest :IDisposable
    {
        private DataConverter dataConverter = new JsonDataConverter();
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;
        CommunicationWorker communicationWorker = null;
        IOrchestrationService SQLServerOrchestrationService = null;
        public CustomFetchRuleTest()
        {
            //options.GetFetchRules = (sp) =>
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
            //            "SubscriptionId"
            //        }
            //    });
            //    r1.Limitions.Add(new Limitation
            //    {
            //        Concurrency = 5,
            //        Scope = new List<string>()
            //        {
            //            "ManagementUnit"
            //        }
            //    });
            //    List<FetchRule> fetchRules = new List<FetchRule>();
            //    fetchRules.Add(r1);
            //    return fetchRules;
            //};
            //options.RuleFields.Add("SubscriptionId");
            //options.RuleFields.Add("ManagementUnit");
    
            List<(string Name, string Version, Type Type)> orchestrationTypes = new List<(string Name, string Version, Type Type)>();
            orchestrationTypes.Add((typeof(TestOrchestration).FullName, "", typeof(TestOrchestration)));
            workerHost = TestHelpers.CreateHostBuilder(
                hubName : "CustomRule",
                orchestrationWorkerOptions: new maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions() {
                    GetBuildInOrchestrators = (sp) => orchestrationTypes 
                }
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

        [Fact(DisplayName = "CustomFetchRuleTest")]
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
                    RequestTo = "SPF",
                    RuleField = new Dictionary<string, object>() {
                        { "SubscriptionId","123"},
                        {"ManagementUnit","abc" }
                    }
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