using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;
using DurableTask.Core;

namespace OrchestrationService.Tests
{
    public class OrchestartionFServiceFixture:IDisposable
    {
        private readonly IHost workerHost = null;
        public IServiceProvider ServiceProvider { get; private set; }
        public OrchestrationWorker OrchestrationWorker { get; private set; }
        public OrchestrationWorkerClient OrchestrationWorkerClient { get; private set; }
        public CommunicationWorker CommunicationWorker { get; private set; }
        public IOrchestrationService OrchestrationService { get; private set; }
        public OrchestartionFServiceFixture()
        {
            workerHost = TestHelpers.CreateHostBuilder().Build();
            workerHost.RunAsync();
            OrchestrationWorker = workerHost.Services.GetService<OrchestrationWorker>();
            OrchestrationWorkerClient = workerHost.Services.GetService<OrchestrationWorkerClient>();
            CommunicationWorker = workerHost.Services.GetService<CommunicationWorker>();
            OrchestrationService = workerHost.Services.GetService<IOrchestrationService>();
            this.ServiceProvider = workerHost.Services;
        }
       
        [CollectionDefinition("WebHost OrchestartionFService")]
        public class WebHostCollection : ICollectionFixture<OrchestartionFServiceFixture>
        {
        }

        public void Dispose()
        {
            if (CommunicationWorker != null)
                CommunicationWorker.DeleteCommunicationAsync().Wait();
            if (OrchestrationService != null)
                OrchestrationService.DeleteAsync(true).Wait();
            GC.SuppressFinalize(this);
        }
    }
}
