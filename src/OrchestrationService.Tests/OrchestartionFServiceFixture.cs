using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace OrchestrationService.Tests
{
    public class OrchestartionFServiceFixture
    {
        private IHost workerHost = null;
        public IServiceProvider ServiceProvider { get; private set; }
        public OrchestrationWorker OrchestrationWorker { get; private set; }
        public OrchestrationWorkerClient OrchestrationWorkerClient { get; private set; }
        public CommunicationWorker CommunicationWorker { get; private set; }
        public OrchestartionFServiceFixture()
        {
            workerHost = TestHelpers.CreateHostBuilder().Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            CommunicationWorker = workerHost.Services.GetService<CommunicationWorker>();
            this.ServiceProvider = workerHost.Services;
        }
        [CollectionDefinition("WebHost OrchestartionFService")]
        public class WebHostCollection : ICollectionFixture<OrchestartionFServiceFixture>
        {
        }
    }
}
