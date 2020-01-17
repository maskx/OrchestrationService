using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using DurableTask.Core;
using System.Threading.Tasks;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace OrchestrationService.Tests
{
    [Trait("c", "TraceActivityTest")]
    public class TraceActivityTest
    {
        private IHost workerHost = null;
        private OrchestrationWorker orchestrationWorker;

        public TraceActivityTest()
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

        [Fact(DisplayName = "Trace")]
        public void Trace()
        {
        }

        public class TestOrchestration : TaskOrchestration<int, string>
        {
            public override async Task<int> RunTask(OrchestrationContext context, string input)
            {
                await context.ScheduleTask<TaskResult>(typeof(TraceActivity), new TraceActivityInput()
                {
                    EventLevel = TraceEventType.Information,
                    EventType = "TestOrchestration-Begin",
                    Format = "123",
                });
                return 1;
            }
        }
    }
}