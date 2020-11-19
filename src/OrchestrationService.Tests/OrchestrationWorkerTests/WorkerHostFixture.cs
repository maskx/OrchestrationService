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

        readonly CommunicationWorker communicationWorker = null;
        readonly IOrchestrationService SQLServerOrchestrationService = null;
        public WorkerHostFixture()
        {
            workerHost = TestHelpers.CreateHostBuilder(
                (cxt, sp) =>
                {
                    sp.AddSingleton<OrchestrationWorkerClient>();
                },
                hubName: "OrchestrationWorkerTests",
                orchestrationWorkerOptions: new OrchestrationWorkerOptions()
                {
                    GetBuildInOrchestrators = (sp) => new List<(string Name, string Version, Type Type)> { ("TestOrchestration", "", typeof(TestOrchestration)) }
                }
             ).Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            communicationWorker = workerHost.Services.GetService<CommunicationWorker>();
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