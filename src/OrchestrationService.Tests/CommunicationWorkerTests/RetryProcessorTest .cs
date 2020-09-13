using DurableTask.Core;
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
    public class RetryProcessorTest : IDisposable
    {
        private DataConverter dataConverter = new JsonDataConverter();
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;

        public RetryProcessorTest()
        {
            CommunicationWorkerOptions options = new CommunicationWorkerOptions();
            options.HubName = "NoRule";
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

        [Fact(DisplayName = "RetryProcessorTest")]
        public void SendRequst()
        {
            var instance = orchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = typeof(TestOrchestration).FullName
                },
                Input = dataConverter.Serialize(new AsyncRequestInput()
                {
                    Processor = "MockRetryCommunicationProcessor"
                })
            }).Result;
            orchestrationWorker.RegistOrchestrationCompletedAction((args) =>
            {
                if (args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId)
                {
                    var b = args;
                }
            });
            while (true)
            {
                var result = TestHelpers.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = dataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, response.Code);
                    var r = response.Content as CommunicationResult;
                    Assert.Equal("Retry->Completed", r.ResponseContent);
                    break;
                }
            }
        }
    }
}