﻿using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OrchestrationService.Tests.CommunicationWorkerTests
{
    [Trait("C", "CommunicationWorker")]
    public class NoFetchRuleTest : IDisposable
    {
        private DataConverter dataConverter = new JsonDataConverter();
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;

        public NoFetchRuleTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
            List<Type> orchestrationTypes = new List<Type>();
            orchestrationTypes.Add(typeof(TestOrchestration));
            workerHost = TestHelpers.CreateHostBuilder(options, orchestrationTypes).Build();
            workerHost.RunAsync();
            orchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
        }

        public void Dispose()
        {
            if (workerHost != null)
                workerHost.StopAsync();
        }

        [Fact(DisplayName = "NoFetchRuleTest")]
        public void SendRequst()
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
                Input = dataConverter.Serialize(new AsyncRequestInput()
                {
                    Processor = "MockCommunicationProcessor"
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
                    Assert.Equal("MockCommunicationProcessor", response.Content);
                    break;
                }
            }
        }
    }
}