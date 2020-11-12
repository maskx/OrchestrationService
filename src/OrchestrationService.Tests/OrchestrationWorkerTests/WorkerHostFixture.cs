using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using DurableTask.Core;

namespace OrchestrationService.Tests.OrchestrationWorkerTests
{
    public class WorkerHostFixture : IDisposable
    {
        private readonly IHost workerHost = null;
        public OrchestrationWorker OrchestrationWorker { get; private set; }
        public OrchestrationWorkerClient OrchestrationWorkerClient { get; private set; }

        readonly CommunicationWorker<CommunicationJob> communicationWorker = null;
        readonly IOrchestrationService SQLServerOrchestrationService = null;
        public WorkerHostFixture()
        {
            CommunicationWorkerOptions options = new();
            options.HubName = "OrchestrationWorkerTests";
            var orchestrationTypes = new List<(string Name, string Version, Type Type)>
            {
                ("TestOrchestration", "", typeof(TestOrchestration))
            };
            workerHost = TestHelpers.CreateHostBuilder(
                communicationWorkerOptions: options,
                orchestrationWorkerOptions: new maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions() { GetBuildInOrchestrators = (sp) => orchestrationTypes }
             ).Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            communicationWorker = workerHost.Services.GetService<CommunicationWorker<CommunicationJob>>();
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
    }

    [CollectionDefinition("WorkerHost Collection")]
    public class WebHostCollection : ICollectionFixture<WorkerHostFixture>
    {
    }
}